using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

public class filterthreeSignalGenerator : signalGenerator
{
    public signalGenerator incoming;

    public float lowCutPercent = 0f;
    public float lowShelfFrequencyPercent = 0.3f;
    public float lowShelfGainPercent = 0.5f;
    public float bellOneFrequencyPercent = 0.35f;
    public float bellOneGainPercent = 0.5f;
    public float bellOneQPercent = 0.35f;
    public float bellTwoFrequencyPercent = 0.65f;
    public float bellTwoGainPercent = 0.5f;
    public float bellTwoQPercent = 0.35f;
    public float highShelfFrequencyPercent = 0.7f;
    public float highShelfGainPercent = 0.5f;
    public float highCutPercent = 1f;

    IntPtr x = IntPtr.Zero;
    int nativeChannels = 0;

    [DllImport("OSLNative")]
    static extern IntPtr FilterThree_New(int channels, float sampleRate);

    [DllImport("OSLNative")]
    static extern void FilterThree_Free(IntPtr x);

    [DllImport("OSLNative")]
    static extern void FilterThree_Reset(IntPtr x);

    [DllImport("OSLNative")]
    static extern void FilterThree_Process(IntPtr x, float[] buffer, int length, float lowCutFrequency,
                                           float lowShelfFrequency, float lowShelfGain, float bellOneFrequency,
                                           float bellOneGain, float bellOneQ, float bellTwoFrequency,
                                           float bellTwoGain, float bellTwoQ, float highShelfFrequency,
                                           float highShelfGain, float highCutFrequency);

    [DllImport("OSLNative")]
    public static extern void SetArrayToSingleValue(float[] a, int length, float val);

    public override void Awake()
    {
        base.Awake();
    }

    void OnDestroy()
    {
        freeNative();
    }

    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        if (!recursionCheckPre()) return;

        ensureNative(channels);

        if (incoming != null)
            incoming.processBuffer(buffer, dspTime, channels);
        else
            SetArrayToSingleValue(buffer, buffer.Length, 0f);

        FilterThree_Process(x, buffer, buffer.Length,
                            mapFrequency(lowCutPercent, 20f, 2000f),
                            mapFrequency(lowShelfFrequencyPercent, 40f, 1600f),
                            mapGain(lowShelfGainPercent),
                            mapFrequency(bellOneFrequencyPercent, 60f, 4000f),
                            mapGain(bellOneGainPercent),
                            mapQ(bellOneQPercent),
                            mapFrequency(bellTwoFrequencyPercent, 300f, 12000f),
                            mapGain(bellTwoGainPercent),
                            mapQ(bellTwoQPercent),
                            mapFrequency(highShelfFrequencyPercent, 1200f, 16000f),
                            mapGain(highShelfGainPercent),
                            mapFrequency(highCutPercent, 1000f, 20000f));

        recursionCheckPost();
    }

    void ensureNative(int channels)
    {
        if (channels == nativeChannels && x != IntPtr.Zero)
            return;

        freeNative();
        nativeChannels = channels;
        x = FilterThree_New(nativeChannels, (float)_sampleRate);
        FilterThree_Reset(x);
    }

    float mapFrequency(float percent, float minFrequency, float maxFrequency)
    {
        float clampedPercent = Mathf.Clamp01(percent);
        float minLog = Mathf.Log(minFrequency);
        float maxLog = Mathf.Log(maxFrequency);
        return Mathf.Exp(Mathf.Lerp(minLog, maxLog, clampedPercent));
    }

    float mapGain(float percent)
    {
        return Utils.map(Mathf.Clamp01(percent), 0f, 1f, -36f, 36f);
    }

    float mapQ(float percent)
    {
        float clampedPercent = Mathf.Clamp01(percent);
        float minLog = Mathf.Log(0.35f);
        float maxLog = Mathf.Log(10f);
        return Mathf.Exp(Mathf.Lerp(minLog, maxLog, clampedPercent));
    }

    void freeNative()
    {
        if (x == IntPtr.Zero)
            return;

        FilterThree_Free(x);
        x = IntPtr.Zero;
        nativeChannels = 0;
    }
}
