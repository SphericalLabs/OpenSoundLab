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
using System;

public class cvSignalGenerator : signalGenerator
{
    public static float lowRange = 0.2f; // central place to set these ranges!
    public static float highRange = 1f; //


    struct PendingChange
    {
        public int sampleIndex;
        public float value;
    }

    float outVal = 0f;
    List<PendingChange> pendingChanges = new List<PendingChange>(4);
    double pendingDspTime = -1;
    private double lastProcessedDspTime = -1;
    private float[] cachedBuffer = new float[2048];
    float rangeMultiplier = lowRange;

    [DllImport("OSLNative")]
    public static extern void SetArrayToSingleValue(float[] a, int length, float val);

    public void setRange(float f)
    {
        rangeMultiplier = f;
    }
    public void setSignal(float f)
    {
        setSignal(f, 0, AudioSettings.dspTime);
    }

    public void setSignal(float f, int sampleIndex, double dspTime)
    {
        if (pendingDspTime != dspTime)
        {
            pendingChanges.Clear();
            pendingDspTime = dspTime;
        }
        pendingChanges.Add(new PendingChange { sampleIndex = sampleIndex, value = f });
    }

    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        if (dspTime == lastProcessedDspTime)
        {
            int len = Mathf.Min(buffer.Length, cachedBuffer.Length);
            System.Array.Copy(cachedBuffer, buffer, len);
            return;
        }

        if (cachedBuffer.Length != buffer.Length)
        {
            System.Array.Resize(ref cachedBuffer, buffer.Length);
        }

        SetArrayToSingleValue(buffer, buffer.Length, outVal * rangeMultiplier);

        if (pendingChanges.Count > 0 && pendingDspTime <= dspTime)
        {
            pendingChanges.Sort((a, b) => a.sampleIndex.CompareTo(b.sampleIndex));

            for (int i = 0; i < pendingChanges.Count; i++)
            {
                int sampleIndex = Mathf.Clamp(pendingChanges[i].sampleIndex, 0, buffer.Length - channels);
                float nextVal = pendingChanges[i].value * rangeMultiplier;

                for (int n = sampleIndex; n < buffer.Length; n += channels)
                {
                    for (int c = 0; c < channels; c++)
                    {
                        buffer[n + c] = nextVal;
                    }
                }

                outVal = pendingChanges[i].value;
            }

            pendingChanges.Clear();
            pendingDspTime = -1;
        }

        lastProcessedDspTime = dspTime;
        System.Array.Copy(buffer, cachedBuffer, buffer.Length);
    }

}
