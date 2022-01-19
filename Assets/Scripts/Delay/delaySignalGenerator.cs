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
        P_CLEAR,
        P_N
    };

    public const float MIN_TIME = 0.001f; // * 1000ms
    public const float MAX_TIME = 0.05f; // * 1000 ms
    public const float MIN_FEEDBACK = 0;
    public const float MAX_FEEDBACK = 1f;
    public const float MIN_WET = -96; //dB
    public const float MAX_WET = 0; //dB
    public const float MIN_DRY = -96; //dB
    public const float MAX_DRY = 0; //dB

    public signalGenerator input, cTimeInput, cFeedbackInput;

    private IntPtr x;
    private float[] p = new float[(int)Param.P_N];
    private bool shouldClear = false;

    private float[] cTimeBuffer = new float[0];
    private float[] cFeedbackBuffer = new float[0];
    private float cTime;
    private float cFeedback;

    public override void Awake()
    {
        int maxDelaySamples = (int)(MAX_TIME * AudioSettings.outputSampleRate);
        x = Delay_New(maxDelaySamples);
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
                p[param] = value; //this is finally set in process method
                break;
            case (int)Param.P_FEEDBACK:
                p[param] = value; //this is finally set in process method
                break;
            case (int)Param.P_WET:
                p[param] = Utils.dbToLin( Utils.map(value, 0, 1, MIN_WET, MAX_WET, 0.8f) );
                break;
            case (int)Param.P_DRY:
                p[param] = Utils.dbToLin( Utils.map(value, 0, 1, MIN_DRY, MAX_DRY, 0.8f) );
                break;
            case (int)Param.P_CLEAR:
                if (value != 0)
                    shouldClear = true;
                break;
        }
    }

    [DllImport("SoundStageNative")]
    private static extern void Delay_Process(float[] buffer, int length, int channels, float cTime, float cFeedback, IntPtr x);

    [DllImport("SoundStageNative")]
    private static extern IntPtr Delay_New(int maxDelayTimeSamples);

    [DllImport("SoundStageNative")]
    private static extern void Delay_Free(IntPtr x);

    [DllImport("SoundStageNative")]
    private static extern void Delay_SetParam(float value, int param, IntPtr x);

    [DllImport("SoundStageNative")]
    private static extern void Delay_Clear(IntPtr x);

    public override void processBuffer(float[] buffer, double dspTime, int channels)
    {
        if (cTimeBuffer.Length != buffer.Length)
            System.Array.Resize(ref cTimeBuffer, buffer.Length);

        if (cFeedbackBuffer.Length != buffer.Length)
            System.Array.Resize(ref cFeedbackBuffer, buffer.Length);

        if (shouldClear)
        {
            Delay_Clear(x);
            shouldClear = false;
        }

        if (input != null)
        {
            input.processBuffer(buffer, dspTime, channels);
        }

        cTime = cFeedback = 0;

        if (cTimeInput != null)
        {
            cTimeInput.processBuffer(cTimeBuffer, dspTime, channels);
            cTime = cTimeBuffer[0];
        }

        if (cFeedbackInput != null)
        {
            cFeedbackInput.processBuffer(cFeedbackBuffer, dspTime, channels);
            cFeedback = cFeedbackBuffer[0];
        }

        Delay_SetParam(Utils.map(Mathf.Pow(Mathf.Clamp01(p[(int)Param.P_TIME] + cTime), 3), 0, 1, MIN_TIME * AudioSettings.outputSampleRate, MAX_TIME * AudioSettings.outputSampleRate), (int)Param.P_TIME, x);
        Delay_SetParam(Utils.map(Mathf.Clamp01(p[(int)Param.P_FEEDBACK] + cFeedback), 0, 1, MIN_FEEDBACK, MAX_FEEDBACK), (int)Param.P_FEEDBACK, x);
        Delay_SetParam(p[(int)Param.P_WET], (int)Param.P_WET, x);
        Delay_SetParam(p[(int)Param.P_DRY], (int)Param.P_DRY, x);

        //We need to process the delay even if there is currently no input,
        //bc there could be unconsumed samples from a previous input left in the delay line.
        //To optimize, we could store the timestamp when the last input connection was removed.
        //Then we only have to process if P_TIME is larger then the elapsed time since the connection was removed.
        Delay_Process(buffer, buffer.Length, channels, cTime, cFeedback, x);
    }
}

