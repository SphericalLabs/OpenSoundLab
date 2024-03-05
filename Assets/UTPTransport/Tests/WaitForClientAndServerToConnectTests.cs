using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;

namespace Utp
{
	public class WaitForClientAndServerToConnectTests
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
		public IEnumerator IEnumerator_ClientDoesNotConnectToServer_ResultIsTimedOut()
		{
			var waitForConnection = new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 5f);

			yield return waitForConnection;

			Assert.That(waitForConnection.Result, Is.EqualTo(WaitForClientAndServerToConnect.Status.TimedOut));
		}

		[UnityTest]
		public IEnumerator IEnumerator_ClientConnectsToServer_ResultIsClientConnected()
		{
			var waitForConnection = new WaitForClientAndServerToConnect(client: _client, server: _server, timeoutInSeconds: 30f);

			_server.Start(port: 7777);
			_client.Connect(address: "localhost", port: 7777);
			yield return waitForConnection;

			Assert.That(waitForConnection.Result, Is.EqualTo(WaitForClientAndServerToConnect.Status.ClientConnected));
		}
	}
}
