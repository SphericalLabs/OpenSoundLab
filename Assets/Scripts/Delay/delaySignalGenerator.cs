// Hannes Barfuss
// hannes.barfuss@fhnw.ch

using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

public class delaySignalGenerator : signalGenerator
{
    public enum Param : int
    {
        P_TIME,
        P_FEEDBACK,
        P_WET,
        P_DRY,
        P_N
    };

    public const float MIN_TIME = 0.050f; // * 1000ms
    public const float MAX_TIME = 5.0f; // * 1000 ms
    public const float MIN_FEEDBACK = 0;
    public const float MAX_FEEDBACK = 1f;
    public const float MIN_WET = -96; //dB
    public const float MAX_WET = 0; //dB
    public const float MIN_DRY = -96; //dB
    public const float MAX_DRY = 0; //dB

    public signalGenerator input;

    private IntPtr x;
    private float[] p = new float[(int)Param.P_N];
    private bool processing = false;

    public override void Awake()
    {
        int maxDelaySamples = (int)(MAX_TIME * AudioSettings.outputSampleRate);
        x = Delay_New(maxDelaySamples);
        Debug.Log("Maximum delay time in samples is " + maxDelaySamples);
    }

    private void OnDestroy()
    {
        Delay_Free(x);
    }

    public void SetParam(float value, int param)
    {
        switch(param)
        {
            case (int)Param.P_TIME:
                p[param] = Utils.map(Mathf.Pow(value, 3), 0, 1, MIN_TIME * AudioSettings.outputSampleRate, MAX_TIME * AudioSettings.outputSampleRate);
                break;
            case (int)Param.P_FEEDBACK:
                p[param] = Utils.map(value, 0, 1, MIN_FEEDBACK, MAX_FEEDBACK);
                break;
            case (int)Param.P_WET:
                p[param] = Utils.dbToLin( Utils.map(value, 0, 1, MIN_WET, MAX_WET, 0.8f) );
                break;
            case (int)Param.P_DRY:
                p[param] = Utils.dbToLin( Utils.map(value, 0, 1, MIN_DRY, MAX_DRY, 0.8f) );
                break;
        }
        Debug.Log("Delay: Set param " + ((Param)param).ToString() + " to value " + p[param]);
    }

    [DllImport("SoundStageNative")]
    private static extern void Delay_Process(float[] buffer, int length, int channels, IntPtr x);

    [DllImport("SoundStageNative")]
    private static extern IntPtr Delay_New(int maxDelayTimeSamples);

    [DllImport("SoundStageNative")]
    private static extern void Delay_Free(IntPtr x);

    [DllImport("SoundStageNative")]
    private static extern void Delay_SetParam(float value, int param, IntPtr x);

    public override void processBuffer(float[] buffer, double dspTime, int channels)
    {
        Delay_SetParam(p[(int)Param.P_TIME], (int)Param.P_TIME, x);
        Delay_SetParam(p[(int)Param.P_FEEDBACK], (int)Param.P_FEEDBACK, x);
        Delay_SetParam(p[(int)Param.P_WET], (int)Param.P_WET, x);
        Delay_SetParam(p[(int)Param.P_DRY], (int)Param.P_DRY, x);

        if (input != null)
        {
            input.processBuffer(buffer, dspTime, channels);
        }

        //We need to process the delay even if there is currently no input,
        //bc there could be unconsumed samples from a previous input left in the delay line.
        //To optimize, we could store the timestamp when the last input connection was removed.
        //Then we only have to process if P_TIME is larger then the elapsed time since the connection was removed.
        Delay_Process(buffer, buffer.Length, channels, x);

        if (!processing)
        {
            processing = true;
            Debug.Log("Delay is processing");
        }
    }
}

