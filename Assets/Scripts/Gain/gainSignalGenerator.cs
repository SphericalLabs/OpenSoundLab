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

using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class gainSignalGenerator : signalGenerator
{

    public signalGenerator incoming;
    public bool active = true;
    public float amp = 0f;
    float lastAmp = 0f;

    float muteFader = 0f;


    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        if (!recursionCheckPre()) return; // checks and avoids fatal recursions
        if (incoming != null) incoming.processBuffer(buffer, dspTime, channels);

        for (int n = 0; n < buffer.Length; n++)
        {
            muteFader = Mathf.Clamp01(muteFader + ((!active || incoming == null) ? -0.005f : 0.005f)); // fade out or in
            buffer[n] *= Utils.lerp(lastAmp, amp, (float)n / buffer.Length) * muteFader;
        }
        lastAmp = amp;
        recursionCheckPost();
    }
}
