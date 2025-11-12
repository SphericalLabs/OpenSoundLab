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

﻿// vis2k: GUILayout instead of spacey += ...; removed Update hotkeys to avoid
// confusion if someone accidentally presses one.
using System.ComponentModel;
using System.Collections.Generic;
using UnityEngine;
using Network;
using Mirror;
using Unity.Services.Relay.Models;

namespace UI
{
    /// <summary>
    /// An extension for the NetworkManager that displays a default HUD for controlling the network state of the game.
    /// <para>This component also shows useful internal state for the networking system in the inspector window of the editor. It allows users to view connections, networked objects, message handlers, and packet statistics. This information can be helpful when debugging networked games.</para>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Network/MenuUI")]
    [RequireComponent(typeof(OslRelayNetworkManager))]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [HelpURL("https://mirror-networking.com/docs/Components/NetworkManagerHUD.html")]
    public class MenuUI : MonoBehaviour
    {
        private OslRelayNetworkManager m_Manager;

        /// <summary>
        /// Whether to show the default control HUD at runtime.
        /// </summary>
        public bool showGUI = true;

        /// <summary>
        /// The horizontal offset in pixels to draw the HUD runtime GUI at.
        /// </summary>
        public int offsetX;

        /// <summary>
        /// The vertical offset in pixels to draw the HUD runtime GUI at.
        /// </summary>
        public int offsetY;

        void Awake()
        {
            m_Manager = GetComponent<OslRelayNetworkManager>();
        }

        void OnGUI()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                if (arg == "-server")
                {
                    m_Manager.StartServer();
                    showGUI = false;
                }
            }

            if (!showGUI)
                return;

            GUILayout.BeginArea(new Rect(10 + offsetX, 40 + offsetY, 215, 9999));
            if (!NetworkClient.isConnected && !NetworkServer.active)
            {
                StartButtons();
            }
            else
            {
                StatusLabels();
            }

            // client ready
            /*
			if (NetworkClient.isConnected && !NetworkClient.ready)
			{
				NetworkClient.Ready();

				if (NetworkClient.localPlayer == null)
				{
					NetworkClient.AddPlayer();
				}

			}*/

            StopButtons();

            GUILayout.EndArea();
        }

        void StartButtons()
        {
            if (!NetworkClient.active)
            {
                // Server Only
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    // cant be a server in webgl build
                    GUILayout.Box("(  WebGL cannot be server  )");
                }
                else
                {
                    if (GUILayout.Button("Server Only")) m_Manager.StartStandardServer();
                }

                if (OslRelayNetworkManager.isLoggedIn)
                {
                    // Server + Client
                    if (Application.platform != RuntimePlatform.WebGLPlayer)
                    {
                        if (GUILayout.Button("Standard Host (Server + Client)"))
                        {
                            m_Manager.StartStandardHost();
                        }

                        if (GUILayout.Button("Relay Host (Server + Client)"))
                        {
                            int maxPlayers = 8;
                            m_Manager.StartRelayHost(maxPlayers);
                        }
                    }

                    // Client + IP
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Client (without Relay)"))
                    {
                        m_Manager.JoinStandardServer();
                    }
                    m_Manager.networkAddress = GUILayout.TextField(m_Manager.networkAddress);
                    GUILayout.EndHorizontal();

                    // Client + Relay Join Code
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Client (with Relay)"))
                    {
                        m_Manager.JoinRelayServer();
                    }
                    m_Manager.relayJoinCode = GUILayout.TextField(m_Manager.relayJoinCode);
                    GUILayout.EndHorizontal();

                    if (GUILayout.Button("Get Relay Regions"))
                    {
                        // Note: We are not doing anything with these regions in this example, we are just illustrating how you would go about fetching these regions
                        m_Manager.GetRelayRegions((List<Region> regions) =>
                        {
                            if (regions.Count > 0)
                            {
                                for (int i = 0; i < regions.Count; i++)
                                {
                                    Region region = regions[i];
                                    Debug.Log("Found region. ID: " + region.Id + ", Name: " + region.Description);
                                }
                            }
                            else
                            {
                                Debug.LogWarning("No regions received");
                            }
                        },

                        () =>
                        {
                            Debug.LogError("Failed to retrieve the list of Relay regions.");
                        });
                    }
                }
                else
                {
                    if (GUILayout.Button("Auth Login"))
                    {
                        m_Manager.UnityLogin();
                    }
                }
            }
            else
            {
                // Connecting
                GUILayout.Label("Connecting to " + m_Manager.networkAddress + "..");
                if (GUILayout.Button("Cancel Connection Attempt"))
                {
                    m_Manager.StopClient();
                }
            }
        }

        void StatusLabels()
        {
            // server / client status message
            if (NetworkServer.active)
            {
                //GUILayout.Label("Server: active. Transport: " + Transport.activeTransport);
                if (m_Manager.IsRelayEnabled())
                {
                    GUILayout.Label("Relay enabled. Join code: " + m_Manager.relayJoinCode);
                }
            }
            if (NetworkClient.isConnected)
            {
                GUILayout.Label("Client: address=" + m_Manager.networkAddress);
            }
        }

        void StopButtons()
        {
            // stop host if host mode
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                if (GUILayout.Button("Stop Host"))
                {
                    m_Manager.StopHost();
                }
            }
            // stop client if client-only
            else if (NetworkClient.isConnected)
            {
                if (GUILayout.Button("Stop Client"))
                {
                    m_Manager.StopClient();
                }
            }
            // stop server if server-only
            else if (NetworkServer.active)
            {
                if (GUILayout.Button("Stop Server"))
                {
                    m_Manager.StopServer();
                }
            }
        }
    }
}
