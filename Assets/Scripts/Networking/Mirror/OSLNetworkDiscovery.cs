// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
//
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
//
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
//
//
// Copyright © 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright © 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright © 2017 Apache 2.0 Google LLC SoundStage VR
//
// Licensed under the Apache License, Version 2.0 (the "License");
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Net;
using UnityEngine;
using UnityEngine.Events;

namespace Mirror.Discovery
{
    [Serializable]
    public class ServerFoundUnityEvent<TResponseType> : UnityEvent<TResponseType> { };

    [DisallowMultipleComponent]
    [AddComponentMenu("Network/OSL Network Discovery")]
    public class OSLNetworkDiscovery : NetworkDiscoveryBase<ServerRequest, OSLServerResonse>
    {
        public bool isDiscoverable = true;


        public void Awake()
        {
            if (Application.isEditor) isDiscoverable = true;
        }

        public bool ToggleIsDiscoverable()
        {
            return isDiscoverable = !isDiscoverable;
        }
        /*
        public void SetIsDiscoverable(bool b)
        {
            isDiscoverable = b;
            if (b)
            {
                AdvertiseServer();
            }
            else
            {

            }
        }*/

        #region Server

        /// <summary>
        /// Process the request from a client
        /// </summary>
        /// <remarks>
        /// Override if you wish to provide more information to the clients
        /// such as the name of the host player
        /// </remarks>
        /// <param name="request">Request coming from client</param>
        /// <param name="endpoint">Address of the client that sent the request</param>
        /// <returns>The message to be sent back to the client or null</returns>
        protected override OSLServerResonse ProcessRequest(ServerRequest request, IPEndPoint endpoint)
        {
            // In this case we don't do anything with the request
            // but other discovery implementations might want to use the data
            // in there,  This way the client can ask for
            // specific game mode or something

            try
            {
                // this is an example reply message,  return your own
                // to include whatever is relevant for your game
                return new OSLServerResonse
                {
                    serverId = ServerId,
                    uri = transport.ServerUri(),
                    version = Application.version,
                    userName = NetworkMenuManager.Instance.userName
                };
            }
            catch (NotImplementedException)
            {
                Debug.LogError($"Transport {transport} does not support network discovery");
                throw;
            }
        }
        #endregion

        #region Client

        /// <summary>
        /// Create a message that will be broadcasted on the network to discover servers
        /// </summary>
        /// <remarks>
        /// Override if you wish to include additional data in the discovery message
        /// such as desired game mode, language, difficulty, etc... </remarks>
        /// <returns>An instance of ServerRequest with data to be broadcasted</returns>
        protected override ServerRequest GetRequest() => new ServerRequest();

        /// <summary>
        /// Process the answer from a server
        /// </summary>
        /// <remarks>
        /// A client receives a reply from a server, this method processes the
        /// reply and raises an event
        /// </remarks>
        /// <param name="response">Response that came from the server</param>
        /// <param name="endpoint">Address of the server that replied</param>
        protected override void ProcessResponse(OSLServerResonse response, IPEndPoint endpoint)
        {
            // we received a message from the remote endpoint
            response.EndPoint = endpoint;

            // although we got a supposedly valid url, we may not be able to resolve
            // the provided host
            // However we know the real ip address of the server because we just
            // received a packet from it,  so use that as host.
            UriBuilder realUri = new UriBuilder(response.uri)
            {
                Host = response.EndPoint.Address.ToString()
            };
            response.uri = realUri.Uri;

            OnServerFound.Invoke(response);
        }

        #endregion



        protected override void ProcessClientRequest(ServerRequest request, IPEndPoint endpoint)
        {
            if (!isDiscoverable)
            {
                return;
            }
            base.ProcessClientRequest(request, endpoint);
        }

        public override void BroadcastDiscoveryRequest()
        {
            if (clientUdpClient == null)
                return;
            //copy from the original class NetworkDiscoveryBase.cs but removed the if below otherwise a already running Host will not look for the other hosts
            /*
            if (NetworkClient.isConnected)
            {
                StopDiscovery();
                return;
            }*/

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, serverBroadcastListenPort);

            if (!string.IsNullOrWhiteSpace(BroadcastAddress))
            {
                try
                {
                    endPoint = new IPEndPoint(IPAddress.Parse(BroadcastAddress), serverBroadcastListenPort);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            using (NetworkWriterPooled writer = NetworkWriterPool.Get())
            {
                writer.WriteLong(secretHandshake);

                try
                {
                    ServerRequest request = GetRequest();

                    writer.Write(request);

                    ArraySegment<byte> data = writer.ToArraySegment();

                    clientUdpClient.SendAsync(data.Array, data.Count, endPoint);
                }
                catch (Exception)
                {
                    // It is ok if we can't broadcast to one of the addresses
                }
            }
        }
    }
}
