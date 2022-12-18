// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This file is part of OpenSoundLab, which is based on SoundStage VR.
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

public class clipPlayerComplex : clipPlayer {

  public float playbackSpeed = 1;
  public float amplitude = 1;
  public float headTrim = 0.5f;
  public float tailTrim = 0.5f;
  public float windowing = 0f;
  int windowLength = 0;

  public samplerDeviceInterface _sampleInterface;

  public Transform scrubTransform;
  public GameObject[] scrubIndicators;
  Vector2 scrubRange = new Vector2(.2f, -.2f);

  public signalGenerator freqExpGen, freqLinGen, ampGen, seqGen;
  public signalGenerator headGen, tailGen;
  public float speedRange = 1;

  public bool active = true;

  float _lastBuffer = 0;
  float[] lastSeqGen;

  Texture2D tex;
  public Renderer waverend;
  public Color32 waveBG = Color.black; // currently overridden in prefab
  public Color32 waveLine = Color.white;
  int wavewidth = 512 * 2;
  int waveheight = 64 * 4;
  Color32[] wavepixels;

  float[] freqExpBuffer;
  float[] freqLinBuffer;
  float[] ampBuffer;
  float[] seqBuffer;
  float[] headBuffer;
  float[] tailBuffer;

  float lastPlaybackSpeed = 0f;
  float lastAmplitude = 0f;

  [DllImport("SoundStageNative")]
  public static extern float ClipSignalGenerator(float[] buffer, float[] freqExpBuffer, float[] freqLinBuffer, float[] ampBuffer, float[] seqBuffer, int length, float[] lastSeqGen, int channels, bool freqExpGen, bool freqLinGen, bool ampGen, bool seqGen, float floatingBufferCount
, int[] sampleBounds, float playbackSpeed, float lastPlaybackSpeed, System.IntPtr clip, int clipChannels, float amplitude, float lastAmplitude, bool playdirection, bool looping, double _sampleDuration, int bufferCount, ref bool active, int windowLength);

  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);

  public override void Awake() {
    base.Awake();
    freqExpBuffer = new float[MAX_BUFFER_LENGTH];
    freqLinBuffer = new float[MAX_BUFFER_LENGTH];
    ampBuffer = new float[MAX_BUFFER_LENGTH];
    seqBuffer = new float[MAX_BUFFER_LENGTH];
    headBuffer = new float[MAX_BUFFER_LENGTH];
    tailBuffer = new float[MAX_BUFFER_LENGTH];
  }

  void Start() {
    lastSeqGen = new float[] { 0, 0 };
    if (!loaded) toggleWaveDisplay(false);
  }

  public override menuItem.deviceType queryDeviceType() {
    return menuItem.deviceType.Sampler;
  }

  public override void toggleWaveDisplay(bool on) {
    if (_waveDisplayAnimation != null) StopCoroutine(_waveDisplayAnimation);
    _waveDisplayAnimation = StartCoroutine(waveDisplayAnimation(on));
  }

  Coroutine _waveDisplayAnimation;
  IEnumerator waveDisplayAnimation(bool on) {
    if (on) {

      waverend.gameObject.SetActive(on);
    } else {
      scrubTransform.gameObject.SetActive(on);
      for (int i = 0; i < scrubIndicators.Length; i++) scrubIndicators[i].SetActive(on);
    }

    if (on) {
      float timer = 0;
      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 12);
        waverend.material.SetFloat("_EmissionGain", Mathf.Lerp(0, 1f, timer));
        yield return null;
      }
      timer = 0;
      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 3);
        waverend.material.SetFloat("_EmissionGain", Mathf.Lerp(1, .55f, timer));
        yield return null;
      }
    } else {
      float timer = 0;
      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 5);
        waverend.material.SetFloat("_EmissionGain", Mathf.Lerp(.55f, 0, timer));
        yield return null;
      }
    }

    if (!on) {
      waverend.gameObject.SetActive(on);
    } else {
      scrubTransform.gameObject.SetActive(on);
      for (int i = 0; i < scrubIndicators.Length; i++) scrubIndicators[i].SetActive(on);
    }
  }

  public float headOffset = 0f;
  public float tailOffset = 0f;
  public int consolidatedSampleLength = -1; // sample length regardless of channel count
  int padding = 10; // should not load sample smaller than padding samples
  int[] tempBounds = new int[] { 0, 0 };

  public void updateSampleBounds() {
    if (!loaded) return;

    // todo: only calc on sample load?
    // otherwise NaN errors at times
    consolidatedSampleLength = (int)(clipSamples.Length / clipChannels - 1); 

    if(calcAndCheckTempBounds()){
      sampleBounds[0] = tempBounds[0];
      sampleBounds[1] = tempBounds[1];
    }

  }

  bool calcAndCheckTempBounds(){
    tempBounds[0] = (int)(consolidatedSampleLength * Mathf.Clamp01((headGen == null) ? trackBounds.x : headOffset));
    tempBounds[1] = (int)(consolidatedSampleLength * Mathf.Clamp01((tailGen == null) ? trackBounds.y : tailOffset));

    if (
      tempBounds[0] >= 0
      && tempBounds[0] < consolidatedSampleLength - padding
      && tempBounds[1] > 0
      && tempBounds[1] <= consolidatedSampleLength
      && tempBounds[0] <= tempBounds[1] - padding
      )
    {
      return true;
    } else {
      return false;
    }
      

    //if (tempBounds[0] < 0) sampleBounds[0] = 0;
    //if (tempBounds[0] > consolidatedSampleLength) sampleBounds[0] = consolidatedSampleLength - padding;
    //if (tempBounds[1] > consolidatedSampleLength) sampleBounds[1] = consolidatedSampleLength;
    //if (tempBounds[0] >= tempBounds[1])
    //{
    //  if (tempBounds[1] >= padding)
    //  {
    //    tempBounds[0] = tempBounds[1] - padding;
    //  }
    //  else
    //  {
    //    tempBounds[1] = consolidatedSampleLength;
    //    tempBounds[0] = consolidatedSampleLength - padding;
    //  }
    //}
  }

  public override void DrawClipTex() {
    tex = new Texture2D(wavewidth, waveheight, TextureFormat.RGBA32, true);
    tex.anisoLevel = 4;
    tex.filterMode = FilterMode.Bilinear;
    wavepixels = new Color32[wavewidth * waveheight];

    for (int i = 0; i < wavewidth; i++) {
      for (int i2 = 0; i2 < waveheight; i2++) {
        wavepixels[i2 * wavewidth + i] = waveBG;
      }
    }

    int centerH = waveheight / 2;
    int columnMult = Mathf.CeilToInt((float)clipSamples.Length / (wavewidth - 1));


    for (int i = 0; i < wavewidth; i++) {

      if (columnMult * i < clipSamples.Length) {
        // pow(0.5f) to make quite sounds appear louder, easier to use than log?
        // cf. https://stackoverflow.com/questions/26452026/how-to-make-waveform-rendering-more-interesting
        int curH = Mathf.FloorToInt((waveheight - 1) * .5f * Mathf.Pow(Mathf.Clamp01(Mathf.Abs(clipSamples[columnMult * i])), 0.5f)); 

        for (int i2 = 0; i2 < centerH; i2++) {
          if (i2 < curH) wavepixels[(centerH - i2) * wavewidth + i] = wavepixels[(centerH + i2) * wavewidth + i] = waveLine;
          else wavepixels[(centerH - i2) * wavewidth + i] = wavepixels[(centerH + i2) * wavewidth + i] = waveBG;
        }
      }
    }

    tex.SetPixels32(wavepixels);
    tex.Apply(true);
    waverend.material.mainTexture = tex;
  }

  public bool looping = true;
  public bool playdirection = true;
  public void Play() {
    _lastBuffer = sampleBounds[0];
    active = true;
  }

  public void Back() {
    _lastBuffer = playdirection ? sampleBounds[0] : sampleBounds[1];
  }

  public void togglePause(bool on) {
    active = on;
  }

  public void Loop() {
    active = true;
    looping = true;
  }

  float lastScrubTarg = 0;
  public void grabScrub(bool on) {
    scrubGrabbed = on;
    float targ = Mathf.InverseLerp(scrubRange.x, scrubRange.y, scrubTransform.localPosition.x);
    lastScrubTarg = scrubTarg = (int)((clipSamples.Length / clipChannels - 1) * targ);
  }

  public void updateTurntableDelta(float d) {
    if (!scrubGrabbed) turntableDelta += d;
  }

  public bool turntableGrabbed = false;
  public float turntableDelta = 0; //seconds scrubbed

  public bool scrubGrabbed = false;
  float samplePos = 0;
  float scrubTarg = 0;

  float cumScrubAmount = 0;
  void Update() {
    if (!scrubGrabbed) {
      Vector3 pos = scrubTransform.localPosition;
      pos.x = Mathf.Lerp(scrubRange.x, scrubRange.y, samplePos);
      scrubTransform.localPosition = pos;


    } else if (scrubGrabbed) {
      float targ = Mathf.InverseLerp(scrubRange.x, scrubRange.y, scrubTransform.localPosition.x);
      scrubTarg = (int)((clipSamples.Length / clipChannels - 1) * targ);
      cumScrubAmount += (scrubTarg - lastScrubTarg);
      lastScrubTarg = scrubTarg;

    }
  }

  public float getScrubAmount() {
    float s = cumScrubAmount * (float)_sampleDuration;
    cumScrubAmount = 0;
    return s;
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    if (!recursionCheckPre()) return; // checks and avoids fatal recursions
    if (!loaded) return;
    floatingBufferCount = _lastBuffer;

    if (freqExpBuffer.Length != buffer.Length)
      System.Array.Resize(ref freqExpBuffer, buffer.Length);
    if (freqLinBuffer.Length != buffer.Length)
      System.Array.Resize(ref freqLinBuffer, buffer.Length);
    if (ampBuffer.Length != buffer.Length)
      System.Array.Resize(ref ampBuffer, buffer.Length);
    if (seqBuffer.Length != buffer.Length)
      System.Array.Resize(ref seqBuffer, buffer.Length);

    if (headBuffer.Length != buffer.Length)
      System.Array.Resize(ref headBuffer, buffer.Length);
    if (tailBuffer.Length != buffer.Length)
      System.Array.Resize(ref tailBuffer, buffer.Length);

    SetArrayToSingleValue(freqExpBuffer, freqExpBuffer.Length, 0f);
    SetArrayToSingleValue(freqLinBuffer, freqLinBuffer.Length, 0f);
    SetArrayToSingleValue(ampBuffer, ampBuffer.Length, 0f);
    SetArrayToSingleValue(seqBuffer, seqBuffer.Length, 0f);
    SetArrayToSingleValue(headBuffer, headBuffer.Length, 0f);
    SetArrayToSingleValue(tailBuffer, tailBuffer.Length, 0f);

    if (freqExpGen != null) freqExpGen.processBuffer(freqExpBuffer, dspTime, channels);
    if (freqLinGen != null) freqLinGen.processBuffer(freqLinBuffer, dspTime, channels);
    if (ampGen != null) ampGen.processBuffer(ampBuffer, dspTime, channels);
    if (seqGen != null) seqGen.processBuffer(seqBuffer, dspTime, channels);

    if (headGen != null)
    {
      headGen.processBuffer(headBuffer, dspTime, channels);
      headOffset = Mathf.Clamp01(headBuffer[0] + headTrim); // use buffer for offset, take only first sample of buffer
    } else {
      headOffset = 0f;
    }

    if (tailGen != null)
    {
      tailGen.processBuffer(tailBuffer, dspTime, channels);
      tailOffset = Mathf.Clamp01(tailBuffer[0] + tailTrim); // use buffer for offset, take only first sample of buffer
    }
    else
    {
      tailOffset = 0f;
    }


    updateSampleBounds(); // might be overkill!

    if (!scrubGrabbed && !turntableGrabbed) {
      bool curActive = active;
      windowLength = Mathf.FloorToInt(windowing * 4800);

      floatingBufferCount = ClipSignalGenerator(buffer, freqExpBuffer, freqLinBuffer, ampBuffer, seqBuffer, buffer.Length, lastSeqGen, channels, freqExpGen != null, freqLinGen != null, ampGen != null, seqGen != null, floatingBufferCount, sampleBounds,
          playbackSpeed, lastPlaybackSpeed, m_ClipHandle.AddrOfPinnedObject(), clipChannels, amplitude, lastAmplitude, playdirection, looping, _sampleDuration, bufferCount, ref active, windowLength);
      if (curActive != active) _sampleInterface.playEvent(active);

      lp_filter[0] = buffer[buffer.Length - 2];
      lp_filter[1] = buffer[buffer.Length - 1];
    } else if (scrubGrabbed) // keeping scrub non-native because such an edge-case and maxes out at 2 instances
      {
      float amount = (scrubTarg - _lastBuffer) / (buffer.Length / channels);

      for (int i = 0; i < buffer.Length; i += channels) {
        bufferCount = Mathf.RoundToInt(floatingBufferCount);
        floatingBufferCount += amount;

        float endAmplitude = amplitude;
        if (ampGen != null) endAmplitude = endAmplitude * ((ampBuffer[i] + 1) / 2f); // -1,1 > 0,1
        buffer[i] = lp_filter[0] = lp_filter[0] * .9f + .1f * clipSamples[bufferCount * clipChannels] * endAmplitude;
        if (clipChannels == 2) buffer[i + 1] = lp_filter[1] = lp_filter[1] * .9f + .1f * clipSamples[bufferCount * clipChannels + 1] * endAmplitude;
        else buffer[i + 1] = buffer[i];

        dspTime += _sampleDuration;
      }
    } else {
      float amount = turntableDelta * (float)_sampleRate * channels / buffer.Length;
      for (int i = 0; i < buffer.Length; i += channels) {
        bufferCount = Mathf.RoundToInt(floatingBufferCount);
        floatingBufferCount += amount;

        if (bufferCount > sampleBounds[1]) floatingBufferCount = bufferCount = sampleBounds[0];

        else if (bufferCount < sampleBounds[0]) floatingBufferCount = bufferCount = sampleBounds[1];

        float endAmplitude = amplitude;
        if (ampGen != null) endAmplitude = endAmplitude * ((ampBuffer[i] + 1) / 2f);

        buffer[i] = lp_filter[0] = lp_filter[0] * .9f + .1f * clipSamples[bufferCount * clipChannels] * endAmplitude;
        if (clipChannels == 2) buffer[i + 1] = lp_filter[1] = lp_filter[1] * .9f + .1f * clipSamples[bufferCount * clipChannels + 1] * endAmplitude;
        else buffer[i + 1] = buffer[i];

        dspTime += _sampleDuration;
      }
      turntableDelta = 0;

    }

    _lastBuffer = floatingBufferCount;
    samplePos = (float)floatingBufferCount * clipChannels / clipSamples.Length;

    lastAmplitude = amplitude;
    lastPlaybackSpeed = playbackSpeed;
    recursionCheckPost();
  }

  float[] lp_filter = new float[] { 0, 0 };
}