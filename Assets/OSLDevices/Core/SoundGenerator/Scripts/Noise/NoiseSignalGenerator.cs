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
using System;

public class NoiseSignalGenerator : signalGenerator
{

    float sampleRatePercent = 1;
    int speedFrames = 1;

    int maxLength = 11025 * 16; //  max length of one random value in samples
    int counter = 0; // used for downsampling

    float curSample = -1.0f;
    float lastSample = 0.0f;

    IntPtr noiseProcessorPointer; // used in OSLNative
    private readonly object lockObject = new object();

    [DllImport("OSLNative")]
    private static extern IntPtr CreateNoiseProcessor(int seed);

    [DllImport("OSLNative")]
    private static extern void DestroyNoiseProcessor(IntPtr processor);

    [DllImport("OSLNative")]
    private static extern void NoiseProcessBuffer(IntPtr processor, float[] buffer, int length, int channels, float sampleRatePercent, ref float lastSample, ref int counter, int speedFrames, [MarshalAs(UnmanagedType.I1)] ref bool updated);

    [DllImport("OSLNative")]
    private static extern void SyncNoiseProcessor(IntPtr processor, int seed, int steps);

    [DllImport("OSLNative")]
    public static extern int GetCurrentSeed(IntPtr processor);

    [DllImport("OSLNative")]
    public static extern int GetCurrentStep(IntPtr processor);

    public int GetSeed()
    {
        lock (lockObject)
        {
            return GetCurrentSeed(noiseProcessorPointer);
        }
    }

    // Instance method to get the current step
    public int GetStep()
    {
        lock (lockObject)
        {
            return GetCurrentStep(noiseProcessorPointer);
        }
    }


    public bool updated = false;

    public void updatePercent(float per)
    {
        lock (lockObject)
        {
            setRatePercent(per);
        }
    }

    public override void Awake()
    {
        base.Awake();
        noiseProcessorPointer = CreateNoiseProcessor(Utils.GetNoiseSeed());
        //SyncNoiseProcessor(noiseProcessorPointer, noiseStep); // noiseStep should be synced via Mirror if necessary
        //// or call it sync and use to also set seed?
    }

    public void OnDestroy()
    {
        DestroyNoiseProcessor(noiseProcessorPointer);
    }

    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        lock (lockObject)
        {
            NoiseProcessBuffer(noiseProcessorPointer, buffer, buffer.Length, channels, sampleRatePercent, ref lastSample, ref counter, speedFrames, ref updated);
        }
    }

    public void syncNoiseSignalGenerator(int seed, int steps)
    {
        lock (lockObject)
        {
            Debug.Log($"Sync noise signal{seed},{steps}");
            SyncNoiseProcessor(noiseProcessorPointer, seed, steps);
        }
    }

    public void captureNetworkState(out int seed, out int step, out float currentSample, out int currentCounter, out float ratePercent)
    {
        lock (lockObject)
        {
            seed = GetCurrentSeed(noiseProcessorPointer);
            step = GetCurrentStep(noiseProcessorPointer);
            currentSample = lastSample;
            currentCounter = counter;
            ratePercent = sampleRatePercent;
        }
    }

    public void applyNetworkState(int seed, int step, float currentSample, int currentCounter, float ratePercent)
    {
        lock (lockObject)
        {
            setRatePercent(ratePercent);
            SyncNoiseProcessor(noiseProcessorPointer, seed, step);
            lastSample = currentSample;
            counter = Mathf.Max(0, currentCounter);
            updated = false;
        }
    }

    void setRatePercent(float per)
    {
        if (sampleRatePercent == per) return;
        sampleRatePercent = per;
        speedFrames = Mathf.RoundToInt(maxLength * Mathf.Pow(Mathf.Clamp01(1f - per / 0.95f), 4));
    }
}
