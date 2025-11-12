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

using Mirror;
using System;

public class NetworkTutorials : NetworkBehaviour
{
    private tutorialsDeviceInterface tutorialsDevice;

    private void Awake()
    {
        tutorialsDevice = GetComponent<tutorialsDeviceInterface>();
        if (tutorialsDevice != null)
        {
            tutorialsDevice.OnTriggerOpenTutorial += HandleTriggerOpenTutorial;
        }
    }

    private void OnDestroy()
    {
        if (tutorialsDevice != null)
        {
            tutorialsDevice.OnTriggerOpenTutorial -= HandleTriggerOpenTutorial;
        }
    }

    private void HandleTriggerOpenTutorial(tutorialPanel tut, bool startPaused)
    {
        if (isServer)
        {
            RpcTriggerOpenTutorial(Array.IndexOf(tutorialsDevice.tutorials, tut), startPaused);
        }
        else
        {
            CmdTriggerOpenTutorial(Array.IndexOf(tutorialsDevice.tutorials, tut), startPaused);
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdTriggerOpenTutorial(int tutorialIndex, bool startPaused)
    {
        RpcTriggerOpenTutorial(tutorialIndex, startPaused);
    }

    [ClientRpc]
    private void RpcTriggerOpenTutorial(int tutorialIndex, bool startPaused)
    {
        if (tutorialIndex >= 0 && tutorialIndex < tutorialsDevice.tutorials.Length)
        {
            tutorialsDevice.InternalOpenTutorial(tutorialsDevice.tutorials[tutorialIndex], startPaused);
        }
    }
}
