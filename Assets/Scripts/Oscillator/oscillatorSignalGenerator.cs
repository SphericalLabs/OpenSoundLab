// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
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

  [DllImport("SoundStageNative")]
  public static extern void OscillatorSignalGenerator(float[] buffer, int length, int channels, ref double _phase, float analogWave, float frequency, float prevFrequency, float amplitude, float prevAmplitude, ref float prevSyncValue,
                              float[] frequencyExpBuffer, float[] frequencyLinBuffer, float[] amplitudeBuffer, float[] syncBuffer, float[] pwmBuffer, bool bFreqExpGen, bool bFreqLinGen, bool bAmpGen, bool bSyncGen, bool bPwmGen, double _sampleDuration, ref double dspTime);
  
  [DllImport("SoundStageNative")] 
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);


  public override void processBuffer(float[] buffer, double dspTime, int channels)
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


