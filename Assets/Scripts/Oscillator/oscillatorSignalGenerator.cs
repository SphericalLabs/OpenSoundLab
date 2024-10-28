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

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class oscillatorSignalGenerator : signalGenerator
{
  public signalGenerator freqExpGen, freqLinGen, ampGen, syncGen, pwmGen;
  public waveViz viz;

  public bool lfo = false;
  public float frequency = 261.6256f; // C4, MIDI 60, will usually be overwritten for Oscillator because of dial
  public float amplitude = 1;
  public float analogWave = 0;

  double lastIncomingDspTime = -1;
  float keyMultConst = Mathf.Pow(2, 1f / 12);

  float[] frequencyExpBuffer = new float[0];
  float[] frequencyLinBuffer = new float[0];
  float[] amplitudeBuffer = new float[0];
  float[] syncBuffer = new float[0];
  float[] pwmBuffer = new float[0];
  float lastSyncValue = -1f;

  int counter = 0;

  float prevAmplitude;
  float prevFrequency;

  [DllImport("OSLNative")]
  public static extern void OscillatorSignalGenerator(float[] buffer, int length, int channels, ref double _phase, float analogWave, float frequency, float prevFrequency, float amplitude, float prevAmplitude, ref float prevSyncValue,
                              float[] frequencyExpBuffer, float[] frequencyLinBuffer, float[] amplitudeBuffer, float[] syncBuffer, float[] pwmBuffer, bool bFreqExpGen, bool bFreqLinGen, bool bAmpGen, bool bSyncGen, bool bPwmGen, double _sampleDuration, ref double dspTime);
  
  [DllImport("OSLNative")] 
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);


  public override void processBufferImpl(float[] buffer, double dspTime, int channels)
  {
    if (!recursionCheckPre()) return; // checks and avoids fatal recursions

    lastIncomingDspTime = dspTime;

    if (frequencyExpBuffer.Length != buffer.Length)
      System.Array.Resize(ref frequencyExpBuffer, buffer.Length);
    if (frequencyLinBuffer.Length != buffer.Length)
      System.Array.Resize(ref frequencyLinBuffer, buffer.Length);
    if (amplitudeBuffer.Length != buffer.Length)
      System.Array.Resize(ref amplitudeBuffer, buffer.Length);
    if (syncBuffer.Length != buffer.Length)
      System.Array.Resize(ref syncBuffer, buffer.Length);
    if (pwmBuffer.Length != buffer.Length)
      System.Array.Resize(ref pwmBuffer, buffer.Length);

    // we don't know if these will be overwritten upstream, so better make them fresh, see previous mixer bug.
    SetArrayToSingleValue(frequencyExpBuffer, frequencyExpBuffer.Length, 0f);
    SetArrayToSingleValue(frequencyLinBuffer, frequencyLinBuffer.Length, 0f);
    SetArrayToSingleValue(amplitudeBuffer, amplitudeBuffer.Length, 0f);
    SetArrayToSingleValue(syncBuffer, syncBuffer.Length, 0f);
    SetArrayToSingleValue(pwmBuffer, pwmBuffer.Length, 0f);

    try
    {
      if (freqExpGen != null) freqExpGen.processBuffer(frequencyExpBuffer, dspTime, channels);
      if (freqLinGen != null) freqLinGen.processBuffer(frequencyLinBuffer, dspTime, channels);
      if (ampGen != null) ampGen.processBuffer(amplitudeBuffer, dspTime, channels);
      if (syncGen != null) syncGen.processBuffer(syncBuffer, dspTime, channels);
      if (pwmGen != null) pwmGen.processBuffer(pwmBuffer, dspTime, channels);
  } catch (System.StackOverflowException e) {
      Debug.LogWarning("catched a stackoverflow because of recursive patch connections");
    }

    OscillatorSignalGenerator(buffer, buffer.Length, channels, ref _phase, analogWave, frequency, prevFrequency, amplitude, prevAmplitude, ref lastSyncValue, frequencyExpBuffer, frequencyLinBuffer, amplitudeBuffer, syncBuffer, pwmBuffer,
        freqExpGen != null, freqLinGen != null, ampGen != null, syncGen != null, pwmGen != null, _sampleDuration, ref dspTime);


    // wave viz if there
    if (viz != null) viz.storeBuffer(buffer, channels);

    // memory for next go around
    prevAmplitude = amplitude;
    prevFrequency = frequency;

    recursionCheckPost();
  }

}


