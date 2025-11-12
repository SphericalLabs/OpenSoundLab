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

using System;
using UnityEngine;

// This test checks if there are losses and errors when accumulating the differences of a 32bit float a ramp signal over a long period of time.
// This might be useful, if the ramp-based timing system needs to derive higher cycle counts such as 0-n from a 0-1 signal.

public class RampSummationTest : MonoBehaviour
{
    // Sample rate and duration setup
    private const int sampleRate = 48000;
    private const int durationSeconds = 3629;
    private const int totalSamples = sampleRate * durationSeconds;

    // Frequency of the ramp signal
    private const float rampFrequency = 0.5f;
    private const float rampAmplitude = 1.0f; // Ramp from 0 to 1

    void Start()
    {
        // Generate the ramp signal
        float[] rampSignal = GenerateRampSignal(totalSamples, sampleRate, rampFrequency);

        // Benchmark normal accumulation
        float normalAccumulationResult;
        double normalAccumulationTime = BenchmarkNormalAccumulation(rampSignal, out normalAccumulationResult);

        // Benchmark Kahan summation
        float kahanAccumulationResult;
        double kahanAccumulationTime = BenchmarkKahanSummation(rampSignal, out kahanAccumulationResult);

        // Output the results
        Debug.Log($"Normal Accumulation: {normalAccumulationResult}, Time: {normalAccumulationTime} ms");
        Debug.Log($"Kahan Accumulation: {kahanAccumulationResult}, Time: {kahanAccumulationTime} ms");
        Debug.Log($"Difference between results: {Math.Abs(normalAccumulationResult - kahanAccumulationResult)}");
    }

    float[] GenerateRampSignal(int totalSamples, int sampleRate, float frequency)
    {
        float[] signal = new float[totalSamples];
        float period = sampleRate / frequency;
        float increment = rampAmplitude / period;

        for (int i = 0; i < totalSamples; i++)
        {
            // Calculate the current sample in the ramp period
            float phase = (i % period) * increment;
            signal[i] = phase;
        }

        return signal;
    }

    double BenchmarkNormalAccumulation(float[] signal, out float result)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        float accumulatedValue = 0f;

        for (int i = 1; i < signal.Length; i++)
        {
            accumulatedValue += signal[i] - signal[i - 1];
        }

        stopwatch.Stop();
        result = accumulatedValue;
        return stopwatch.Elapsed.TotalMilliseconds;
    }

    double BenchmarkKahanSummation(float[] signal, out float result)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        float sum = 0f;
        float c = 0f;

        for (int i = 1; i < signal.Length; i++)
        {
            float y = (signal[i] - signal[i - 1]) - c;
            float t = sum + y;
            c = (t - sum) - y;
            sum = t;
        }

        stopwatch.Stop();
        result = sum;
        return stopwatch.Elapsed.TotalMilliseconds;
    }
}
