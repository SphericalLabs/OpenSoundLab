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

public class vcaSignalGenerator : signalGenerator
{

    public signalGenerator incoming, controlSig;
    public bool active = true;
    public float amp = 1f;
    float lastAmp = 0f;
    float sign;
    float[] controlBuffer = new float[1];


    [DllImport("OSLNative")]
    public static extern void SetArrayToSingleValue(float[] a, int length, float val);

    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        if (!recursionCheckPre()) return; // checks and avoids fatal recursions
        if (controlBuffer.Length != buffer.Length)
            System.Array.Resize(ref controlBuffer, buffer.Length);

        SetArrayToSingleValue(controlBuffer, controlBuffer.Length, 0f);
        if (controlSig != null) controlSig.processBuffer(controlBuffer, dspTime, channels);
        if (incoming != null) incoming.processBuffer(buffer, dspTime, channels);

        if (incoming != null)
        {
            if (controlSig != null)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = buffer[i] * controlBuffer[i] * Mathf.Pow(Utils.lerp(lastAmp, amp, (float)i / (buffer.Length - 1)), 2); // RingMod, 0-1 fades normal, -1 inverts phase
                }
            }
            else
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = buffer[i] * Mathf.Pow(Utils.lerp(lastAmp, amp, (float)i / (buffer.Length - 1)), 2); // dial acts as attenuator
                }
            }
        }
        else
        { // nothing plugged, no output
          //if(controlSig != null) {
          //  for (int i = 0; i < buffer.Length; i++)
          //  {
          //    buffer[i] = controlBuffer[i] * Mathf.Pow(amp, 2);
          //  }
          //} else {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 0;
            }
            //}
        }

        lastAmp = amp;
        recursionCheckPost();
    }
}
