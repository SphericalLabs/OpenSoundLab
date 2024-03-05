using System;
using System.Collections.Generic;

using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;

namespace Utp
{
	public class RelayUtils
	{
		/// <summary>
		/// Construct the ServerData needed to create a RelayNetworkParameter for a host.
		/// </summary>
		/// <param name="allocation">The Allocation for the Relay Server.</param>
		/// <param name="connectionType">The type of connection to the Relay Server.</param>
		/// <returns>The RelayServerData.</returns>
		public static RelayServerData HostRelayData(Allocation allocation, RelayServerEndpoint.NetworkOptions connectionType)
		{
			//Get string from connection
			string connectionTypeString = GetStringFromConnectionType(connectionType);

            if (String.IsNullOrEmpty(connectionTypeString))
            {
				throw new ArgumentException($"ConnectionType {connectionType} is invalid");
			}

			// Select endpoint based on desired connectionType
			var endpoint = GetEndpointForConnectionType(allocation.ServerEndpoints, connectionTypeString);

			if (endpoint == null)
			{
				throw new ArgumentException($"endpoint for connectionType {connectionType} not found");
			}

			// Prepare the server endpoint using the Relay server IP and port
			var serverEndpoint = NetworkEndPoint.Parse(endpoint.Host, (ushort)endpoint.Port);

			// UTP uses pointers instead of managed arrays for performance reasons, so we use these helper functions to convert them
			var allocationIdBytes = ConvertFromAllocationIdBytes(allocation.AllocationIdBytes);
			var connectionData = ConvertConnectionData(allocation.ConnectionData);
			var key = ConvertFromHMAC(allocation.Key);

			// Prepare the Relay server data and compute the nonce value
			// The host passes its connectionData twice into this function
			var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationIdBytes, ref connectionData,
				ref connectionData, ref key, connectionTypeString == "dtls");
			relayServerData.ComputeNewNonce();

			return relayServerData;
		}

		/// <summary>
		/// Construct the ServerData needed to create a RelayNetworkParameter for a player.
		/// </summary>
		/// <param name="allocation">The JoinAllocation for the Relay Server.</param>
		/// <param name="connectionType">The type of connection to the Relay Server.</param>
		/// <returns>The RelayServerData.</returns>
		public static RelayServerData PlayerRelayData(JoinAllocation allocation, RelayServerEndpoint.NetworkOptions connectionType)
		{
			//Get string from connection
			string connectionTypeString = GetStringFromConnectionType(connectionType);

			if (String.IsNullOrEmpty(connectionTypeString))
			{
				throw new ArgumentException($"ConnectionType {connectionType} is invalid");
			}

			// Select endpoint based on desired connectionType
			var endpoint = GetEndpointForConnectionType(allocation.ServerEndpoints, connectionTypeString);

			if (endpoint == null)
			{
				throw new ArgumentException($"endpoint for connectionType {connectionType} not found");
			}

			// Prepare the server endpoint using the Relay server IP and port
			var serverEndpoint = NetworkEndPoint.Parse(endpoint.Host, (ushort)endpoint.Port);

			// UTP uses pointers instead of managed arrays for performance reasons, so we use these helper functions to convert them
			var allocationIdBytes = ConvertFromAllocationIdBytes(allocation.AllocationIdBytes);
			var connectionData = ConvertConnectionData(allocation.ConnectionData);
			var hostConnectionData = ConvertConnectionData(allocation.HostConnectionData);
			var key = ConvertFromHMAC(allocation.Key);

			// Prepare the Relay server data and compute the nonce values
			// A player joining the host passes its own connectionData as well as the host's
			var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationIdBytes, ref connectionData,
				ref hostConnectionData, ref key, connectionTypeString == "dtls");
			relayServerData.ComputeNewNonce();

			return relayServerData;
		}

		/// <summary>
		/// Gets a network type and returns its string alternative.
		/// </summary>
		/// <param name="connectionType">The type of connection to stringify.</param>
		/// <returns>The connection type as a string.</returns>
		private static string GetStringFromConnectionType(RelayServerEndpoint.NetworkOptions connectionType)
        {
			switch(connectionType)
            {
				case (RelayServerEndpoint.NetworkOptions.Tcp): return "tcp";
				case (RelayServerEndpoint.NetworkOptions.Udp): return "udp";
				default: return String.Empty;
			}
        }

		#region Helper Methods

		private static RelayAllocationId ConvertFromAllocationIdBytes(byte[] allocationIdBytes)
		{
			unsafe
			{
				fixed (byte* ptr = allocationIdBytes)
				{
					return RelayAllocationId.FromBytePointer(ptr, allocationIdBytes.Length);
				}
			}
		}

		private static RelayConnectionData ConvertConnectionData(byte[] connectionData)
		{
			unsafe
			{
				fixed (byte* ptr = connectionData)
				{
					return RelayConnectionData.FromBytePointer(ptr, RelayConnectionData.k_Length);
				}
			}
		}

		private static RelayHMACKey ConvertFromHMAC(byte[] hmac)
		{
			unsafe
			{
				fixed (byte* ptr = hmac)
				{
					return RelayHMACKey.FromBytePointer(ptr, RelayHMACKey.k_Length);
				}
			}
		}

		private static RelayServerEndpoint GetEndpointForConnectionType(List<RelayServerEndpoint> endpoints, string connectionType)
		{
			foreach (var endpoint in endpoints)
			{
				if (endpoint.ConnectionType == connectionType)
				{
					return endpoint;
				}
			}

			return null;
		}

		#endregion
	}
}
