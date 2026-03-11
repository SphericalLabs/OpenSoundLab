// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2026 OSLLv1 Sphericals OpenSoundLab
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

public class filterSignalGenerator : signalGenerator
{

    public signalGenerator incoming, freqIncoming;

    public float cutoffFrequency = 0f;
    float lastCutoffFrequency = 0f;
    public float resonance = .5f;

    float[] frequencyBuffer;

    public enum filterType
    {
        LP
    };

    public filterType curType = filterType.LP;

    [DllImport("OSLNative")]
    public static extern void SetArrayToSingleValue(float[] a, int length, float val);

    [DllImport("OSLNative")]
    public static extern void processStereoFilter(float[] buffer, int length, ref mfValues mfL, ref mfValues mfR, float cutoffFrequency, float lastCutoffFrequency, float[] frequencyBuffer, float resonance, float sampleRate);

    // create structs for passing to native code
    mfValues mf1L = new mfValues();
    mfValues mf1R = new mfValues();

    public override void Awake()
    {
        base.Awake();
        frequencyBuffer = new float[MAX_BUFFER_LENGTH];
    }

    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        if (!recursionCheckPre()) return; // checks and avoids fatal recursions
        if (frequencyBuffer.Length != buffer.Length)
            System.Array.Resize(ref frequencyBuffer, buffer.Length);

        SetArrayToSingleValue(frequencyBuffer, frequencyBuffer.Length, 0f);
        if (freqIncoming != null)
            freqIncoming.processBuffer(frequencyBuffer, dspTime, channels);

        if (incoming != null)
            incoming.processBuffer(buffer, dspTime, channels);
        else
            SetArrayToSingleValue(buffer, buffer.Length, 0f);

        curType = filterType.LP;
        processStereoFilter(buffer, buffer.Length, ref mf1L, ref mf1R, cutoffFrequency, lastCutoffFrequency, frequencyBuffer, resonance, (float)_sampleRate);

        lastCutoffFrequency = cutoffFrequency; // for slope limiting in native code
        recursionCheckPost();
    }
}

public struct mfValues
{
    public float f, p, q; // filter coefficients
    public float b0, b1, b2, b3, b4; // filter buffers (beware denormals!)
};
