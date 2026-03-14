using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

public class filtertwoSignalGenerator : signalGenerator
{
    public const float minCutoffHz = 10f;
    public const float maxCutoffHz = 18000f;
    public const float modulationOctaveRange = 4f;
    public const float modulationLowpassHz = 500f;

    public enum filterMode
    {
        LP,
        HP,
        BP,
        Notch
    }

    public signalGenerator incoming, freqIncoming;

    public float cutoffFrequency = 0.5f;
    public float resonance = 0.5f;
    public filterMode curMode = filterMode.LP;

    float lastCutoffFrequency = 0.5f;
    float lastResonance = 0.5f;
    float[] frequencyBuffer;

    IntPtr x = IntPtr.Zero;
    int nativeChannels = 0;

    [DllImport("OSLNative")]
    static extern IntPtr FilterTwo_New(int channels, float sampleRate);

    [DllImport("OSLNative")]
    static extern void FilterTwo_Free(IntPtr x);

    [DllImport("OSLNative")]
    static extern void FilterTwo_Reset(IntPtr x);

    [DllImport("OSLNative")]
    static extern void FilterTwo_Process(IntPtr x, float[] buffer, int length, float cutoffPercent,
                                         float lastCutoffPercent, float minCutoffHz, float maxCutoffHz,
                                         float modulationOctaveRange, float modulationLowpassHz,
                                         float[] frequencyBuffer, float resonance, float lastResonance, int mode);

    [DllImport("OSLNative")]
    public static extern void SetArrayToSingleValue(float[] a, int length, float val);

    public override void Awake()
    {
        base.Awake();
        frequencyBuffer = new float[MAX_BUFFER_LENGTH];
        lastResonance = resonance;
    }

    void OnDestroy()
    {
        freeNative();
    }

    public float getModePercent()
    {
        switch (curMode)
        {
            case filterMode.HP:
                return 1f / 3f;
            case filterMode.BP:
                return 2f / 3f;
            case filterMode.Notch:
                return 1f;
            default:
                return 0f;
        }
    }

    public void setModeFromPercent(float percent)
    {
        switch (Mathf.RoundToInt(Mathf.Clamp01(percent) * 3f))
        {
            case 1:
                curMode = filterMode.HP;
                break;
            case 2:
                curMode = filterMode.BP;
                break;
            case 3:
                curMode = filterMode.Notch;
                break;
            default:
                curMode = filterMode.LP;
                break;
        }
    }

    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        if (!recursionCheckPre()) return;

        ensureNative(channels);
        ensureBuffers(buffer.Length);
        fillFrequencyBuffer(dspTime, channels);

        if (incoming != null)
            incoming.processBuffer(buffer, dspTime, channels);
        else
            SetArrayToSingleValue(buffer, buffer.Length, 0f);

        FilterTwo_Process(x, buffer, buffer.Length, cutoffFrequency, lastCutoffFrequency, minCutoffHz, maxCutoffHz,
                          modulationOctaveRange, modulationLowpassHz, frequencyBuffer, resonance, lastResonance,
                          (int)curMode);

        lastCutoffFrequency = cutoffFrequency;
        lastResonance = resonance;
        recursionCheckPost();
    }

    void ensureNative(int channels)
    {
        if (channels == nativeChannels && x != IntPtr.Zero)
            return;

        freeNative();
        nativeChannels = channels;
        x = FilterTwo_New(nativeChannels, (float)_sampleRate);
        FilterTwo_Reset(x);
    }

    void ensureBuffers(int length)
    {
        if (frequencyBuffer.Length != length)
            Array.Resize(ref frequencyBuffer, length);
    }

    void fillFrequencyBuffer(double dspTime, int channels)
    {
        SetArrayToSingleValue(frequencyBuffer, frequencyBuffer.Length, 0f);
        if (freqIncoming != null)
            freqIncoming.processBuffer(frequencyBuffer, dspTime, channels);
    }

    void freeNative()
    {
        if (x == IntPtr.Zero)
            return;

        FilterTwo_Free(x);
        x = IntPtr.Zero;
        nativeChannels = 0;
    }
}
