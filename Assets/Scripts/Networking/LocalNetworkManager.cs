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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace Network
{
    public class LocalNetworkManager : NetworkManager
    {

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

    }
}

