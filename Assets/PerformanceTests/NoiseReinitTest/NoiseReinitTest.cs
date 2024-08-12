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
