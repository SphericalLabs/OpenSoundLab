using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using Utp;
using System.Collections.Generic;

namespace Utp
{
    public class RelayUtilsTest
    {

        //Test connection types
        static RelayServerEndpoint.NetworkOptions[] connectionTypes = new RelayServerEndpoint.NetworkOptions[]
        {
            RelayServerEndpoint.NetworkOptions.Udp,
            RelayServerEndpoint.NetworkOptions.Tcp
        };

        #region HostRelayData

        /// <summary>
        /// Tests the HostRelayData call inside RelayUtils.
        /// </summary>
        /// <param name="connectionType">Udp/Tcp connection option.</param>
        [Test]
        public void HostRelayData_NormalConnectionState_ReturnsRelayServerData([ValueSource(nameof(connectionTypes))]RelayServerEndpoint.NetworkOptions connectionType)
        {
            //Get connection type as string
            string connectionTypeString = "invalid";
            if (connectionType == RelayServerEndpoint.NetworkOptions.Udp || connectionType == RelayServerEndpoint.NetworkOptions.Tcp)
            {
                connectionTypeString = connectionType == RelayServerEndpoint.NetworkOptions.Udp ? "udp" : "tcp";
            }

            //Create dummy data to inject into temporary relay allocation
            RelayServer relayServer = new RelayServer("0.0.0.0", 0000);
            System.Guid allocationId = new System.Guid("00000000-0000-0000-0000-000000000000");
            List<RelayServerEndpoint> serverEndpoints = new List<RelayServerEndpoint>();
            serverEndpoints.Add(new RelayServerEndpoint(connectionTypeString, connectionType, false, false, "0.0.0.0", 00000));
            byte[] key = new byte[16];
            byte[] connectionData = new byte[16];
            byte[] allocationIdBytes = new byte[16];
            string region = string.Empty;

            Allocation allocation = new Allocation(
                allocationId,
                serverEndpoints,
                relayServer,
                key,
                connectionData,
                allocationIdBytes,
                region
            );

            //Assert data against null/default data
            RelayServerData data = RelayUtils.HostRelayData(allocation, connectionType);
            Assert.AreNotEqual(data, default(RelayServerData));
        }

        /// <summary>
        /// Tests that null allocation data throws a null exception inside HostRelayData.
        /// </summary>
        /// <param name="connectionType">Udp/Tcp connection option.</param>
        [Test]
        public void HostRelayData_WithNullAllocation_ThrowsNullReferenceException([ValueSource(nameof(connectionTypes))] RelayServerEndpoint.NetworkOptions connectionType)
        {
            //Create null allocation & Assert null
            Allocation allocation = null;

            Assert.Throws<System.NullReferenceException>(() =>
            {
                RelayServerData data = RelayUtils.HostRelayData(allocation, connectionType);
            });
        }

        /// <summary>
        /// Tests that a bad connection type in HostRelayData will throw an argument exception.
        /// </summary>
        [Test]
        public void HostRelayData_WithInvalidConnectionType_ThrowsArgumentException()
        {
            //Create dummy data to inject into temporary relay allocation
            RelayServer relayServer = new RelayServer("0.0.0.0", 0000);
            System.Guid allocationId = new System.Guid("00000000-0000-0000-0000-000000000000");
            List<RelayServerEndpoint> serverEndpoints = new List<RelayServerEndpoint>();
            serverEndpoints.Add(new RelayServerEndpoint("invalidConnType", (RelayServerEndpoint.NetworkOptions)9, false, false, "0.0.0.0", 00000));
            byte[] key = new byte[16];
            byte[] connectionData = new byte[16];
            byte[] allocationIdBytes = new byte[16];
            string region = string.Empty;

            Allocation allocation = new Allocation(
                allocationId,
                serverEndpoints,
                relayServer,
                key,
                connectionData,
                allocationIdBytes,
                region
            );

            //Assert exception thrown
            Assert.Throws<System.ArgumentException>(() =>
            {
                RelayServerData data = RelayUtils.HostRelayData(allocation, (RelayServerEndpoint.NetworkOptions)99);
            });
        }

        /// <summary>
        /// Tests that the endpoints list inside HostRelayData's allocation must have entries.
        /// </summary>
        /// <param name="connectionType">Udp/Tcp connection option.</param>
        [Test]
        public void HostRelayData_WithEmptyEndpointsInAllocation_ThrowsArgumentException([ValueSource(nameof(connectionTypes))] RelayServerEndpoint.NetworkOptions connectionType)
        {
            //Create dummy data to inject into temporary relay allocation
            RelayServer relayServer = new RelayServer("0.0.0.0", 0000);
            System.Guid allocationId = new System.Guid("00000000-0000-0000-0000-000000000000");
            List<RelayServerEndpoint> serverEndpoints = new List<RelayServerEndpoint>();
            byte[] key = new byte[16];
            byte[] connectionData = new byte[16];
            byte[] allocationIdBytes = new byte[16];
            string region = string.Empty;

            Allocation allocation = new Allocation(
                allocationId,
                serverEndpoints,
                relayServer,
                key,
                connectionData,
                allocationIdBytes,
                region
            );

            //Assert exception thrown
            Assert.Throws<System.ArgumentException>(() =>
            {
                RelayServerData data = RelayUtils.HostRelayData(allocation, connectionType);
            });
        }

        #endregion

        #region PlayerRelayData

        /// <summary>
        /// Tests the PlayerRelayData call inside RelayUtils.
        /// </summary>
        /// <param name="connectionType">Udp/Tcp connection option.</param>
        [Test]
        public void PlayerRelayData_NormalConnectionState_ReturnsRelayServerData([ValueSource(nameof(connectionTypes))] RelayServerEndpoint.NetworkOptions connectionType)
        {
            //Get connection type as string
            string connectionTypeString = "invalid";
            if (connectionType == RelayServerEndpoint.NetworkOptions.Udp || connectionType == RelayServerEndpoint.NetworkOptions.Tcp)
            {
                connectionTypeString = connectionType == RelayServerEndpoint.NetworkOptions.Udp ? "udp" : "tcp";
            }

            //Create dummy data to inject into temporary relay allocation
            RelayServer relayServer = new RelayServer("0.0.0.0", 0000);
            System.Guid allocationId = new System.Guid("00000000-0000-0000-0000-000000000000");
            List<RelayServerEndpoint> serverEndpoints = new List<RelayServerEndpoint>();
            serverEndpoints.Add(new RelayServerEndpoint(connectionTypeString, connectionType, false, false, "0.0.0.0", 00000));
            byte[] key = new byte[16];
            byte[] connectionData = new byte[16];
            byte[] allocationIdBytes = new byte[16];
            string region = string.Empty;
            byte[] hostConnectionData = new byte[16];

            JoinAllocation allocation = new JoinAllocation(
                allocationId,
                serverEndpoints,
                relayServer,
                key,
                connectionData,
                allocationIdBytes,
                region,
                hostConnectionData
            );

            //Assert data against null/default data
            RelayServerData data = RelayUtils.PlayerRelayData(allocation, connectionType);
            Assert.AreNotEqual(data, default(RelayServerData));
        }

        /// <summary>
        /// Tests that null allocation data throws a null exception inside PlayerRelayData.
        /// </summary>
        /// <param name="connectionType">Udp/Tcp connection option.</param>
        [Test]
        public void PlayerRelayData_WithNullAllocation_ThrowsNullReferenceException([ValueSource(nameof(connectionTypes))] RelayServerEndpoint.NetworkOptions connectionType)
        {
            //Create null allocation & Assert null
            JoinAllocation allocation = null;

            Assert.Throws<System.NullReferenceException>(() =>
            {
                RelayServerData data = RelayUtils.PlayerRelayData(allocation, connectionType);
            });
        }

        /// <summary>
        /// Tests that a bad connection type in HostRelayData will throw an argument exception.
        /// </summary>
        [Test]
        public void PlayerRelayData_WithInvalidConnectionType_ThrowsArgumentException()
        {
            //Create dummy data to inject into temporary relay allocation
            RelayServer relayServer = new RelayServer("0.0.0.0", 0000);
            System.Guid allocationId = new System.Guid("00000000-0000-0000-0000-000000000000");
            List<RelayServerEndpoint> serverEndpoints = new List<RelayServerEndpoint>();
            serverEndpoints.Add(new RelayServerEndpoint("invalidConnType", (RelayServerEndpoint.NetworkOptions)99, false, false, "0.0.0.0", 00000));
            byte[] key = new byte[16];
            byte[] connectionData = new byte[16];
            byte[] allocationIdBytes = new byte[16];
            string region = string.Empty;
            byte[] hostConnectionData = new byte[16];

            JoinAllocation allocation = new JoinAllocation(
                allocationId,
                serverEndpoints,
                relayServer,
                key,
                connectionData,
                allocationIdBytes,
                region,
                hostConnectionData
            );

            //Assert exception thrown
            Assert.Throws<System.ArgumentException>(() =>
            {
                RelayServerData data = RelayUtils.PlayerRelayData(allocation, (RelayServerEndpoint.NetworkOptions)99);
            });
        }

        /// <summary>
        /// Tests that the endpoints list inside PlayerRelayData's allocation must have entries.
        /// </summary>
        /// <param name="connectionType">Udp/Tcp connection option.</param>
        [Test]
        public void PlayerRelayData_WithEmptyEndpointsInAllocation_ThrowsArgumentException([ValueSource(nameof(connectionTypes))] RelayServerEndpoint.NetworkOptions connectionType)
        {
            //Create dummy data to inject into temporary relay allocation
            RelayServer relayServer = new RelayServer("0.0.0.0", 0000);
            System.Guid allocationId = new System.Guid("00000000-0000-0000-0000-000000000000");
            List<RelayServerEndpoint> serverEndpoints = new List<RelayServerEndpoint>();
            byte[] key = new byte[16];
            byte[] connectionData = new byte[16];
            byte[] allocationIdBytes = new byte[16];
            string region = string.Empty;
            byte[] hostConnectionData = new byte[16];

            JoinAllocation allocation = new JoinAllocation(
                allocationId,
                serverEndpoints,
                relayServer,
                key,
                connectionData,
                allocationIdBytes,
                region,
                hostConnectionData
            );

            //Assert exception thrown
            Assert.Throws<System.ArgumentException>(() =>
            {
                RelayServerData data = RelayUtils.PlayerRelayData(allocation, connectionType);
            });
        }

        #endregion

    }

}