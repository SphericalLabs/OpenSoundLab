using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class artefactSignalGenerator : signalGenerator
{
    [DllImport("OSLNative")]
    public static extern void Artefact_Process(float[] buffer, float noiseAmount, int downsampleFactor, float jitterAmount, int bitReduction, int channels, int n);

    public float noiseAmount = 0;
    public float jitterAmount = 0;
    public float downsampleFactor = 1; //downsampling with factor 1 == preserve sample rate
    public float bitReduction = 0;

    int P_NOISE = 0;
    int P_JITTER = 1;
    int P_DOWNSAMPLE = 2;
    int P_BITREDUCTION = 3;

    public signalGenerator input;

    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        if (!recursionCheckPre()) return; // checks and avoids fatal recursions
        if (input != null)
        {
            input.processBuffer(buffer, dspTime, channels);

            float noise, jitter;
            int dwnmpl, bitrdx;
            noise = Mathf.Pow(noiseAmount, 3);
            jitter = Mathf.Pow(jitterAmount, 0.5f);
            dwnmpl = (int)Utils.map(downsampleFactor, 0, 1, 1, 50);
            bitrdx = (int)Utils.map(bitReduction, 0, 1, 0, 32);

            Artefact_Process(buffer, noise, dwnmpl, jitter, bitrdx, channels, buffer.Length);
        }
        if (!recursionCheckPre()) return; // checks and avoids fatal recursions
  }
}
