using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Utp
{
    public class UtpTransportTests
    {
        private UtpTransport _server;
        private UtpTransport _client;

        [SetUp]
        public void SetUp()
        {
            var serverObj = new GameObject();
            _server = serverObj.AddComponent<UtpTransport>();

            var clientObj = new GameObject();
            _client = clientObj.AddComponent<UtpTransport>();
        }

        [TearDown]
        public void TearDown()
        {
            _client.ClientDisconnect();
            GameObject.Destroy(_client.gameObject);

            _server.ServerStop();
            GameObject.Destroy(_server.gameObject);
        }

        [Test]
        public void ServerActive_ServerIsNotActive_ReturnsFalse()
        {
            Assert.IsFalse(_server.ServerActive(), "Server is running, but should not be.");
        }

        [Test]
        public void ServerActive_ServerIsActive_ReturnsTrue()
        {
            _server.ServerStart();

            Assert.IsTrue(_server.ServerActive(), "Server is not running, but should be.");
        }

        [Test]
        public void ServerStop_ServerIsActive_ServerIsNoLongerActive()
        {
            _server.ServerStart();

            _server.ServerStop();

            Assert.IsFalse(_server.ServerActive(), "Server is running, but should not be.");
        }

        [Test]
        public void ServerGetClientAddress_ConnectionIdOfNonExistentClient_ReturnsEmptyString()
        {
            int connectionIdForNonExistentClient = 1;

            string clientAddress = _server.ServerGetClientAddress(connectionId: connectionIdForNonExistentClient);

            Assert.IsEmpty(clientAddress, "A client address was returned instead of an empty string.");
        }

        [UnityTest]
        public IEnumerator ServerGetClientAddress_ConnectionIdOfConnectedClient_ReturnsClientAddress()
        {
            int connectionIdForConnectedClient = 1;
            _server.ServerStart();
            _client.ClientConnect(_server.ServerUri());
            yield return new WaitForTransportToConnect(client: _client, server: _server, timeoutInSeconds: 30f);

            string clientAddress = _server.ServerGetClientAddress(connectionId: connectionIdForConnectedClient);

            Assert.IsNotEmpty(clientAddress, "A client address was not returned, connection possibly timed out..");
        }

        [UnityTest]
        public IEnumerator ServerGetClientAddress_ConnectionIdOfDisconnectedClient_ReturnsEmptyString()
        {
            int connectionIdOfDisconnectedClient = 1;
            _server.ServerStart();
            _client.ClientConnect(_server.ServerUri());
            yield return new WaitForTransportToConnect(client: _client, server: _server, timeoutInSeconds: 30f);
            _client.ClientDisconnect();

            string clientAddress = _server.ServerGetClientAddress(connectionId: connectionIdOfDisconnectedClient);

            Assert.IsNotEmpty(clientAddress, "A client address was not returned, connection possibly timed out..");
        }

        [Test]
        public void ClientConnected_ClientIsNotConnectedToServer_ReturnsFalse()
        {
            Assert.IsFalse(_client.ClientConnected(), "Client is connected, but should not be.");
        }

        [UnityTest]
        public IEnumerator ClientConnected_ClientIsConnectedToServer_ReturnsTrue()
        {
            _server.ServerStart();
            _client.ClientConnect(_server.ServerUri());
            yield return new WaitForTransportToConnect(client: _client, server: _server, timeoutInSeconds: 30f);

            Assert.IsTrue(_client.ClientConnected(), "Client is not connected, but should be.");
        }
    }
}
