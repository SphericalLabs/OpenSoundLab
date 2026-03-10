using Mirror;
using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport.Utilities;

namespace Utp
{
	#region Jobs

	[BurstCompile]
	struct ClientUpdateJob : IJob
	{
		private const int CHANNEL_PREFIX_BYTES = 1;

		/// <summary>
		/// Used to bind, listen, and send data to connections.
		/// </summary>
		public NetworkDriver driver;

		/// <summary>
		/// client connections to this server.
		/// </summary>
		public Unity.Networking.Transport.NetworkConnection connection;

		/// <summary>
		/// Temporary storage for connection events that occur on job threads so they may be dequeued on the main thread.
		/// </summary>
		public NativeQueue<UtpConnectionEvent>.ParallelWriter connectionEventsQueue;

		/// <summary>
		/// Process all incoming events/messages on this connection.
		/// </summary>
		public void Execute()
		{
			//Back out if connection is invalid
			if (!connection.IsCreated)
			{
				return;
			}

			NetworkEvent.Type netEvent;
			while ((netEvent = connection.PopEvent(driver, out DataStreamReader stream)) != NetworkEvent.Type.Empty)
			{
				//Create new event
				UtpConnectionEvent connectionEvent = default(UtpConnectionEvent);

				switch (netEvent)
				{
					//Connect event
					case (NetworkEvent.Type.Connect):
						{

							connectionEvent = new UtpConnectionEvent()
							{
								eventType = (byte)UtpConnectionEventType.OnConnected,
								connectionId = connection.GetHashCode()
							};

							//Queue event
							connectionEventsQueue.Enqueue(connectionEvent);

							break;
						}

					//Data recieved event
					case (NetworkEvent.Type.Data):
						{
							if (stream.Length <= CHANNEL_PREFIX_BYTES)
							{
								break;
							}

							//Create managed array of data
							NativeArray<byte> nativeMessage = new NativeArray<byte>(stream.Length, Allocator.Temp);

							//Read data from stream
							stream.ReadBytes(nativeMessage);

							int payloadLength = stream.Length - CHANNEL_PREFIX_BYTES;
							byte channelId = nativeMessage[0];
							FixedList4096Bytes<byte> eventData = getFixedList(nativeMessage, CHANNEL_PREFIX_BYTES, payloadLength);
							byte eventType = (byte)UtpConnectionEventType.OnReceivedData;
							payloadLength = eventData.Length;

							connectionEvent = new UtpConnectionEvent()
							{
								eventType = eventType,
								connectionId = connection.GetHashCode(),
								eventData = eventData,
								channelId = channelId,
								payloadLength = payloadLength
							};

							//Queue event
							connectionEventsQueue.Enqueue(connectionEvent);

							break;
						}

					//Disconnect event
					case (NetworkEvent.Type.Disconnect):
						{
							connectionEvent = new UtpConnectionEvent()
							{
								eventType = (byte)UtpConnectionEventType.OnDisconnected,
								connectionId = connection.GetHashCode()
							};

							//Queue event
							connectionEventsQueue.Enqueue(connectionEvent);

							break;
						}

				}
			}
		}

		private FixedList4096Bytes<byte> getFixedList(NativeArray<byte> data, int startIndex, int length)
		{
			FixedList4096Bytes<byte> retVal = new FixedList4096Bytes<byte>();
			if (length <= 0)
			{
				return retVal;
			}

			int copyLength = Math.Min(length, retVal.Capacity);

			unsafe
			{
				byte* ptr = (byte*)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(data);
				retVal.AddRange(ptr + startIndex, copyLength);
			}

			return retVal;
		}


	}

	[BurstCompile]
	struct ClientSendJob : IJob
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
			//Back out if connection is invalid
			if (!connection.IsCreated)
			{
				return;
			}

			DataStreamWriter writer;
			int writeStatus = driver.BeginSend(pipeline, connection, out writer);

			//If endpoint was success, write data to stream
			if (writeStatus == (int)Unity.Networking.Transport.Error.StatusCode.Success)
			{
				writer.WriteBytes(data);
				driver.EndSend(writer);
			}
		}
	}

	#endregion

	/// <summary>
	/// A client for Mirror using UTP.
	/// </summary>
	public class UtpClient : UtpEntity
	{
		private const int CHANNEL_PREFIX_BYTES = 1;

		/// <summary>
		/// Invokes when connected to a server.
		/// </summary>
		public Action OnConnected;

		/// <summary>
		/// Invokes when data has been received.
		/// </summary>
		public Action<ArraySegment<byte>, int> OnReceivedData;

		/// <summary>
		/// Invokes when disconnected from a server.
		/// </summary>
		public Action OnDisconnected;

		/// <summary>
		/// Used alongside a driver to connect, send, and receive data from a listen server.
		/// </summary>
		private Unity.Networking.Transport.NetworkConnection connection;

		/// <summary>
		/// The number of pipelines tracked in the header size array.
		/// </summary>
		private const int NUM_PIPELINES = 2;

		/// <summary>
		/// The driver's max header size for UTP transport.
		/// </summary>
		private int[] driverMaxHeaderSize = new int[NUM_PIPELINES];

		/// <summary>
		/// Whether the client is connected to the server or not.
		/// </summary>
		private bool isConnected;

		/// <summary>
		/// Constructor for UTP client.
		/// </summary>
		/// <param name="timeoutInMilliseconds">The response timeout in miliseconds.</param>
		public UtpClient(int timeoutInMilliseconds)
		{
			this.timeoutInMilliseconds = timeoutInMilliseconds;
		}

		/// <summary>
		/// Constructor for UTP client.
		/// </summary>
		/// <param name="OnConnected">Action that is invoked when connected.</param>
		/// <param name="OnReceivedData">Action that is invoked when receiving data.</param>
		/// <param name="OnDisconnected">Action that is invoked when disconnected.</param>
		/// <param name="timeoutInMilliseconds">The response timeout in miliseconds.</param>
		public UtpClient(Action OnConnected, Action<ArraySegment<byte>, int> OnReceivedData, Action OnDisconnected, int timeoutInMilliseconds)
			: this(timeoutInMilliseconds)
		{
			this.OnConnected = OnConnected;
			this.OnReceivedData = OnReceivedData;
			this.OnDisconnected = OnDisconnected;
		}

		/// <summary>
		/// Attempt to connect to a listen server at a given IP/port. Currently only supports IPV4.
		/// </summary>
		/// <param name="address">The host address at which the listen server is running.</param>
		/// <param name="port">The port which the listen server is listening on.</param>
		public void Connect(string address, ushort port)
		{
			if (IsConnected())
			{
				UtpLog.Warning($"Abandoning connection attempt, this client is already connected to a server.");
				return;
			}

			if (string.IsNullOrEmpty(address))
			{
				UtpLog.Error("Abandoning connection attempt, a null or empty address was provided.");
				return;
			}

			if (address == "localhost")
			{
				address = "127.0.0.1";
			}

			// TODO: Support for IPv6.
			NetworkEndPoint endpoint;
			if (!NetworkEndPoint.TryParse(address, port, out endpoint))
			{
				UtpLog.Error($"Abandoning connection attempt, failed to convert {address}:{port} into a valid NetworkEndpoint.");
				return;
			}

			connectionsEventsQueue = new NativeQueue<UtpConnectionEvent>(Allocator.Persistent);

			var settings = new NetworkSettings();
			settings.WithNetworkConfigParameters(disconnectTimeoutMS: timeoutInMilliseconds);
			settings.WithFragmentationStageParameters(payloadCapacity: 1 * 1024 * 1024);

			driver = NetworkDriver.Create(settings);
			reliablePipeline = driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
			unreliablePipeline = driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(UnreliableSequencedPipelineStage));

			connection = driver.Connect(endpoint);

			if (IsValidConnection(connection))
			{
				UtpLog.Info($"Client connected to the server at {address}:{port}.");
			}
			else
			{
				UtpLog.Error($"Client failed to connect to the server at {address}:{port}.");
			}
		}

		/// <summary>
		/// Attempt to connect to a Relay host given a join allocation.
		/// </summary>
		/// <param name="joinAllocation"></param>
		public void RelayConnect(JoinAllocation joinAllocation)
		{
			if (IsConnected())
			{
				UtpLog.Warning($"Abandoning connection attempt, this client is already connected to a server.");
				return;
			}

			connectionsEventsQueue = new NativeQueue<UtpConnectionEvent>(Allocator.Persistent);

			RelayServerData relayServerData = RelayUtils.PlayerRelayData(joinAllocation, RelayServerEndpoint.NetworkOptions.Udp);

			var settings = new NetworkSettings();
			settings.WithNetworkConfigParameters(disconnectTimeoutMS: timeoutInMilliseconds);
			settings.WithRelayParameters(ref relayServerData);
			settings.WithFragmentationStageParameters(payloadCapacity: 1 * 1024 * 1024);

			driver = NetworkDriver.Create(settings);
			reliablePipeline = driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
			unreliablePipeline = driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(UnreliableSequencedPipelineStage));

			connection = driver.Connect(relayServerData.Endpoint);

			if (IsValidConnection(connection))
			{
				UtpLog.Info($"Client connected to the Relay server at {relayServerData.Endpoint.Address}:{relayServerData.Endpoint.Port}.");
			}
			else
			{
				UtpLog.Error($"Client failed to connect to the Relay server at {relayServerData.Endpoint.Address}:{relayServerData.Endpoint.Port}.");
			}
		}

		/// <summary>
		/// Disconnect from a listen server.
		/// </summary>
		public void Disconnect()
		{
			jobHandle.Complete();

			//If there is an existing connection, force a disconnect
			if (connection.IsCreated)
			{
				UtpLog.Info("Client disconnecting from server");

				//Disconnect from server
				connection.Disconnect(driver);
				connection = default(Unity.Networking.Transport.NetworkConnection);

				//We need to ensure the driver has the opportunity to send a disconnect event to the server
				driver.ScheduleUpdate().Complete();

				//Invoke disconnect action
				OnDisconnected?.Invoke();
			}

			//Flush the event queue
			if (connectionsEventsQueue.IsCreated)
			{
				while (connectionsEventsQueue.TryDequeue(out UtpConnectionEvent connectionEvent)) { }
				connectionsEventsQueue.Dispose();
			}

			//Dispose of existing network driver
			if (driver.IsCreated)
			{
				driver.Dispose();
				driver = default(NetworkDriver);
			}
		}

		/// <summary>
		/// Tick the client, creating the client job and scheduling it. Processes incoming events
		/// </summary>
		public void Tick()
		{
			// First complete the job that was initialized in the previous frame
			jobHandle.Complete();

			// Trigger Mirror callbacks for events that resulted in the last jobs work
			ProcessIncomingEvents();

			//Cache driver & connection info
			cacheConnectionInfo();

			// Need to ensure the driver did not become inactive
			if (!IsNetworkDriverInitialized())
			{
				driverMaxHeaderSize = new int[NUM_PIPELINES];
				return;
			}

			// Create a new job
			var job = new ClientUpdateJob
			{
				driver = driver,
				connection = connection,
				connectionEventsQueue = connectionsEventsQueue.AsParallelWriter()
			};

			// Schedule job
			jobHandle = driver.ScheduleUpdate();
			jobHandle = job.Schedule(jobHandle);
		}

		/// <summary>
		/// Send data to the listen server over a particular channel.
		/// </summary>
		/// <param name="segment">The data to send.</param>
		/// <param name="channelId">The 'Mirror.Channels' channel to send the data over.</param>
		public void Send(ArraySegment<byte> segment, int channelId)
		{
			//Get pipeline for job
			NetworkPipeline pipeline = channelId == Channels.Reliable ? reliablePipeline : unreliablePipeline;

			NativeArray<byte> segmentArray = new NativeArray<byte>(segment.Count + CHANNEL_PREFIX_BYTES, Allocator.Persistent);
			segmentArray[0] = (byte)channelId;
			NativeArray<byte>.Copy(segment.Array, segment.Offset, segmentArray, CHANNEL_PREFIX_BYTES, segment.Count);

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

		/// <summary>
		/// Processes connection events from the queue.
		/// </summary>
		public void ProcessIncomingEvents()
		{
			// Exit if the driver is not active or connection isn't ready
			if (!IsNetworkDriverInitialized() || !connection.IsCreated)
			{
				return;
			}

			//Process event queue
			while (connectionsEventsQueue.IsCreated && connectionsEventsQueue.TryDequeue(out UtpConnectionEvent connectionEvent))
			{
				switch (connectionEvent.eventType)
				{
					//Connect action
					case ((byte)UtpConnectionEventType.OnConnected):
						{
							OnConnected?.Invoke();
							break;
						}

					//Receive data action
					case ((byte)UtpConnectionEventType.OnReceivedData):
						{
							byte[] payload = new byte[connectionEvent.payloadLength];
							for (int i = 0; i < connectionEvent.payloadLength; i++)
							{
								payload[i] = connectionEvent.eventData[i];
							}
							OnReceivedData?.Invoke(new ArraySegment<byte>(payload, 0, connectionEvent.payloadLength), connectionEvent.channelId);
							break;
						}

					//Disconnect action
					case ((byte)UtpConnectionEventType.OnDisconnected):
						{
							OnDisconnected?.Invoke();
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
		/// Returns this client's driver's max header size based on the requested channel.
		/// </summary>
		/// <param name="channelId">The channel to check.</param>
		/// <returns>This client's max header size.</returns>
		public int GetMaxHeaderSize(int channelId = Channels.Reliable)
		{
			if (IsConnected() && IsNetworkDriverInitialized())
			{
				return driverMaxHeaderSize[channelId];
			}

			return 0;
		}

		/// <summary>
		/// Whether or not the client is connected to a server.
		/// </summary>
		/// <returns>True if connected to a server, false otherwise.</returns>
		public bool IsConnected()
		{
			return isConnected;
		}

		/// <summary>
		/// Caches important properties to allow for getter methods to be called without interfering with the job system.
		/// </summary>
		private void cacheConnectionInfo()
		{
			//Check for an active connection from this client
			if (IsValidConnection(connection))
			{
				bool isInitialized = IsNetworkDriverInitialized();

				//If driver is active, cache its max header size for UTP transport
				if (isInitialized)
				{
					driverMaxHeaderSize[Channels.Reliable] = driver.MaxHeaderSize(reliablePipeline);
					driverMaxHeaderSize[Channels.Unreliable] = driver.MaxHeaderSize(unreliablePipeline);
				}

				//Set connection state
				isConnected = isInitialized && connection.GetState(driver) == Unity.Networking.Transport.NetworkConnection.State.Connected;
			}
			else
			{
				//If there is no valid connection, set values accordingly
				driverMaxHeaderSize[Channels.Reliable] = 0;
				driverMaxHeaderSize[Channels.Unreliable] = 0;
				isConnected = false;
			}
		}
	}
}
