using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using Mirror;

public class NetworkNoiseSignalGenerator : NetworkSignalGenerator
{
    float speedPercent = 1;
    int speedFrames = 1;

    int maxLength = 11025 * 16; //  max length of one random value in samples
    int counter = 0; // used for downsampling

    float curSample = -1.0f;

    int noiseStep = 0; // count how many samples have been calculated with thise noiseGen already, used for syncing with other clients
    [SyncVar] int seed = 0; // select a specific noise pattern
    IntPtr noiseProcessorPointer; // used in OSLNative
    private readonly object lockObject = new object();

    [DllImport("OSLNative")]
    private static extern IntPtr CreateNoiseProcessor(int seed);

    [DllImport("OSLNative")]
    private static extern void DestroyNoiseProcessor(IntPtr processor);

    [DllImport("OSLNative")]
    private static extern void NoiseProcessBuffer(IntPtr processor, float[] buffer, int length, int channels, float frequency, ref int counter, int speedFrames, ref bool updated);

    [DllImport("OSLNative")]
    private static extern void SyncNoiseProcessor(IntPtr processor, int seed, int steps);

    public bool updated = false;

    public void updatePercent(float per)
    {
        if (speedPercent == per) return;
        speedPercent = per;
        speedFrames = Mathf.RoundToInt(maxLength * Mathf.Pow(Mathf.Clamp01(1f - per / 0.95f), 4));
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

    public override void processBuffer(float[] buffer, double dspTime, int channels)
    {
        lock (lockObject)
        {
            NoiseProcessBuffer(noiseProcessorPointer, buffer, buffer.Length, channels, speedPercent, ref counter, speedFrames, ref updated);
            noiseStep += buffer.Length;
        }
    }

    public void syncNoiseSignalGenerator(int seed, int steps)
    {
        lock (lockObject)
        {
            SyncNoiseProcessor(noiseProcessorPointer, seed, steps);
            noiseStep = steps;
        }
    }

    #region Mirror
    protected override void OnSync()
    {
        if (isServer)
        {
            RpcUpdatePhase(_phase);
            RpcUpdateSteps(noiseStep);
        }
        else
        {
            CmdRequestSync();
        }
    }

    protected override void OnIntervalSync()
    {
        base.OnIntervalSync();
        if (isServer)
        {
            RpcUpdateSteps(noiseStep);
        }
    }
    [Command]
    protected override void CmdRequestSync()
    {
        base.CmdRequestSync();
        RpcUpdateSteps(noiseStep);

    }
    [ClientRpc]
    protected virtual void RpcUpdateSteps(int noiseStep)
    {
        if (isClient)
        {
            this.noiseStep = noiseStep;
        }
    }

    #endregion
}
