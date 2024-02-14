// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright � 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 
// 
// Copyright � 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright � 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright � 2017 Apache 2.0 Google LLC SoundStage VR
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
        P_MODE_MIN_TIME,
        P_MODE_MAX_TIME,
        P_N
    };

    const int INTERPOLATION_NONE = 1;
    const int INTERPOLATION_LINEAR = 2;

    //const int DELAYMODE_SIMPLE = 0; deprecated
    const int DELAYMODE_OVERSAMPLED = 1;
    const int DELAYMODE_EFFICIENT = 2;

    public const float MAX_TIME = 12.5f; // * 1000 ms.
    public const float MIN_FEEDBACK = 0;
    public const float MAX_FEEDBACK = 1f;
    public const float MIN_WET = -96; //dB
    public const float MAX_WET = 0; //dB
    public const float MIN_DRY = -96; //dB
    public const float MAX_DRY = 0; //dB

    //currently selected min/max range in samples.
    public int modeMinTime = 1;
    public int modeMaxTime = 1; //cannot be larger than const MAX_TIME.

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
        switch(param)
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


    public void SetTimeRange(int mode)
    {
        switch (mode)
        {
            case 0:
                modeMinTime = (int)( 0.01f * sampleRate);
                modeMaxTime = (int)(12.5f * sampleRate);
                break;
            case 1:
                modeMinTime = (int)(0.01f * sampleRate);
                modeMaxTime = (int)(0.10f * sampleRate);
                break;
            case 2:
                modeMinTime = (int)(0.10f * sampleRate);
                modeMaxTime = (int)(3.00f * sampleRate);
                break;
            case 3:
                modeMinTime = (int)( 3.00f * sampleRate);
                modeMaxTime = (int)(30.00f * sampleRate);
                break;
        }
        Delay_SetParam(modeMinTime, (int)Param.P_MODE_MIN_TIME, x);
        Delay_SetParam(modeMaxTime, (int)Param.P_MODE_MAX_TIME, x);
    }

    [DllImport("SoundStageNative")]
    //private static extern void Delay_Process(float[] buffer, int length, int channels, IntPtr x);
    private static extern void Delay_Process(float[] buffer, float[] timeBuffer, float[] feedbackBuffer, float[] mixBuffer, int n, int channels, IntPtr x);

    [DllImport("SoundStageNative")]
    private static extern IntPtr Delay_New(int maxDelayTimeSamples);

    [DllImport("SoundStageNative")]
    private static extern void Delay_Free(IntPtr x);

    [DllImport("SoundStageNative")]
    private static extern void Delay_SetParam(float value, int param, IntPtr x);

    [DllImport("SoundStageNative")]
    private static extern void Delay_Clear(IntPtr x);

    [DllImport("SoundStageNative")]
    private static extern void Delay_SetMode(int mode, IntPtr x);

    [DllImport("SoundStageNative")]
    public static extern void SetArrayToSingleValue(float[] a, int length, float val);

    private bool containsTrigger(float[] buffer){
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

  public override void processBuffer(float[] buffer, double dspTime, int channels)
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
        Delay_SetParam(Utils.map(Mathf.Pow(Mathf.Clamp01(p[(int)Param.P_TIME]), 3), 0, 1, modeMinTime, modeMaxTime), (int)Param.P_TIME, x);
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

