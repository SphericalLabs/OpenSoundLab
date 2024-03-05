using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace Utp
{
	public class RelayManager : MonoBehaviour, IRelayManager
	{
		/// <summary>
		/// The allocation managed by a host who is running as a client and server.
		/// </summary>
		public Allocation ServerAllocation { get; set; }

		/// <summary>
		/// The allocation managed by a client who is connecting to a server.
		/// </summary>
		public JoinAllocation JoinAllocation { get; set; }

		/// <summary>
		/// A callback for when a Relay server is allocated and a join code is fetched.
		/// </summary>
		public Action<string, string> OnRelayServerAllocated { get; set; }

		/// <summary>
		/// The interface to the Relay services API.
		/// </summary>
		public IRelayServiceSDK RelayServiceSDK { get; set; } = new WrappedRelayServiceSDK();

		private void Awake()
		{
			UtpLog.Info("RelayManager initialized");
		}

		/// <summary>
		/// Retrieve the <seealso cref="Unity.Services.Relay.Models.JoinAllocation"/> corresponding to the specified join code.
		/// </summary>
		/// <param name="joinCode">The join code that will be used to retrieve the JoinAllocation.</param>
		/// <param name="onSuccess">A callback to invoke when the Relay allocation is successfully retrieved from the join code.</param>
		/// <param name="onFailure">A callback to invoke when the Relay allocation is unsuccessfully retrieved from the join code.</param>
		public void GetAllocationFromJoinCode(string joinCode, Action onSuccess, Action onFailure)
		{
			StartCoroutine(GetAllocationFromJoinCodeTask(joinCode, onSuccess, onFailure));
		}

		private IEnumerator GetAllocationFromJoinCodeTask(string joinCode, Action onSuccess, Action onFailure)
		{
			Task<JoinAllocation> joinAllocation = RelayServiceSDK.JoinAllocationAsync(joinCode);

			while (!joinAllocation.IsCompleted)
			{
				yield return null;
			}

			if (joinAllocation.IsFaulted)
			{
				joinAllocation.Exception.Flatten().Handle((Exception err) =>
				{
					UtpLog.Error($"Unable to get Relay allocation from join code, encountered an error: {err.Message}.");

					return true;
				});

				onFailure?.Invoke();

				yield break;
			}

			JoinAllocation = joinAllocation.Result;

			onSuccess?.Invoke();
		}

		/// <summary>
		/// Get a list of Regions from the Relay Service.
		/// </summary>
		/// <param name="onSuccess">A callback to invoke when the list of regions is successfully retrieved.</param>
		/// <param name="onFailure">A callback to invoke when the list of regions is unsuccessfully retrieved.</param>
		public void GetRelayRegions(Action<List<Region>> onSuccess, Action onFailure)
		{
			StartCoroutine(GetRelayRegionsTask(onSuccess, onFailure));
		}

		private IEnumerator GetRelayRegionsTask(Action<List<Region>> onSuccess, Action onFailure)
		{
			Task<List<Region>> listRegions = RelayServiceSDK.ListRegionsAsync();

			while (!listRegions.IsCompleted)
			{
				yield return null;
			}

			if (listRegions.IsFaulted)
			{
				listRegions.Exception.Flatten().Handle((Exception err) =>
				{
					UtpLog.Error($"Unable to retrieve the list of Relay regions, encountered an error: {err.Message}.");
					return true;
				});

				onFailure?.Invoke();

				yield break;
			}

			onSuccess?.Invoke(listRegions.Result);
		}

		/// <summary>
		/// Allocate a Relay Server.
		/// </summary>
		/// <param name="maxPlayers">The max number of players that may connect to this server.</param>
		/// <param name="regionId">The region to allocate the server in. May be null.</param>
		/// <param name="onSuccess">A callback to invoke when the Relay server is successfully allocated.</param>
		/// <param name="onFailure">A callback to invoke when the Relay server is unsuccessfully allocated.</param>
		public void AllocateRelayServer(int maxPlayers, string regionId, Action<string> onSuccess, Action onFailure)
		{
			StartCoroutine(AllocateRelayServerTask(maxPlayers, regionId, onSuccess, onFailure));
		}

		private IEnumerator AllocateRelayServerTask(int maxPlayers, string regionId, Action<string> onSuccess, Action onFailure)
		{
			Task<Allocation> createAllocation = RelayServiceSDK.CreateAllocationAsync(maxPlayers, regionId);

			while (!createAllocation.IsCompleted)
			{
				yield return null;
			}

			if (createAllocation.IsFaulted)
			{
				createAllocation.Exception.Flatten().Handle((Exception err) =>
				{
					UtpLog.Error($"Unable to allocate Relay server, encountered an error creating a Relay allocation: {err.Message}.");
					return true;
				});

				onFailure?.Invoke();

				yield break;
			}

			ServerAllocation = createAllocation.Result;

			UtpLog.Verbose($"Received allocation: {ServerAllocation.AllocationId}");

			StartCoroutine(GetJoinCodeTask(onSuccess, onFailure));
		}

		private IEnumerator GetJoinCodeTask(Action<string> onSuccess, Action onFailure)
		{
			Task<string> getJoinCode = RelayServiceSDK.GetJoinCodeAsync(ServerAllocation.AllocationId);

			while (!getJoinCode.IsCompleted)
			{
				yield return null;
			}

			if (getJoinCode.IsFaulted)
			{
				getJoinCode.Exception.Flatten().Handle((Exception err) =>
				{
					UtpLog.Error($"Unable to allocate Relay server, encountered an error retrieving the join code: {err.Message}.");
					return true;
				});

				onFailure?.Invoke();

				yield break;
			}

			onSuccess?.Invoke(getJoinCode.Result);
		}
	}
}