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

﻿using Mirror;
using UnityEngine;

public class Player : NetworkBehaviour
{
    /// <summary>
    /// The Sessions ID for the current server.
    /// </summary>
    [SyncVar]
    public string sessionId = "";

    /// <summary>
    /// Player name.
    /// </summary>
    public string username;

    public string ip;

    /// <summary>
    /// Platform the user is on.
    /// </summary>
    public string platform;

    /// <summary>
    /// Shifts the players position in space based on the inputs received.
    /// </summary>
    void HandleMovement()
    {
        if (isLocalPlayer)
        {
            float moveHorizontal = Input.GetAxis("Horizontal") * Time.deltaTime;
            float moveVertical = Input.GetAxis("Vertical") * Time.deltaTime;
            Vector3 movement = new Vector3(moveHorizontal * 3f, moveVertical * 3f, 0);
            transform.position = transform.position + movement;
        }
    }

    private void Awake()
    {
        username = SystemInfo.deviceName;
        platform = Application.platform.ToString();
        ip = NetworkManager.singleton.networkAddress;
    }

    private void Start()
    {
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
    }

    void Update()
    {
        HandleMovement();
    }

    public void MoveHorizontal(float value)
    {
        if (isLocalPlayer)
        {
            Vector3 movement = new Vector3(value * 0.1f, 0, 0);
            transform.position = transform.position + movement;
        }
    }
    public void MoveVertical(float value)
    {
        if (isLocalPlayer)
        {
            Vector3 movement = new Vector3(0f, value * 0.1f, 0);
            transform.position = transform.position + movement;
        }
    }

    /// <summary>
    /// Called after player has spawned in the scene.
    /// </summary>
    public override void OnStartServer()
    {
        Debug.Log("Player has been spawned on the server!");
    }
}
