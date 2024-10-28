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
        P_LIMIT,
        P_BYPASS,
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
        SetParam(6.0f, (int)Param.P_KNEE);
        SetParam(0, (int)Param.P_LIMIT);
        SetParam(0, (int)Param.P_LOOKAHEAD);
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
                p[param] = Utils.map(value, 0, 1, 0.01f, 1000.0f, 0.2f);
                //p[param] = 0.1f + value * 99.9f;
                break;
            case (int)Param.P_RELEASE:
                p[param] = Utils.map(value, 0, 1, 10.0f, 2000.0f, 0.2f);
                //p[param] = 10 + value * 1990;
                break;
            case (int)Param.P_THRESHOLD:
                p[param] = Utils.map(value, 0, 1, -40, 0, 0.2f);
                //p[param] = value * -40;
                break;
            case (int)Param.P_RATIO:
                p[param] = value == 1 ? float.MaxValue : Utils.map(value, 0, 1, 1, 24, 0.2f);
                //p[param] = 1 + value * 11;
                break;
            case (int)Param.P_KNEE:
                p[param] = Utils.map(value, 0, 1, 0, 18, 0.2f);
                //p[param] = value * 18;
                break;
            case (int)Param.P_MAKEUP:
                p[param] = Utils.map(value, 0, 1, 0, 40, 0.2f);
                //p[param] = value * 40;
                break;
            case (int)Param.P_LOOKAHEAD:
                p[param] = Utils.map(value, 0, 1, 0, 100, 0.2f);
                //p[param] = value * 100;
                break;
            case (int)Param.P_LIMIT:
                p[param] = value == 0 ? 0 : 1;
                break;
            case (int)Param.P_BYPASS:
                p[param] = value == 0 ? 0 : 1;
                break;
        }
    }

    [DllImport("OSLNative")]
    private static extern void Compressor_Process(float[] buffer, float[] sidechain, int length, int channels, IntPtr x);

    [DllImport("OSLNative")]
    private static extern IntPtr Compressor_New(float sampleRate);

    [DllImport("OSLNative")]
    private static extern void Compressor_Free(IntPtr x);

    [DllImport("OSLNative")]
    private static extern void Compressor_SetParam(float value, int param, IntPtr x);

    [DllImport("OSLNative")]
    private static extern float Compressor_GetAttenuation(IntPtr x);

    [DllImport("OSLNative")]
    private static extern bool Compressor_IsRamping(IntPtr x);

    [DllImport("OSLNative")]
    private static extern bool Compressor_IsClipping(IntPtr x);
    
    [DllImport("OSLNative")]
    public static extern void SetArrayToSingleValue(float[] a, int length, float val);

   public override void processBufferImpl(float[] buffer, double dspTime, int channels)
   {
        if (!recursionCheckPre()) return; // checks and avoids fatal recursions

        if (sidechainBuffer.Length != buffer.Length)
            System.Array.Resize(ref sidechainBuffer, buffer.Length);
        SetArrayToSingleValue(sidechainBuffer, sidechainBuffer.Length, 0f);

        Compressor_SetParam(p[(int)Param.P_BYPASS], (int)Param.P_BYPASS, x);
        Compressor_SetParam(p[(int)Param.P_ATTACK], (int)Param.P_ATTACK, x);
        Compressor_SetParam(p[(int)Param.P_RELEASE], (int)Param.P_RELEASE, x);
        Compressor_SetParam(p[(int)Param.P_THRESHOLD], (int)Param.P_THRESHOLD, x);
        Compressor_SetParam(p[(int)Param.P_RATIO], (int)Param.P_RATIO, x);
        Compressor_SetParam(p[(int)Param.P_MAKEUP], (int)Param.P_MAKEUP, x);

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
        //isClipping = Compressor_IsClipping(x);
        recursionCheckPost();
   }
}

