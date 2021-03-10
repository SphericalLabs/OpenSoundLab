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
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class oscillatorSignalGenerator : signalGenerator
{
    public signalGenerator freqGen, ampGen, syncGen;
    public waveViz viz;

    public bool lfo = false;
    public float frequency = 220f;
    public float amplitude = 1;
    public float analogWave = 0;

    double lastIncomingDspTime = -1;
    float keyMultConst = Mathf.Pow(2, 1f / 12);

    float[] frequencyBuffer = new float[0];
    float[] amplitudeBuffer = new float[0];
    float[] syncBuffer = new float[0];
    float lastSyncValue = -1f;


    int counter = 0;

    float prevAmplitude;
    [DllImport("SoundStageNative")]
    public static extern void OscillatorSignalGenerator(float[] buffer, int length, int channels, ref double _phase, float analogWave, float frequency, float amplitude, float prevAmplitude, ref float prevSyncValue,
                                float[] frequencyBuffer, float[] amplitudeBuffer, float[] syncBuffer, bool bFreqGen, bool bAmpGen, bool bSyncGen, double _sampleDuration, ref double dspTime);

    public override void processBuffer(float[] buffer, double dspTime, int channels)
    {
        lastIncomingDspTime = dspTime;

        if (frequencyBuffer.Length != buffer.Length)
            System.Array.Resize(ref frequencyBuffer, buffer.Length);
        if (amplitudeBuffer.Length != buffer.Length)
            System.Array.Resize(ref amplitudeBuffer, buffer.Length);
        if (syncBuffer.Length != buffer.Length)
            System.Array.Resize(ref syncBuffer, buffer.Length);

        if (freqGen != null) freqGen.processBuffer(frequencyBuffer, dspTime, channels);
        if (ampGen != null) ampGen.processBuffer(amplitudeBuffer, dspTime, channels);
        if (syncGen != null) syncGen.processBuffer(syncBuffer, dspTime, channels);

        OscillatorSignalGenerator(buffer, buffer.Length, channels, ref _phase, analogWave, frequency, amplitude, prevAmplitude, ref lastSyncValue, frequencyBuffer, amplitudeBuffer, syncBuffer,
            freqGen != null, ampGen != null, syncGen != null, _sampleDuration, ref dspTime);

        
        // wave viz if there
        if (viz != null) viz.UpdateViz(buffer);

        // memory for next go around
        prevAmplitude = amplitude;
        //counter++;
    }

}


