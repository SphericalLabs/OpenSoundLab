using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;

namespace Utp
{
	public class WaitForClientAndServerToDisconnectTests
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

		[UnityTest]
		public IEnumerator IEnumerator_ClientIsNotConnectedToServer_ResultIsClientDisconnected()
		{
			var waitForDisconnect = new WaitForClientAndServerToDisconnect(client: _client, server: _server, timeoutInSeconds: 5f);

			yield return waitForDisconnect;

			Assert.That(waitForDisconnect.Result, Is.EqualTo(WaitForClientAndServerToDisconnect.Status.ClientDisconnected));
		}

		[UnityTest]
		public IEnumerator IEnumerator_ClientRemainsConnectedToTheServer_ResultIsTimedOut()
		{
			var waitForDisconnect = new WaitForClientAndServerToDisconnect(client: _client, server: _server, timeoutInSeconds: 5f);
			_server.Start(port: 7777);
			_client.Connect(address: "localhost", port: 7777);
			yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);

			yield return waitForDisconnect;

			Assert.That(waitForDisconnect.Result, Is.EqualTo(WaitForClientAndServerToDisconnect.Status.TimedOut));
		}

		[UnityTest]
		public IEnumerator IEnumerator_ServerDisconnectsClient_ResultIsClientDisconnected()
		{
			var waitForDisconnect = new WaitForClientAndServerToDisconnect(client: _client, server: _server, timeoutInSeconds: 30f);
			_server.Start(port: 7777);
			_client.Connect(address: "localhost", port: 7777);
			yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);

			_server.Disconnect(connectionId: 1);
			yield return waitForDisconnect;

			Assert.That(waitForDisconnect.Result, Is.EqualTo(WaitForClientAndServerToDisconnect.Status.ClientDisconnected));
		}

		[UnityTest]
		public IEnumerator IEnumerator_ClientDisconnectsFromServer_ResultIsClientDisconnected()
		{
			var waitForDisconnect = new WaitForClientAndServerToDisconnect(client: _client, server: _server, timeoutInSeconds: 30f);
			_server.Start(port: 7777);
			_client.Connect(address: "localhost", port: 7777);
			yield return new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);

			_client.Disconnect();
			yield return waitForDisconnect;

			Assert.That(waitForDisconnect.Result, Is.EqualTo(WaitForClientAndServerToDisconnect.Status.ClientDisconnected));
		}
	}
}
