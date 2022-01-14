// Hannes Barfuss
// hannes.barfuss@fhnw.ch

using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

public class compressorSignalGenerator : signalGenerator
{
    public enum Param : int
    {
        P_ATTACK,
        P_RELEASE,
        P_THRESHOLD,
        P_RATIO,
        P_KNEE,
        P_MAKEUP,
        P_LOOKAHEAD,
        P_N
    };

    public signalGenerator input, sidechain;

    private IntPtr x;
    private float[] sidechainBuffer = new float[2] { 0, 0 };
    private float[] p = new float[(int)Param.P_N];
    public float attenuation;
    public bool isClipping;

    public override void Awake()
    {
        x = Compressor_New(AudioSettings.outputSampleRate);
        Debug.Log("Sample rate is " + AudioSettings.outputSampleRate);
    }

    private void OnDestroy()
    {
        Compressor_Free(x);
    }

    public void SetParam(float value, int param)
    {
        switch(param)
        {
            case (int)Param.P_ATTACK:
                p[param] = 0.1f + value * 99.9f;
                break;
            case (int)Param.P_RELEASE:
                p[param] = 10 + value * 1990;
                break;
            case (int)Param.P_THRESHOLD:
                p[param] = value * -40;
                break;
            case (int)Param.P_RATIO:
                p[param] = 1 + value * 11;
                break;
            case (int)Param.P_KNEE:
                p[param] = value * 18;
                break;
            case (int)Param.P_MAKEUP:
                p[param] = value * 40;
                break;
            case (int)Param.P_LOOKAHEAD:
                p[param] = value * 100;
                break;
        }
        Debug.Log("Set param " + param + " to value " + p[param]);
    }

    [DllImport("SoundStageNative")]
    private static extern void Compressor_Process(float[] buffer, float[] sidechain, int length, int channels, IntPtr x);

    [DllImport("SoundStageNative")]
    private static extern IntPtr Compressor_New(float sampleRate);

    [DllImport("SoundStageNative")]
    private static extern void Compressor_Free(IntPtr x);

    [DllImport("SoundStageNative")]
    private static extern void Compressor_SetParam(float value, int param, IntPtr x);

    [DllImport("SoundStageNative")]
    private static extern float Compressor_GetAttenuation(IntPtr x);

    [DllImport("SoundStageNative")]
    private static extern bool Compressor_IsRamping(IntPtr x);

    [DllImport("SoundStageNative")]
    private static extern bool Compressor_IsClipping(IntPtr x);

    public override void processBuffer(float[] buffer, double dspTime, int channels)
    {
        if (sidechainBuffer.Length != buffer.Length)
            System.Array.Resize(ref sidechainBuffer, buffer.Length);

        Compressor_SetParam(p[(int)Param.P_ATTACK], (int)Param.P_ATTACK, x);
        Compressor_SetParam(p[(int)Param.P_RELEASE], (int)Param.P_RELEASE, x);
        Compressor_SetParam(p[(int)Param.P_THRESHOLD], (int)Param.P_THRESHOLD, x);
        Compressor_SetParam(p[(int)Param.P_RATIO], (int)Param.P_RATIO, x);
        Compressor_SetParam(p[(int)Param.P_KNEE], (int)Param.P_KNEE, x);
        Compressor_SetParam(p[(int)Param.P_MAKEUP], (int)Param.P_MAKEUP, x);
        Compressor_SetParam(p[(int)Param.P_LOOKAHEAD], (int)Param.P_LOOKAHEAD, x);

        if (input != null)
        {
            input.processBuffer(buffer, dspTime, channels);
            if (sidechain != null)
            {
                sidechain.processBuffer(sidechainBuffer, dspTime, channels);
                Compressor_Process(buffer, sidechainBuffer, buffer.Length, channels, x);
            }
            else
            {
                Compressor_Process(buffer, buffer, buffer.Length, channels, x);
            }
        }

        attenuation = Compressor_GetAttenuation(x);
        isClipping = Compressor_IsClipping(x);
    }
}

