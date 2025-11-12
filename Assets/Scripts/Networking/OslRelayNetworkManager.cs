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

﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Unity.Services.Authentication;
using Unity.Services.Core;

using Utp;

namespace Network
{
    public class OslRelayNetworkManager : RelayNetworkManager
    {
        /// <summary>
        /// The local player object that spawns in.
        /// </summary>
        public Player localPlayer;
        private string m_SessionId = "";
        private string m_Username;
        private string m_UserId;

        /// <summary>
        /// Flag to determine if the user is logged into the backend.
        /// </summary>
        public static bool isLoggedIn = false;

        /// <summary>
        /// List of players currently connected to the server.
        /// </summary>
        private List<Player> m_Players;

        public override void Awake()
        {
            base.Awake();
            m_Players = new List<Player>();

            m_Username = SystemInfo.deviceName;
        }

        public async void UnityLogin()
        {
            try
            {
                await UnityServices.InitializeAsync();
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Logged into Unity, player ID: " + AuthenticationService.Instance.PlayerId);
                isLoggedIn = true;
            }
            catch (Exception e)
            {
                isLoggedIn = false;
                Debug.Log(e);
                if (NetworkMenuManager.Instance != null)
                {
                    NetworkMenuManager.Instance.GoBackToLocalScene();
                }
            }
        }

        public override void Update()
        {
            base.Update();
            if (NetworkManager.singleton.isNetworkActive)
            {
                if (localPlayer == null)
                {
                    FindLocalPlayer();
                }
            }
            else
            {
                localPlayer = null;
                m_Players.Clear();
            }
        }


        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log("MyNetworkManager: Server Started!");

            m_SessionId = System.Guid.NewGuid().ToString();
        }

        /*
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            base.OnServerAddPlayer(conn);

            foreach (KeyValuePair<uint, NetworkIdentity> kvp in NetworkServer.spawned)
            {
                Player comp = kvp.Value.GetComponent<Player>();

                // Add to player list if new
                if (comp != null && !m_Players.Contains(comp))
                {
                    comp.sessionId = m_SessionId;
                    m_Players.Add(comp);
                }
            }
        }*/

        public override void OnStopServer()
        {
            //base.OnStopServer();
            Debug.Log("MyNetworkManager: Server Stopped!");
            m_SessionId = "";
        }


        public override void OnStartHost()
        {
            base.OnStartHost();

            if (NetworkMenuManager.Instance != null)
            {
                NetworkMenuManager.Instance.ActivateHostUI();
            }
        }

        public override void OnStopHost()
        {
            base.OnStopHost();
            if (NetworkMenuManager.Instance != null)
            {
                NetworkMenuManager.Instance.DeactivateUI();
            }
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);

            Dictionary<uint, NetworkIdentity> spawnedPlayers = NetworkServer.spawned;

            // Update players list on client disconnect
            foreach (Player player in m_Players)
            {
                bool playerFound = false;

                foreach (KeyValuePair<uint, NetworkIdentity> kvp in spawnedPlayers)
                {
                    Player comp = kvp.Value.GetComponent<Player>();

                    // Verify the player is still in the match
                    if (comp != null && player == comp)
                    {
                        playerFound = true;
                        break;
                    }
                }

                if (!playerFound)
                {
                    m_Players.Remove(player);
                    break;
                }
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (NetworkMenuManager.Instance != null && mode == NetworkManagerMode.ClientOnly)
            {
                NetworkMenuManager.Instance.ActivateClientUI();
            }
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            Debug.Log("MyNetworkManager: Left the Server!");

            localPlayer = null;

            m_SessionId = "";
            if (NetworkMenuManager.Instance != null)
            {
                NetworkMenuManager.Instance.DeactivateUI();
            }
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            if (NetworkMenuManager.Instance != null)
            {
                NetworkMenuManager.Instance.CheckIfClientGetKickedOut();
            }
        }

        /*
        [Obsolete]
        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect();
            Debug.Log($"MyNetworkManager: {m_Username} Connected to Server!");
        }

        [Obsolete]
        public override void OnClientDisconnect(NetworkConnection conn)
        {
            base.OnClientDisconnect(conn);
            Debug.Log("MyNetworkManager: Disconnected from Server!");
        }*/

        /// <summary>
        /// Finds the local player if they are spawned in the scene.
        /// </summary>
        void FindLocalPlayer()
        {
            //Check to see if the player is loaded in yet
            if (NetworkClient.localPlayer == null)
                return;

            localPlayer = NetworkClient.localPlayer.GetComponent<Player>();
        }

        /*
        public override void ConfigureHeadlessFrameRate()
        {
            Application.targetFrameRate = serverTickRate;
            Debug.Log($"Tick Rate set to {Application.targetFrameRate} Hz.");
        }*/
    }
}

