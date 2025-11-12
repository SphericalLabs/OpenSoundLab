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

using UnityEngine;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System;

public class NoiseReinitTest : MonoBehaviour
{
    [DllImport("OSLNative")]
    private static extern IntPtr CreateNoiseProcessor(int seed);

    [DllImport("OSLNative")]
    private static extern void DestroyNoiseProcessor(IntPtr processor);

    [DllImport("OSLNative")]
    private static extern void SyncNoiseProcessor(IntPtr processor, int seed, int steps);

    private const int iterations = 10; // Number of iterations for averaging

    void Start()
    {
        RunBenchmark();
    }

    void RunBenchmark()
    {
        int[] discardSteps = { 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000 }; // Different magnitudes of steps
        int seed = 12345; // Arbitrary seed for the noise processor

        IntPtr processor = CreateNoiseProcessor(seed);

        foreach (int steps in discardSteps)
        {
            float averageTime = BenchmarkSyncNoiseProcessor(processor, seed, steps);
            UnityEngine.Debug.Log($"Average time for {steps} discard steps: {averageTime} ms");
        }

        DestroyNoiseProcessor(processor);
    }

    float BenchmarkSyncNoiseProcessor(IntPtr processor, int seed, int steps)
    {
        Stopwatch stopwatch = new Stopwatch();
        float totalTime = 0;

        for (int i = 0; i < iterations; i++)
        {
            stopwatch.Reset();
            stopwatch.Start();

            SyncNoiseProcessor(processor, seed, steps);

            stopwatch.Stop();
            totalTime += stopwatch.ElapsedMilliseconds;
        }

        return totalTime / iterations;
    }
}
