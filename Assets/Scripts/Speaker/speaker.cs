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

public class speaker : signalGenerator
{

    public float volume = 1;
    public signalGenerator incoming;
    public bool monoMode = false;
    public bool leftOn = true;

    [DllImport("OSLNative")]
    public static extern void MultiplyArrayBySingleValue(float[] buffer, int length, float val);

    private void OnAudioFilterRead(float[] buffer, int channels)
    {
        if (incoming == null) return;
        double dspTime = AudioSettings.dspTime;
        incoming.processBuffer(buffer, dspTime, channels);

        if (monoMode)
        {
            // please note: unity only uses left channel if spatialize is on!
            // todo: how to handle non-spatialize settings?
            if (leftOn)
            {

                //NATIVE!
                for (int n = 0; n < buffer.Length; n += 2)
                {
                    buffer[n] = buffer[n + 1] = buffer[n] * volume;
                    // no need for dealing with right channel
                }
            }
            else
            {
                //NATIVE!
                for (int n = 0; n < buffer.Length; n += 2)
                {
                    buffer[n] = buffer[n + 1] = buffer[n + 1] * volume;
                    // no need for dealing with right channel
                }
            }
        }
        else
        {
            if (volume != 1)
                MultiplyArrayBySingleValue(buffer, buffer.Length, volume);
        }
    }
}
