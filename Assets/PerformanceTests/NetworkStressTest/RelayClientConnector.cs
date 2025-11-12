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

using UnityEngine;
using Mirror;
using System;
using System.Collections;
using kcp2k;
using UnityEngine.SceneManagement;
using Network;


// this is not working yet, Relay connection does not start when given a join code via command-line
// please note that this file must also be added to a gameobject in the relay scene! that is currently not the case.
public class RelayClientConnector : MonoBehaviour
{
    public string ipAddress = "localhost";
    public ushort port = 7777; // Default Mirror port
    public bool autoConnect = false;
    public string relayJoinCode = "";

    private void Start()
    {

        ParseCommandLineArgs();

        if (relayJoinCode != "")
        {
            if (SceneManager.GetActiveScene().buildIndex == (int)masterControl.Scenes.Local)
            {
                SceneManager.LoadSceneAsync((int)masterControl.Scenes.Relay);
            }
            else if (SceneManager.GetActiveScene().buildIndex == (int)masterControl.Scenes.Relay)
            {
                SetupRelay();
                if (autoConnect) StartCoroutine(DelayedConnectRelay(5));
            }
        }
        else
        {
            SetupLocal();
            if (autoConnect) ConnectLocal();
        }

        //Debug.LogWarning("ClientConnector is only supported on Windows Standalone or Editor.");
        //this.enabled = false;

    }

    private void Update()
    {

        if (Input.GetKeyDown("c"))
        {
            if (relayJoinCode != "")
            {
                DelayedConnectRelay(2);
            }
            else
            {
                ConnectLocal();
            }
        }

    }

    private void ParseCommandLineArgs()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-ip" && i + 1 < args.Length)
            {
                ipAddress = args[i + 1];
            }
            else if (args[i] == "-port" && i + 1 < args.Length)
            {
                if (ushort.TryParse(args[i + 1], out ushort parsedPort))
                {
                    port = parsedPort;
                }
                else
                {
                    Debug.LogError("Invalid port number provided.");
                }
            }
            else if (args[i] == "-autoconnect")
            {
                autoConnect = true;
            }
            else if (args[i] == "-relaycode" && i + 1 < args.Length)
            {
                relayJoinCode = args[i + 1];
            }
        }
    }


    OslRelayNetworkManager relayManager;
    private void SetupRelay()
    {
        relayManager = FindObjectOfType<OslRelayNetworkManager>();
        if (relayManager == null)
        {
            Debug.LogError("MyNetworkManager not found in the scene.");
            return;
        }

        if (!string.IsNullOrEmpty(relayJoinCode))
        {
            relayManager.relayJoinCode = relayJoinCode;
        }
    }

    private IEnumerator DelayedConnectRelay(int sec = 5)
    {

        Debug.Log("Waiting 5 seconds before connecting to Relay...");
        yield return new WaitForSeconds(sec);
        ConnectRelay();
    }

    private void ConnectRelay()
    {
        relayManager.JoinRelayServer();
    }

    NetworkManager localManager;
    public void SetupLocal()
    {

        localManager = NetworkManager.singleton;
        if (Transport.active is TelepathyTransport telepathyTransport)
        {
            telepathyTransport.port = port;
        }
        else if (Transport.active is KcpTransport kcpTransport)
        {
            kcpTransport.Port = port;
        }
        else
        {
            Debug.LogError("Unsupported transport type");
            return;
        }

        relayManager.networkAddress = ipAddress;
    }

    public void ConnectLocal()
    {

        relayManager.StartClient();
        Debug.Log($"Connecting to {ipAddress}:{port}");

    }
}
