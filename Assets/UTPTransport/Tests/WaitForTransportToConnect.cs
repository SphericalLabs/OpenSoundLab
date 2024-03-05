using System.Collections;
using UnityEngine;

namespace Utp
{
    internal class WaitForTransportToConnect : IEnumerator
    {
        public enum Status
        {
            Undetermined,
            ClientConnected,
            TimedOut,
        }

        public Status Result { get; private set; } = Status.Undetermined;

        public object Current => null;

        private float _elapsedTime = 0f;
        private float _timeout = 0f;

        private UtpTransport _client = null;
        private UtpTransport _server = null;

        public WaitForTransportToConnect(UtpTransport client, UtpTransport server, float timeoutInSeconds)
        {
            _client = client;
            _server = server;
            _timeout = timeoutInSeconds;
        }

        public bool MoveNext()
        {
            _client.ClientEarlyUpdate();
            _server.ServerEarlyUpdate();

            _elapsedTime += Time.deltaTime;

            if (_elapsedTime >= _timeout)
            {
                Result = Status.TimedOut;
                return false;
            }
            else if (_client.ClientConnected())
            {
                Result = Status.ClientConnected;
                return false;
            }

            return true;
        }

        public void Reset()
        {
            _elapsedTime = 0f;
            _timeout = 0f;
            Result = Status.Undetermined;
        }
    } 
}