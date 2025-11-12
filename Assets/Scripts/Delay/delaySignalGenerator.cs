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
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
        P_INTERPOLATION,
        P_MIN_SAMPLES,
        P_MAX_SAMPLES,
        P_N
    };

    const int INTERPOLATION_NONE = 1;
    const int INTERPOLATION_LINEAR = 2;

    //const int DELAYMODE_SIMPLE = 0; deprecated
    const int DELAYMODE_OVERSAMPLED = 1;
    const int DELAYMODE_EFFICIENT = 2;

    public float MIN_TIME; // in seconds
    public float MAX_TIME; // in seconds
    public const float MIN_FEEDBACK = 0;
    public const float MAX_FEEDBACK = 1f;
    public const float MIN_WET = -96; //dB
    public const float MAX_WET = 0; //dB
    public const float MIN_DRY = -96; //dB
    public const float MAX_DRY = 0; //dB

    int sampleRate;

    public signalGenerator sigIn, sigModTime, sigModFeedback, sigModTrigger, sigModMix;

    private IntPtr x;
    private float[] p = new float[(int)Param.P_N];
    private bool shouldClear = false;

    private float[] modTimeBuffer = new float[0];
    private float[] modFeedbackBuffer = new float[0];
    private float[] modTriggerBuffer = new float[0];
    private float[] modMixBuffer = new float[0];

    private bool modTriggerState = false;
    private float modFeedbackVal;
    private float lastTriggerFloat = 0f;

    public override void Awake()
    {
        sampleRate = AudioSettings.outputSampleRate;
        MIN_TIME = Mathf.Pow(2f, minPower) / sampleRate; // in seconds
        MAX_TIME = Mathf.Pow(2f, maxPower) / sampleRate; // in seconds
        int maxDelaySamples = (int)(MAX_TIME * sampleRate);
        x = Delay_New(maxDelaySamples);
        Delay_SetParam(INTERPOLATION_LINEAR, (int)Param.P_INTERPOLATION, x);
        Delay_SetMode(DELAYMODE_EFFICIENT, x);
    }

    private void OnDestroy()
    {
        Delay_Free(x);
    }

    public void SetParam(float value, int param)
    {
        switch (param)
        {
            case (int)Param.P_TIME:
                p[param] = value; //this is finally set in process method
                break;
            case (int)Param.P_FEEDBACK:
                p[param] = value; //this is finally set in process method
                break;
            case (int)Param.P_WET:
                p[param] = value;
                break;
            case (int)Param.P_DRY:
                p[param] = value;
                break;
            case (int)Param.P_CLEAR:
                if (value != 0)
                    shouldClear = true;
                break;

        }
    }

    float timeCenter;
    float timeRange;

    // absolute minimum 2^9 samples
    // absolute maximum 2^21 samples
    const int minPower = 9;
    const int maxPower = 21;


    public void SetTimeRange(int mode)
    {
        switch (mode)
        {
            case 0: // full range
                timeCenter = Mathf.Pow(2f, minPower + (maxPower - minPower) / 2);
                timeRange = (maxPower - minPower) / 2;
                break;
            case 1: // low third range
                timeCenter = Mathf.Pow(2f, minPower + (maxPower - minPower) / 6 * 1);
                timeRange = (maxPower - minPower) / 6;
                break;
            case 2: // mid third range
                timeCenter = Mathf.Pow(2f, minPower + (maxPower - minPower) / 6 * 3);
                timeRange = (maxPower - minPower) / 6;
                break;
            case 3: // high third range
                timeCenter = Mathf.Pow(2f, minPower + (maxPower - minPower) / 6 * 5);
                timeRange = (maxPower - minPower) / 6;
                break;
        }

        Delay_SetParam(Mathf.Pow(2f, minPower), (int)Param.P_MIN_SAMPLES, x);
        Delay_SetParam(Mathf.Pow(2f, maxPower), (int)Param.P_MAX_SAMPLES, x);

    }

    [DllImport("OSLNative")]
    //private static extern void Delay_Process(float[] buffer, int length, int channels, IntPtr x);
    private static extern void Delay_Process(float[] buffer, float[] timeBuffer, float[] feedbackBuffer, float[] mixBuffer, int n, int channels, IntPtr x);

    [DllImport("OSLNative")]
    private static extern IntPtr Delay_New(int maxDelayTimeSamples);

    [DllImport("OSLNative")]
    private static extern void Delay_Free(IntPtr x);

    [DllImport("OSLNative")]
    private static extern void Delay_SetParam(float value, int param, IntPtr x);

    [DllImport("OSLNative")]
    private static extern void Delay_Clear(IntPtr x);

    [DllImport("OSLNative")]
    private static extern void Delay_SetMode(int mode, IntPtr x);

    [DllImport("OSLNative")]
    public static extern void SetArrayToSingleValue(float[] a, int length, float val);

    private bool containsTrigger(float[] buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] > 0f && lastTriggerFloat <= 0f)
            {
                return true;
            }
            lastTriggerFloat = buffer[i];
        }
        return false;
    }

    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        if (!recursionCheckPre()) return; // checks and avoids fatal recursions

        if (modTimeBuffer.Length != buffer.Length)
            System.Array.Resize(ref modTimeBuffer, buffer.Length);
        if (modFeedbackBuffer.Length != buffer.Length)
            System.Array.Resize(ref modFeedbackBuffer, buffer.Length);
        if (modTriggerBuffer.Length != buffer.Length)
            System.Array.Resize(ref modTriggerBuffer, buffer.Length);
        if (modMixBuffer.Length != buffer.Length)
            System.Array.Resize(ref modMixBuffer, buffer.Length);

        // we don't know if these will be overwritten upstream, so better make them fresh, see previous mixer bug.
        SetArrayToSingleValue(modTimeBuffer, modTimeBuffer.Length, 0f);
        SetArrayToSingleValue(modFeedbackBuffer, modFeedbackBuffer.Length, 0f);
        SetArrayToSingleValue(modTriggerBuffer, modTriggerBuffer.Length, 0f);
        SetArrayToSingleValue(modMixBuffer, modMixBuffer.Length, 0f);

        //Process mod inputs if plugged in:
        if (sigModTrigger != null)
        {
            sigModTrigger.processBuffer(modTriggerBuffer, dspTime, channels);

            if (containsTrigger(modTriggerBuffer))
            {
                modTriggerState = true;
                shouldClear = true;
            }
            else
            {
                modTriggerState = false;
            }
        }

        modFeedbackVal = 0;

        if (sigModTime != null)
        {
            sigModTime.processBuffer(modTimeBuffer, dspTime, channels);
            //We do not have to choose a value here bc time modulation works on audiorate; we just pass the whole buffer to the native code later
        }

        if (sigModFeedback != null)
        {
            sigModFeedback.processBuffer(modFeedbackBuffer, dspTime, channels);
            modFeedbackVal = modFeedbackBuffer[0];
        }

        float wet = p[(int)Param.P_WET];
        float dry = p[(int)Param.P_DRY];
        if (sigModMix != null)
        {
            sigModMix.processBuffer(modMixBuffer, dspTime, channels);
            float tmp = Mathf.Pow(wet, 2) + modMixBuffer[0]; //expecting input values in range [-1...1]
            tmp = Mathf.Clamp01(tmp);
            //TODO: not sure if sqrt crossfade is the best solution here; the signals obviously do have SOME correlation...
            wet = Utils.equalPowerCrossfadeGain(tmp);
            dry = Utils.equalPowerCrossfadeGain(1 - tmp);
        }

        //Clear buffer if necessary:
        if (shouldClear)
        {
            Delay_Clear(x);
            shouldClear = false;
        }

        //process input signal:
        if (sigIn != null)
        {
            sigIn.processBuffer(buffer, dspTime, channels);
        }

        //Set all delay params:
        Delay_SetParam(timeCenter * Mathf.Pow(2, Utils.map(Mathf.Clamp01(p[(int)Param.P_TIME]), 0f, 1f, -timeRange, timeRange)), (int)Param.P_TIME, x);
        //Delay_SetParam(Utils.map(Mathf.Pow(Mathf.Clamp01(p[(int)Param.P_TIME]), 3), 0, 1, modeMinTime, modeMaxTime), (int)Param.P_TIME, x);
        Delay_SetParam(Utils.map(Mathf.Clamp01(p[(int)Param.P_FEEDBACK] + modFeedbackVal), 0, 1, MIN_FEEDBACK, MAX_FEEDBACK), (int)Param.P_FEEDBACK, x);
        Delay_SetParam(wet, (int)Param.P_WET, x);
        Delay_SetParam(dry, (int)Param.P_DRY, x);

        //Process delay:
        //We need to process the delay even if there is currently no input,
        //bc there could be unconsumed samples from a previous input left in the delay line.
        //To optimize, we could store the timestamp when the last input connection was removed.
        //Then we only have to process if P_TIME is larger then the elapsed time since the connection was removed.
        //Delay_Process(buffer, buffer.Length, channels, x);
        Delay_Process(buffer, modTimeBuffer, null, modMixBuffer, buffer.Length, channels, x);

        recursionCheckPost();
    }
}

