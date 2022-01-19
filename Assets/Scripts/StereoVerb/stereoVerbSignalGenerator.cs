// Copyright 2017 Google LLC
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

using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

public class stereoVerbSignalGenerator : signalGenerator {

    public enum Param : int
    {
        P_ROOMSIZE,
        P_DAMPING,
        P_DRY,
        P_WET,
        P_WIDTH,
        P_FREEZE,
        P_N
    };

    /* These mirror some of the Freeverb parameters, therefore the C++ style naming. 
     If you change these, please also make the corresponding change in tuning.h of freeverb sources.
    Otherwise behavior is undefined.
     */
    const float kRoomSizeMin = 0.7f;
    const float kRoomSizeMax = kRoomSizeMin + 0.299f; //HB edit

    const float MIN_WET = 0;
    const float MAX_WET = 1;
    const float MIN_DRY = 0;
    const float MAX_DRY = 1;

    public signalGenerator incoming;

    private IntPtr x;
    private float[] p = new float[(int)Param.P_N];
    [DllImport("SoundStageNative")]
    private static extern void StereoVerb_Process(float[] buffer, int length, int channels, IntPtr x);
    [DllImport("SoundStageNative")]
    private static extern IntPtr StereoVerb_New(int sampleRate);
    [DllImport("SoundStageNative")]
    private static extern void StereoVerb_Free(IntPtr x);
    [DllImport("SoundStageNative")]
    private static extern void StereoVerb_SetParam(int param, float value, IntPtr x);
    [DllImport("SoundStageNative")]
    private static extern float StereoVerb_GetParam(int param, IntPtr x);

    public override void Awake() {
        base.Awake();
        x = StereoVerb_New(AudioSettings.outputSampleRate);
    }

    private void OnDestroy()
    {
        StereoVerb_Free(x);
    }

    void Update() {
    
    }

    public void SetParam(float value, int param)
    {
        switch (param)
        {
            case (int)Param.P_ROOMSIZE:
                p[param] = Utils.map(value, 0, 1, kRoomSizeMin, kRoomSizeMax);
                //Debug.Log("Set param " + param + " to value " + p[param]);
                break;
            case (int)Param.P_DAMPING:
                p[param] = value;
                break;
            case (int)Param.P_WET:
                p[param] = Utils.map(value, 0, 1, MIN_WET, MAX_WET, 0.3f);
                break;
            case (int)Param.P_DRY:
                p[param] = Utils.map(value, 0, 1, MIN_DRY, MAX_DRY, 0.3f);
                //Debug.Log("Set param " + param + " to value " + p[param]);
                break;
            case (int)Param.P_WIDTH:
                p[param] = value;
                break;
            case (int)Param.P_FREEZE:
                p[param] = value == 0 ? 1 : 0; //in this case, we want "on" to be "off" and vice versa :)
                break;
        }
        //Debug.Log("Set param " + param + " to value " + p[param]);
    }

    public override void processBuffer(float[] buffer, double dspTime, int channels) {
        //Set new parameters
        for (int i = 0; i < (int)Param.P_N; i++)
        {
            StereoVerb_SetParam(i, p[i], x);
        }

        if(incoming != null)
            incoming.processBuffer(buffer, dspTime, channels);
        //We process the reverb even if there is no input available, to make sure reverb tails decay to the end, and to enable spacy freeze pads.
        StereoVerb_Process(buffer, buffer.Length, channels, x);
    }
}
