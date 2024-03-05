using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Utp
{
    public class UtpClientTests
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
        public void IsConnected_ClientIsNotConnectedToServer_ReturnsFalse()
        {
            Assert.That(_client.IsConnected(), Is.False);
        }

        [Test]
        public void IsConnected_ClientTriesToConnectToNonExistentServer_ReturnsFalse()
        {
            _client.Connect("localhost", 7777);
            Assert.That(_client.IsConnected(), Is.False);
        }

        [UnityTest]
        public IEnumerator IsConnected_ClientIsConnectedToServer_ReturnsTrue()
        {
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);

            Assert.That(_client.IsConnected(), Is.True);
        }

        [UnityTest]
        public IEnumerator OnConnected_ClientConnectsToServer_CallbackIsInvoked()
        {
            bool callbackWasInvoked = false;
            _client.OnConnected += () => { callbackWasInvoked = true; };

            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);

            Assert.That(callbackWasInvoked, Is.True);
        }

        [UnityTest]
        public IEnumerator OnDisconnected_ClientDisconnectsFromServer_CallbackIsInvoked()
        {
            bool callbackWasInvoked = false;
            _client.OnDisconnected += () => { callbackWasInvoked = true; };
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);

            _client.Disconnect();
            yield return new WaitForClientAndServerToDisconnect(client: _client, server: _server, timeoutInSeconds: 30f);

            Assert.That(callbackWasInvoked, Is.True);
        }

        [UnityTest]
        public IEnumerator OnReceivedData_ServerSendsDataToClient_CallbackIsInvoked()
        {
            bool callbackWasInvoked = false;
            _client.OnReceivedData += (segment) => { callbackWasInvoked = true; };
            _server.Start(7777);
            _client.Connect("localhost", 7777);
            yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);

            int clientConnectionId = 1;
            var dummyDataToSend = new ArraySegment<byte>(new byte[4]);
            int validChannelId = 1;
            _server.Send(connectionId: clientConnectionId, segment: dummyDataToSend, channelId: validChannelId);
            yield return tickClientAndServerForSeconds(_client, _server, 5f);

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
