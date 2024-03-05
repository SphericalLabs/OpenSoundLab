using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Utp
{
    public class UtpServerTests
    {
        private UtpServer _server;
        private UtpClient _client;

        [SetUp]
        public void SetUp()
        {
            _server = new UtpServer(timeoutInMilliseconds: 1000);
            _client = new UtpClient(timeoutInMilliseconds: 1000);
        }

        [TearDown]
        public void TearDown()
        {
            _client.Disconnect();
            _server.Stop();
        }

        [Test]
        public void IsActive_ServerWasNotStarted_ReturnsFalse()
        {
            Assert.IsFalse(_server.IsActive(), "Server is active without being started.");
        }

        [Test]
        public void IsActive_ServerWasStarted_ReturnsTrue()
        {
            _server.Start(7777);

            Assert.IsTrue(_server.IsActive(), "Server did not start and is not active.");
        }

        [Test]
        public void GetClientAddress_ConnectionIdOfNonExistentClient_ReturnsEmptyString()
        {
            int connectionIdOfNonExistentClient = 1;

            string clientAddress = _server.GetClientAddress(connectionIdOfNonExistentClient);

            Assert.IsEmpty(clientAddress, "Client address was not empty.");
        }

        [UnityTest]
        public IEnumerator GetClientAddress_ConnectionIdOfConnectedClient_ReturnsClientAddress()
        {
            int idOfConnectedClient = 1;
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);

            string clientAddress = _server.GetClientAddress(idOfConnectedClient);

            Assert.IsNotEmpty(clientAddress, "Client address was empty, indicating the client is probably not connected.");
        }

        [Test]
        public void Disconnect_ConnectionIdOfNonExistentClient_LogsWarning()
        {
            int connectionIdOfNonExistentClient = 1;

            _server.Disconnect(connectionId: connectionIdOfNonExistentClient);

            LogAssert.Expect(LogType.Warning, "Connection not found: 1");
        }

        [UnityTest]
        public IEnumerator Disconnect_ConnectionIdOfConnectedClient_ClientIsDisconnected()
        {
            int connectionIdOfConnectedClient = 1;
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);

            _server.Disconnect(connectionId: connectionIdOfConnectedClient);
            yield return new WaitForClientAndServerToDisconnect(client: _client, server: _server, timeoutInSeconds: 10f);
            
            Assert.IsFalse(_client.IsConnected(), "Client was not successfully disconnected from server");
        }

        [Test]
        public void FindConnection_ConnectionIdOfNonExistentClient_ReturnsDefaultNetworkConnection()
        {
            int connectionIdOfNonExistentClient = 1;

            Unity.Networking.Transport.NetworkConnection connection = _server.FindConnection(connectionIdOfNonExistentClient);

            Assert.That(connection, Is.EqualTo(default(Unity.Networking.Transport.NetworkConnection)));
        }

        [UnityTest]
        public IEnumerator FindConnection_ConnectionIdOfConnectedClient_ReturnsNetworkConnection()
        {
            int connectionIdOfConnectedClient = 1;
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);

            Unity.Networking.Transport.NetworkConnection connection = _server.FindConnection(connectionIdOfConnectedClient);

            Assert.That(connection, Is.Not.EqualTo(default(Unity.Networking.Transport.NetworkConnection)));
        }

        [UnityTest]
        public IEnumerator OnConnected_ClientConnectsToServer_CallbackIsInvoked()
        {
            bool callbackWasInvoked = false;
            _server.OnConnected += (int connectionId) => { callbackWasInvoked = true; };
            _server.Start(7777);

            _client.Connect("localhost", 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);

            Assert.That(callbackWasInvoked, Is.True);
        }

        [UnityTest]
        public IEnumerator OnDisconnect_ServerDisconnectsClient_CallbackIsInvoked()
        {
            bool callbackWasInvoked = false;
            _server.OnDisconnected += (int connectionId) => { callbackWasInvoked = true; };
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);

            int idOfConnectedClient = 1;
            _server.Disconnect(idOfConnectedClient);
            yield return new WaitForClientAndServerToDisconnect(client: _client, server: _server, timeoutInSeconds: 30f);

            Assert.That(callbackWasInvoked, Is.True);
        }

        [UnityTest]
        public IEnumerator OnReceivedData_ClientSendsDataToServer_CallbackIsInvoked()
        {
            bool callbackWasInvoked = false;
            _server.OnReceivedData += (connectionId, segment) => { callbackWasInvoked = true; };
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);

            int validChannelId = 1;
            var dummyDataToSend = new ArraySegment<byte>(new byte[4]);
            _client.Send(segment: dummyDataToSend, channelId: validChannelId);
            yield return tickClientAndServerForSeconds(client: _client, server: _server, numSeconds: 5f);

            Assert.That(callbackWasInvoked, Is.True);
        }

        private IEnumerator tickClientAndServerForSeconds(UtpClient client, UtpServer server, float numSeconds)
        {
            float elapsedTime = 0f;
            while (elapsedTime < numSeconds)
            {
                client.Tick();
                server.Tick();
                yield return null;
                elapsedTime += Time.deltaTime;
            }
        }
    }
}
