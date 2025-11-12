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
// you may not use this file except in compliance with the License.
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

public class turntableComponentInterface : MonoBehaviour
{

    public Transform turntable;
    public clipPlayerComplex player;
    turntableUI _UI;

    float DPS = 270; //degrees per second

    void Awake()
    {
        _UI = GetComponentInChildren<turntableUI>();
    }

    public void addDelta(float d)
    {
        player.updateTurntableDelta(d / DPS);
    }

    public void updateTurntableGrab(bool on)
    {
        player.turntableGrabbed = on;
    }

    void Update()
    {
        if (player.active && !player.turntableGrabbed && !player.scrubGrabbed) turntable.Rotate(0, Time.deltaTime * 270 * player.playbackSpeed, 0); //.75*360 = 270
        else if (player.scrubGrabbed)
        {
            float s = player.getScrubAmount();
            turntable.Rotate(0, 270 * s, 0);
        }
    }
}
