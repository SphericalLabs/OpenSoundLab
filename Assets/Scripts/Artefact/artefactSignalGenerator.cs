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

﻿using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class artefactSignalGenerator : signalGenerator
{
    [DllImport("OSLNative")]
    public static extern void Artefact_Process(float[] buffer, float noiseAmount, int downsampleFactor, float jitterAmount, int bitReduction, int channels, int n);

    public float noiseAmount = 0;
    public float jitterAmount = 0;
    public float downsampleFactor = 1; //downsampling with factor 1 == preserve sample rate
    public float bitReduction = 0;

    int P_NOISE = 0;
    int P_JITTER = 1;
    int P_DOWNSAMPLE = 2;
    int P_BITREDUCTION = 3;

    public signalGenerator input;

    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        if (!recursionCheckPre()) return; // checks and avoids fatal recursions
        if (input != null)
        {
            input.processBuffer(buffer, dspTime, channels);

            float noise, jitter;
            int dwnmpl, bitrdx;
            noise = Mathf.Pow(noiseAmount, 3);
            jitter = Mathf.Pow(jitterAmount, 0.5f);
            dwnmpl = (int)Utils.map(downsampleFactor, 0, 1, 1, 50);
            bitrdx = (int)Utils.map(bitReduction, 0, 1, 0, 32);

            Artefact_Process(buffer, noise, dwnmpl, jitter, bitrdx, channels, buffer.Length);
        }
        if (!recursionCheckPre()) return; // checks and avoids fatal recursions
    }
}
