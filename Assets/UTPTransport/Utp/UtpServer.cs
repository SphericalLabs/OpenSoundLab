using Mirror;
using System;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;

namespace Utp
{

	#region Jobs

	/// <summary>
	/// Job used to update connections. 
	/// </summary>
	[BurstCompile]
	struct ServerUpdateConnectionsJob : IJob
	{
		/// <summary>
		/// Used to bind, listen, and send data to connections.
		/// </summary>
		public NetworkDriver driver;

		/// <summary>
		/// Client connections to this server.
		/// </summary>
		public NativeList<Unity.Networking.Transport.NetworkConnection> connections;

		/// <summary>
		/// Temporary storage for connection events that occur on job threads so they may be dequeued on the main thread.
		/// </summary>
		public NativeQueue<UtpConnectionEvent>.ParallelWriter connectionsEventsQueue;

		public void Execute()
		{
			//Iterate through connections list
			for (int i = 0; i < connections.Length; i++)
			{
				//If a connection is no longer established, remove it
				if (driver.GetConnectionState(connections[i]) == Unity.Networking.Transport.NetworkConnection.State.Disconnected)
				{
					connections.RemoveAtSwapBack(i--);
				}
			}

			// Accept new connections
			Unity.Networking.Transport.NetworkConnection networkConnection;
			while ((networkConnection = driver.Accept()) != default(Unity.Networking.Transport.NetworkConnection))
			{
				//Set up connection event
				UtpConnectionEvent connectionEvent = new UtpConnectionEvent()
				{
					eventType = (byte)UtpConnectionEventType.OnConnected,
					connectionId = networkConnection.GetHashCode()
				};

				//Queue connection event
				connectionsEventsQueue.Enqueue(connectionEvent);

				//Add connection to connection list
				connections.Add(networkConnection);
			}
		}

		/// <summary>
		/// Disconnect and remove a connection via it's ID.
		/// </summary>
		/// <param name="connectionId">The ID of the connection to disconnect.</param>
		public void Disconnect(int connectionId)
		{
			foreach (Unity.Networking.Transport.NetworkConnection connection in connections)
			{
				if (connection.GetHashCode() == connectionId)
				{
					connection.Disconnect(driver);

					//Set up connection event
					UtpConnectionEvent connectionEvent = new UtpConnectionEvent()
					{
						eventType = (byte)UtpConnectionEventType.OnDisconnected,
						connectionId = connection.GetHashCode()
					};

					//Queue connection event
					connectionsEventsQueue.Enqueue(connectionEvent);

					return;
				}
			}
		}
	}

	/// <summary>
	/// Job to query incoming events for all connections. 
	/// </summary>
	[BurstCompile]
	struct ServerUpdateJob : IJobParallelForDefer
	{
		/// <summary>
		/// Used to bind, listen, and send data to connections.
		/// </summary>
		public NetworkDriver.Concurrent driver;

		/// <summary>
		/// client connections to this server.
		/// </summary>
		public NativeArray<Unity.Networking.Transport.NetworkConnection> connections;

		/// <summary>
		/// Temporary storage for connection events that occur on job threads so they may be dequeued on the main thread.
		/// </summary>
		public NativeQueue<UtpConnectionEvent>.ParallelWriter connectionsEventsQueue;

		/// <summary>
		/// Process all incoming events/messages on this connection.
		/// </summary>
		/// <param name="index">The current index being accessed in the array.</param>
		public void Execute(int index)
		{
			NetworkEvent.Type netEvent;
			while ((netEvent = driver.PopEventForConnection(connections[index], out DataStreamReader stream)) != NetworkEvent.Type.Empty)
			{
				if (netEvent == NetworkEvent.Type.Data)
				{
					NativeArray<byte> nativeMessage = new NativeArray<byte>(stream.Length, Allocator.Temp);
					stream.ReadBytes(nativeMessage);

					//Set up connection event
					UtpConnectionEvent connectionEvent = new UtpConnectionEvent()
					{
						eventType = (byte)UtpConnectionEventType.OnReceivedData,
						eventData = GetFixedList(nativeMessage),
						connectionId = connections[index].GetHashCode()
					};

					//Queue connection event
					connectionsEventsQueue.Enqueue(connectionEvent);
				}
				else if (netEvent == NetworkEvent.Type.Disconnect)
				{
					//Set up disconnect event
					UtpConnectionEvent connectionEvent = new UtpConnectionEvent()
					{
						eventType = (byte)UtpConnectionEventType.OnDisconnected,
						connectionId = connections[index].GetHashCode()
					};

					//Queue disconnect event
					connectionsEventsQueue.Enqueue(connectionEvent);
				}
			}
		}

		/// <summary>
		/// Convert unmanaged native array to 4096 Byte list. Uses unsafe code.
		/// </summary>
		/// <param name="data">The data to convert.</param>
		/// <returns>An unmanaged fixed list of data.</returns>
		public FixedList4096Bytes<byte> GetFixedList(NativeArray<byte> data)
		{
			FixedList4096Bytes<byte> retVal = new FixedList4096Bytes<byte>();

			if (data.Length > 0)
			{
				unsafe
				{
					retVal.AddRange(NativeArrayUnsafeUtility.GetUnsafePtr(data), data.Length);
				}
			}

			return retVal;
		}
	}

	[BurstCompile]
	struct ServerSendJob : IJob
	{
		/// <summary>
		/// Used to bind, listen, and send data to connections.
		/// </summary>
		public NetworkDriver driver;

		/// <summary>
		/// The network pipeline to stream data.
		/// </summary>
		public NetworkPipeline pipeline;

		/// <summary>
		/// The client's network connection instance.
		/// </summary>
		public Unity.Networking.Transport.NetworkConnection connection;

		/// <summary>
		/// The segment of data to send over (deallocates after use).
		/// </summary>
		[DeallocateOnJobCompletion]
		public NativeArray<byte> data;

		public void Execute()
		{
			DataStreamWriter writer;
			int writeStatus = driver.BeginSend(pipeline, connection, out writer);

			//If Acquire was success
			if (writeStatus == (int)Unity.Networking.Transport.Error.StatusCode.Success)
			{
				writer.WriteBytes(data);
				driver.EndSend(writer);
			}
		}
	}

	#endregion

	/// <summary>
	/// A listen server for Mirror using UTP. 
	/// </summary>
	public class UtpServer : UtpEntity
	{
		/// <summary>
		/// Invokes when a client has connected to the server.
		/// </summary>
		public Action<int> OnConnected;

		/// <summary>
		/// Invokes when data has been received by a third party.
		/// </summary>
		public Action<int, ArraySegment<byte>> OnReceivedData;

		/// <summary>
		/// Invokes when a client has disconnected.
		/// </summary>
		public Action<int> OnDisconnected;

		/// <summary>
		/// Client connections to this server.
		/// </summary>
		private NativeList<Unity.Networking.Transport.NetworkConnection> connections;

		/// <summary>
		/// The number of pipelines tracked in the header size array.
		/// </summary>
		private const int NUM_PIPELINES = 2;

		/// <summary>
		/// The driver's max header size for UTP transport.
		/// </summary>
		private int[] driverMaxHeaderSize = new int[NUM_PIPELINES];

		/// <summary>
		/// Constructor for UTP server.
		/// </summary>
		/// <param name="timeoutInMilliseconds">The response timeout in miliseconds.</param>
		public UtpServer(int timeoutInMilliseconds)
		{
			this.timeoutInMilliseconds = timeoutInMilliseconds;
		}

		/// <summary>
		/// Constructor for UTP server.
		/// </summary>
		/// <param name="OnConnected">Action that is invoked when connected.</param>
		/// <param name="OnReceivedData">Action that is invoked when receiving data.</param>
		/// <param name="OnDisconnected">Action that is invoked when disconnected.</param>
		/// <param name="timeoutInMilliseconds">The response timeout in miliseconds.</param>
		public UtpServer(Action<int> OnConnected, Action<int, ArraySegment<byte>> OnReceivedData, Action<int> OnDisconnected, int timeoutInMilliseconds)
			: this(timeoutInMilliseconds)
		{
			this.OnConnected = OnConnected;
			this.OnReceivedData = OnReceivedData;
			this.OnDisconnected = OnDisconnected;
		}

		/// <summary>
		/// Initialize the server. Currently only supports IPV4.
		/// </summary>
		/// <param name="port">The port to listen for connections on.</param>
		/// <param name="useRelay">Whether or not to use start a server using Unity's Relay Service.</param>
		/// <param name="allocation">The Relay allocation, if using Relay.</param>
		public void Start(ushort port, bool useRelay = false, Allocation allocation = null)
		{
			if (IsNetworkDriverInitialized())
			{
				UtpLog.Warning("Attempting to start a server that is already active.");
				return;
			}

			//Instantiate network settings
			var settings = new NetworkSettings();
			settings.WithNetworkConfigParameters(disconnectTimeoutMS: timeoutInMilliseconds);

			//Create IPV4 endpoint
			NetworkEndPoint endpoint = NetworkEndPoint.AnyIpv4;
			endpoint.Port = port;

			if (useRelay)
			{
				//Instantiate relay network data
				RelayServerData relayServerData = RelayUtils.HostRelayData(allocation, RelayServerEndpoint.NetworkOptions.Udp);
				RelayNetworkParameter relayNetworkParameter = new RelayNetworkParameter { ServerData = relayServerData };
				NetworkSettings networkSettings = new NetworkSettings();

				//Initialize relay network
				RelayParameterExtensions.WithRelayParameters(ref networkSettings, ref relayServerData);

				//Instantiate network driver
				driver = NetworkDriver.Create(networkSettings);
			}
			else
			{
				//Initialize network settings
				NetworkSettings networkSettings = new NetworkSettings();

				//Instantiate network driver
				driver = NetworkDriver.Create(networkSettings);
				endpoint.Port = port;
			}

			//Initialize connections list & event queue
			connections = new NativeList<Unity.Networking.Transport.NetworkConnection>(16, Allocator.Persistent);
			connectionsEventsQueue = new NativeQueue<UtpConnectionEvent>(Allocator.Persistent);

			//Create network pipelines
			reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
			unreliablePipeline = driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));

			int bindReturnCode = driver.Bind(endpoint);
			if (!driver.Bound)
			{
				UtpLog.Error($"Unable to start server, failed to bind the specified port {endpoint.Port}. {nameof(NetworkDriver.Bind)}() returned {bindReturnCode}.");
				return;
			}

			int listenReturnCode = driver.Listen();
			if (!driver.Listening)
			{
				UtpLog.Error($"Unable to start server, failed to listen. {nameof(NetworkDriver.Listen)} returned {listenReturnCode}.");
				return;
			}

			UtpLog.Info(useRelay ? ("Relay server started") : ($"Server started on port: {endpoint.Port}"));
		}

		/// <summary>
		/// Tick the server, creating the server jobs and scheduling them. Processes events created by the jobs.
		/// </summary>
		public void Tick()
		{
			//If the network driver has shut down, back out
			if (!IsNetworkDriverInitialized())
			{
				return;
			}

			// First complete the job that was initialized in the previous frame
			jobHandle.Complete();

			// Trigger Mirror callbacks for events that resulted in the last jobs work
			ProcessIncomingEvents();

			//Cache driver & connection info
			cacheConnectionInfo();

			// Create a new jobs
			var serverUpdateJob = new ServerUpdateJob
			{
				driver = driver.ToConcurrent(),
				connections = connections.AsDeferredJobArray(),
				connectionsEventsQueue = connectionsEventsQueue.AsParallelWriter()
			};

			var connectionJob = new ServerUpdateConnectionsJob
			{
				driver = driver,
				connections = connections,
				connectionsEventsQueue = connectionsEventsQueue.AsParallelWriter()
			};

			// Schedule jobs
			jobHandle = driver.ScheduleUpdate();

			// We are explicitly scheduling ServerUpdateJob before ServerUpdateConnectionsJob so that disconnect events are enqueued before the corresponding NetworkConnection is removed
			jobHandle = serverUpdateJob.Schedule(connections, 1, jobHandle);
			jobHandle = connectionJob.Schedule(jobHandle);
		}

		/// <summary>
		/// Stop a running server.
		/// </summary>
		public void Stop()
		{
			UtpLog.Info("Stopping server");

			jobHandle.Complete();

			//Dispose of event queue
			if (connectionsEventsQueue.IsCreated)
			{
				connectionsEventsQueue.Dispose();
			}

			//Dispose of connections
			if (connections.IsCreated)
			{
				connections.Dispose();
			}

			//Dispose of driver
			if (driver.IsCreated)
			{
				driver.Dispose();
				driver = default(NetworkDriver);
			}
		}

		/// <summary>
		/// Disconnect and remove a connection via it's ID.
		/// </summary>
		/// <param name="connectionId">The ID of the connection to disconnect.</param>
		public void Disconnect(int connectionId)
		{
			jobHandle.Complete();

			//Continue if connection was found
			if (TryGetConnection(connectionId, out Unity.Networking.Transport.NetworkConnection connection))
			{
				UtpLog.Info($"Disconnecting connection with ID: {connectionId}");
				connection.Disconnect(driver);

				// When disconnecting, we need to ensure the driver has the opportunity to send a disconnect event to the client
				driver.ScheduleUpdate().Complete();

				//Invoke disconnect action
				OnDisconnected?.Invoke(connectionId);
			}
			else
			{
				UtpLog.Warning($"Connection not found: {connectionId}");
			}
		}

		/// <summary>
		/// Send data to a connection over a particular channel.
		/// </summary>
		/// <param name="connectionId">The ID of the connection to send data to.</param>
		/// <param name="segment">The data to send.</param>
		/// <param name="channelId">The 'Mirror.Channels' channel to send the data over.</param>
		public void Send(int connectionId, ArraySegment<byte> segment, int channelId)
		{
			jobHandle.Complete();

			//Continue if connection was found
			if (TryGetConnection(connectionId, out Unity.Networking.Transport.NetworkConnection connection))
			{
				//Get pipeline for job
				NetworkPipeline pipeline = channelId == Channels.Reliable ? reliablePipeline : unreliablePipeline;

				//Convert ArraySegment to NativeArray for burst compile
				NativeArray<byte> segmentArray = new NativeArray<byte>(segment.Count, Allocator.Persistent);
				NativeArray<byte>.Copy(segment.Array, segment.Offset, segmentArray, 0, segment.Count);

				// Create a new job
				var job = new ClientSendJob
				{
					driver = driver,
					pipeline = pipeline,
					connection = connection,
					data = segmentArray
				};

				// Schedule job
				jobHandle = job.Schedule(jobHandle);
			}
		}

		/// <summary>
		/// Look up a client's address via it's ID. If using Relay, this will always return the address of the Relay server.
		/// </summary>
		/// <param name="connectionId">The ID of the connection.</param>
		/// <returns>The client address, or Relay server if using Relay.</returns>
		public string GetClientAddress(int connectionId)
		{
			//If a connection was found, get its address
			if (TryGetConnection(connectionId, out Unity.Networking.Transport.NetworkConnection connection))
			{
				NetworkEndPoint endpoint = driver.RemoteEndPoint(connection);
				return endpoint.Address;
			}
			else
			{
				UtpLog.Warning($"Connection not found: {connectionId}");
				return String.Empty;
			}
		}

		public int GetMaxHeaderSize(int channelId = Channels.Reliable)
		{
			if (IsNetworkDriverInitialized())
			{
				return driverMaxHeaderSize[channelId];
			}

			return 0;
		}

		/// <summary>
		/// Processes connection events from the queue.
		/// </summary>
		public void ProcessIncomingEvents()
		{
			//Check if the server is active
			if (!IsNetworkDriverInitialized())
			{
				return;
			}

			//Process the events in the event list
			UtpConnectionEvent connectionEvent;
			while (connectionsEventsQueue.TryDequeue(out connectionEvent))
			{
				switch (connectionEvent.eventType)
				{
					//Connect action 
					case ((byte)UtpConnectionEventType.OnConnected):
						{
							OnConnected?.Invoke(connectionEvent.connectionId);
							break;
						}

					//Receive data action
					case ((byte)UtpConnectionEventType.OnReceivedData):
						{
							OnReceivedData?.Invoke(connectionEvent.connectionId, new ArraySegment<byte>(connectionEvent.eventData.ToArray()));
							break;
						}

					//Disconnect action
					case ((byte)UtpConnectionEventType.OnDisconnected):
						{
							OnDisconnected?.Invoke(connectionEvent.connectionId);
							break;
						}

					//Invalid action
					default:
						{
							UtpLog.Warning($"Invalid connection event: {connectionEvent.eventType}");
							break;
						}

				}
			}
		}

		/// <summary>
		/// Processes connection events from the queue.
		/// </summary>
		/// <param name="connectionId">The ID of the connection to find.</param>
		/// <returns>The connection if found in the list, a default connection otherwise.</returns>
		public Unity.Networking.Transport.NetworkConnection FindConnection(int connectionId)
		{
			jobHandle.Complete();

			if (connections.IsCreated)
			{
				foreach (Unity.Networking.Transport.NetworkConnection connection in connections)
				{
					if (connection.GetHashCode() == connectionId)
					{
						return connection;
					}
				}
			}

			return default(Unity.Networking.Transport.NetworkConnection);
		}

		/// <summary>
		/// Returns whether a connection is valid.
		/// </summary>
		/// <param name="connectionId">The id of the connection to check.</param>
		/// <returns>Whether the connection is valid.</returns>
		private bool TryGetConnection(int connectionId, out Unity.Networking.Transport.NetworkConnection connection)
		{
			connection = FindConnection(connectionId);
			return connection.GetHashCode() == connectionId;
		}

		/// <summary>
		/// Determine whether the server is running or not.
		/// </summary>
		/// <returns>True if running, false otherwise.</returns>
		public bool IsActive()
		{
			return IsNetworkDriverInitialized();
		}

		private void cacheConnectionInfo()
		{
			bool isInitialized = IsNetworkDriverInitialized();

			//If driver is active, cache its max header size for UTP transport
			if (isInitialized)
			{
				driverMaxHeaderSize[Channels.Reliable] = driver.MaxHeaderSize(reliablePipeline);
				driverMaxHeaderSize[Channels.Unreliable] = driver.MaxHeaderSize(unreliablePipeline);
			}

		}
	}
}

