using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace Utp
{
	public class RelayManagerTests
	{
		private RelayManager _relayManager;

		[SetUp]
		public void SetUp()
		{
			var obj = new GameObject();
			_relayManager = obj.AddComponent<RelayManager>();
		}

		[TearDown]
		public void TearDown()
		{
			_relayManager.OnRelayServerAllocated = null;

			GameObject.Destroy(_relayManager.gameObject);
		}

		[Test]
		public void GetAllocationFromJoinCode_FaultedTask_InvokesOnFailure()
		{
			_relayManager.RelayServiceSDK = new TaskAlwaysFaults();

			bool didInvokeOnSuccess = false;
			bool didInvokeOnFailure = false;

			string validJoinCode = "test";
			Action onSuccess = () => { didInvokeOnSuccess = true; };
			Action onFailure = () => { didInvokeOnFailure = true; };
			_relayManager.GetAllocationFromJoinCode(joinCode: validJoinCode, onSuccess: onSuccess, onFailure: onFailure);

			LogAssert.Expect(LogType.Error, new Regex(@"Unable to get Relay allocation from join code, encountered an error:"));
			Assert.That(didInvokeOnSuccess, Is.False);
			Assert.That(didInvokeOnFailure, Is.True);
		}

		[Test]
		public void GetAllocationFromJoinCode_CompletedTask_InvokesOnSuccess()
		{
			_relayManager.RelayServiceSDK = new TaskAlwaysCompletes();

			bool didInvokeOnSuccess = false;
			bool didInvokeOnFailure = false;

			string validJoinCode = "test";
			Action onSuccess = () => { didInvokeOnSuccess = true; };
			Action onFailure = () => { didInvokeOnFailure = true; };
			_relayManager.GetAllocationFromJoinCode(joinCode: validJoinCode, onSuccess: onSuccess, onFailure: onFailure);

			Assert.That(didInvokeOnSuccess, Is.True);
			Assert.That(didInvokeOnFailure, Is.False);
		}

		[Test]
		public void GetRelayRegions_FaultedTask_InvokesOnFailure()
		{
			_relayManager.RelayServiceSDK = new TaskAlwaysFaults();

			bool didInvokeOnSucess = false;
			bool didInvokeOnFailure = false;
			Action<List<Region>> onSuccess = (regions) => { didInvokeOnSucess = true; };
			Action onFailure = () => { didInvokeOnFailure = true; };
			_relayManager.GetRelayRegions(onSuccess: onSuccess, onFailure: onFailure);

			LogAssert.Expect(LogType.Error, new Regex(@"Unable to retrieve the list of Relay regions, encountered an error:"));
			Assert.That(didInvokeOnSucess, Is.False);
			Assert.That(didInvokeOnFailure, Is.True);
		}

		[Test]
		public void GetRelayRegions_CompletedTask_InvokesOnSuccess()
		{
			_relayManager.RelayServiceSDK = new TaskAlwaysCompletes();

			bool didInvokeOnSucess = false;
			bool didInvokeOnFailure = false;
			Action<List<Region>> onSuccess = (regions) => { didInvokeOnSucess = true; };
			Action onFailure = () => { didInvokeOnFailure = true; };
			_relayManager.GetRelayRegions(onSuccess: onSuccess, onFailure: onFailure);

			Assert.That(didInvokeOnSucess, Is.True);
			Assert.That(didInvokeOnFailure, Is.False);
		}

		[Test]
		public void AllocateRelayServer_FaultedTask_InvokesOnFailure()
		{
			_relayManager.RelayServiceSDK = new TaskAlwaysFaults();

			bool didInvokeOnSuccess = false;
			bool didInvokeOnFailure = false;

			int validMaxPlayers = 8;
			string validRegionId = "test";
			Action<string> onSuccess = (code) => { didInvokeOnSuccess = true; };
			Action onFailure = () => { didInvokeOnFailure = true; };
			_relayManager.AllocateRelayServer(maxPlayers: validMaxPlayers, regionId: validRegionId, onSuccess: onSuccess, onFailure: onFailure);

			LogAssert.Expect(LogType.Error, new Regex(@"Unable to allocate Relay server, encountered an error creating a Relay allocation:"));
			Assert.That(_relayManager.ServerAllocation, Is.Null);
			Assert.That(didInvokeOnSuccess, Is.False);
			Assert.That(didInvokeOnFailure, Is.True);
		}

		[Test]
		public void AllocateRelayServer_CompletedTask_InvokesOnSuccess()
		{
			_relayManager.RelayServiceSDK = new TaskAlwaysCompletes();

			bool didInvokeOnSuccess = false;
			bool didInvokeOnFailure = false;

			int validMaxPlayers = 8;
			string validRegionId = "test";
			Action<string> onSuccess = (code) => { didInvokeOnSuccess = true; };
			Action onFailure = () => { didInvokeOnFailure = true; };
			_relayManager.AllocateRelayServer(maxPlayers: validMaxPlayers, regionId: validRegionId, onSuccess: onSuccess, onFailure: onFailure);

			Assert.That(_relayManager.ServerAllocation, Is.Not.Null);
			Assert.That(didInvokeOnSuccess, Is.True);
			Assert.That(didInvokeOnFailure, Is.False);
		}

		private class TaskAlwaysCompletes : IRelayServiceSDK
		{
			public Task<Allocation> CreateAllocationAsync(int maxConnections, string region = null)
			{
				byte[] resultKey = new byte[4];
				byte[] resultConnectionData = new byte[4];
				byte[] resultAllocationIdBytes = new byte[4];
				Guid resultAllocationId = new Guid();
				List<RelayServerEndpoint> resultEndpointList = new List<RelayServerEndpoint>();
				string localHostIp = "127.0.0.1";
				ushort samplePort = 12345;
				RelayServer resultRelayServer = new RelayServer(ipV4: localHostIp, port: samplePort);
				return Task.FromResult<Allocation>(
					result: new Allocation(
						allocationId: resultAllocationId,
						serverEndpoints: resultEndpointList,
						relayServer: resultRelayServer,
						key: resultKey,
						connectionData: resultConnectionData,
						allocationIdBytes: resultAllocationIdBytes,
						region: region
					)
				);
			}

			public Task<string> GetJoinCodeAsync(Guid allocationId)
			{
				string validJoinCode = "test";
				return Task.FromResult<string>(validJoinCode);
			}

			public Task<JoinAllocation> JoinAllocationAsync(string joinCode)
			{
				Guid joinAllocationAllocationId = new Guid();
				List<RelayServerEndpoint> joinAllocationEndpointList = new List<RelayServerEndpoint>();
				string localHostIp = "127.0.0.1";
				ushort samplePort = 12345;
				byte[] joinAllocationKey = new byte[4];
				byte[] joinAllocationHostConnectionData = new byte[4];
				byte[] joinAllocationConnectionData = new byte[4];
				string joinAllocationRegion = string.Empty;
				byte[] joinAllocationAllocationIdBytes = new byte[4];
				RelayServer joinAllocationRelayServer = new RelayServer(ipV4: localHostIp, port: samplePort);
				return Task.FromResult<JoinAllocation>(
					result: new JoinAllocation(
						allocationId: joinAllocationAllocationId,
						serverEndpoints: joinAllocationEndpointList,
						relayServer: joinAllocationRelayServer,
						key: joinAllocationKey,
						hostConnectionData: joinAllocationHostConnectionData,
						connectionData: joinAllocationConnectionData,
						region: joinAllocationRegion,
						allocationIdBytes: joinAllocationAllocationIdBytes
					)
				);
			}

			public Task<List<Region>> ListRegionsAsync()
			{
				List<Region> regionList = new List<Region>();
				Region validRegion = new Region(id: "valid-region", description: "test");
				regionList.Add(validRegion);
				return Task.FromResult<List<Region>>(
					result: regionList
				);
			}
		}

		private class TaskAlwaysFaults : IRelayServiceSDK
		{
			public Task<Allocation> CreateAllocationAsync(int maxConnections, string region = null)
			{
				return Task.FromException<Allocation>(new Exception("Task faulted!"));
			}

			public Task<string> GetJoinCodeAsync(Guid allocationId)
			{
				return Task.FromException<string>(new Exception("Task faulted!"));
			}

			public Task<JoinAllocation> JoinAllocationAsync(string joinCode)
			{
				return Task.FromException<JoinAllocation>(new Exception("Task faulted!"));
			}

			public Task<List<Region>> ListRegionsAsync()
			{
				return Task.FromException<List<Region>>(new Exception("Task faulted!"));
			}
		}
	}
}
