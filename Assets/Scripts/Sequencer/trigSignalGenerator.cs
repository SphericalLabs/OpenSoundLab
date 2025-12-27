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
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class trigSignalGenerator : signalGenerator
{

    bool newSignal = false;
    bool signalOn = false;

    [DllImport("OSLNative")]
    public static extern void SetArrayToSingleValue(float[] a, int length, float val);

    public void setSignal(bool on)
    {
        newSignal = true;
        signalOn = on;
    }

    private double lastProcessedDspTime = -1;
    private float[] cachedBuffer = new float[2048];
    private double sigTime = -1;

    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        if (dspTime == lastProcessedDspTime)
        {
            int len = Mathf.Min(buffer.Length, cachedBuffer.Length);
            System.Array.Copy(cachedBuffer, buffer, len);
            return;
        }

        SetArrayToSingleValue(buffer, buffer.Length, 0f);

        if (newSignal)
        {
            if ((sigTime == -1 || sigTime == dspTime) && signalOn)
            {
                sigTime = dspTime;
                buffer[0] = buffer[1] = 1f;
            }
            else
            {
                newSignal = false;
                sigTime = -1;
            }
        }

        lastProcessedDspTime = dspTime;
        System.Array.Copy(buffer, cachedBuffer, buffer.Length);
    }
}


