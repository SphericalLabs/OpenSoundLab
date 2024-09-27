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
using System;
using System.Runtime.InteropServices;

public class waveTranscribeRecorder : signalGenerator {
  public int duration = 300;
  public bool recording = false;
  public bool playing = false;

  public GameObject tapePrefab;
  public Transform tapeHolder;
  public tape curTape;

  public TextMesh saveText;

  float[] sampleBuffer;
  // Int16[] cheaperBuffer; // -> todo?
  int maxDuration = 300; // in seconds
  public int truncatedBufferLength = 0;
  int curBufferIndex = 0;

  public Transform scrubTransform;
  Vector2 scrubRange = new Vector2(.4f, -.4f);

  public signalGenerator incoming;
  recorderDeviceInterface _deviceInterface;

  // WAVE DRAW TOOLS
  Texture2D tex;
  public Renderer waverend;
  int wavewidth = 1024;
  int waveheight = 512;
  public int period = 512;
  Color32[] wavepixels;
  int curWaveW = 0;
  int lastWaveH = 0;
  public Color32 waveBG = Color.black; // overridden in prefab
  public Color32 waveLine = Color.white;
  int columnMult = 1;
  double _sampleRateOverride;

  float[] lastRecSig, lastPlaySig, lastBackSig;

  [DllImport("OSLNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);


  public override void Awake() {
    base.Awake();
    _deviceInterface = GetComponent<recorderDeviceInterface>();
    sampleBuffer = new float[(int)(maxDuration * AudioSettings.outputSampleRate) * 2];
    truncatedBufferLength = sampleBuffer.Length;

    _sampleRateOverride = AudioSettings.outputSampleRate;

    // don't do mipmapping here to keep computation low
    tex = new Texture2D(wavewidth, waveheight, TextureFormat.RGBA32, false);
    wavepixels = new Color32[wavewidth * waveheight];
    waverend.material.mainTexture = tex;

    lastRecSig = new float[] { 0, 0 };
    lastPlaySig = new float[] { 0, 0 };
    lastBackSig = new float[] { 0, 0 };
  }

  public void updateDuration(int newduration) {
    duration = newduration;
    truncatedBufferLength = duration * AudioSettings.outputSampleRate * 2;
    columnMult = Mathf.CeilToInt((float)truncatedBufferLength / (wavewidth - 1));
    recalcTex();
    if (!playing) {
      tex.SetPixels32(wavepixels);
      tex.Apply(false);
    }
  }

  void recalcTex() {
    int centerH = waveheight / 2;

    for (int i = 0; i < wavewidth; i++) {
      if (columnMult * i < truncatedBufferLength) {
        int curH = Mathf.FloorToInt((waveheight - 1) * .5f * Mathf.Pow(Mathf.Clamp01(Mathf.Abs(sampleBuffer[columnMult * i])), 0.5f));

        for (int i2 = 0; i2 < centerH; i2++) {
          if (i2 < curH) wavepixels[(centerH - i2) * wavewidth + i] = wavepixels[(centerH + i2) * wavewidth + i] = waveLine;
          else wavepixels[(centerH - i2) * wavewidth + i] = wavepixels[(centerH + i2) * wavewidth + i] = waveBG;
        }
      }
    }
  }

  void Start() {
    Flush();
    tex.SetPixels32(wavepixels);
    tex.Apply(false);
    Vector3 pos = scrubTransform.localPosition;
    pos.x = Mathf.Lerp(scrubRange.x, scrubRange.y, 0);
    scrubTransform.localPosition = pos;
    columnMult = Mathf.CeilToInt((float)sampleBuffer.Length / (wavewidth - 1));
  }

  void scrubReset() {
    resetScrub = false;
    Vector3 pos = scrubTransform.localPosition;
    pos.x = scrubRange.x;
    scrubTransform.localPosition = pos;
  }

  bool resetScrub = false;
  float samplePos = 0; // for viz
  void Update() {
    if (resetScrub) scrubReset();
    if (!playing) return;

    tex.SetPixels32(wavepixels);
    tex.Apply(false);

    Vector3 pos = scrubTransform.localPosition;
    pos.x = Mathf.Lerp(scrubRange.x, scrubRange.y, samplePos);
    scrubTransform.localPosition = pos;
  }

  public void Flush() {
    if (bufferToWav.instance.savingInProgress) return;

    for (int i = 0; i < wavewidth; i++) {
      for (int i2 = 0; i2 < waveheight; i2++) {
        wavepixels[i2 * wavewidth + i] = waveBG;
      }
    }

    SetArrayToSingleValue(sampleBuffer, sampleBuffer.Length, 0f); 

    tex.SetPixels32(wavepixels);
    tex.Apply(false);

    if (curTape != null) Destroy(curTape.gameObject);
  }

  public void Save() {
    if (bufferToWav.instance.savingInProgress) return;
    string audioFilename = masterControl.instance.SaveDir + System.IO.Path.DirectorySeparatorChar + "Samples" + System.IO.Path.DirectorySeparatorChar +
        "Recordings" + System.IO.Path.DirectorySeparatorChar +
      string.Format("{0:yyyy-MM-dd_HH-mm-ss}.wav",
       DateTime.Now);

    bufferToWav.instance.Save(audioFilename, sampleBuffer, 2, truncatedBufferLength, saveText, this, _deviceInterface.normalizeSwitch.switchVal);
  }

  string curfilename = "";
  public override void updateTape(string s)
  {
    s = System.IO.Path.GetFileNameWithoutExtension(s);
    curfilename = s;
    if (curTape != null) Destroy(curTape.gameObject);
    curTape = (Instantiate(tapePrefab, tapeHolder, false) as GameObject).GetComponent<tape>();
    curTape.Setup(s, sampleManager.instance.sampleDictionary["Recordings"][s]);
  }

  public void Back() {
    curBufferIndex = 0;
    resetScrub = true;
  }

  public void Stop() {
    playing = false;
    resetScrub = true;
    _deviceInterface.buttons[1].phantomHit(playing);

    if (!playing && recording) {
      recording = false;
      _deviceInterface.buttons[0].phantomHit(recording);
    }
  }

  double lastIncomingDspTime = -1;
  //float[] oldBuffer;
  bool playingLastFrame = false;

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    //if (lastIncomingDspTime == dspTime) {

    //  for (int i = 0; i < buffer.Length; i++) {
    //    buffer[i] = oldBuffer[i];
    //  }
    //  return;
    //}
    //lastIncomingDspTime = dspTime;

    //oldBuffer = new float[buffer.Length];
    float[] recBuffer = new float[buffer.Length];
    float[] playBuffer = new float[buffer.Length];
    float[] backBuffer = new float[buffer.Length];
    if (_deviceInterface.recordTrigger.signal != null) _deviceInterface.recordTrigger.signal.processBuffer(recBuffer, dspTime, channels);
    if (_deviceInterface.playTrigger.signal != null) _deviceInterface.playTrigger.signal.processBuffer(playBuffer, dspTime, channels);
    if (_deviceInterface.backTrigger.signal != null) _deviceInterface.backTrigger.signal.processBuffer(backBuffer, dspTime, channels);

    if (incoming != null) incoming.processBuffer(buffer, dspTime, channels);
    for (int i = 0; i < buffer.Length; i += channels) {
      if (_deviceInterface.playTrigger.signal != null) {
        if (playBuffer[i] > lastPlaySig[1] && lastPlaySig[1] <= lastPlaySig[0]) {
          playing = !playing;
          _deviceInterface.buttons[1].phantomHit(playing);

          if (!playing && recording) {
            recording = false;
            _deviceInterface.buttons[0].phantomHit(recording);
          }

        }

        lastPlaySig[0] = lastPlaySig[1];
        lastPlaySig[1] = playBuffer[i];
      }

      if (_deviceInterface.recordTrigger.signal != null) {
        if (recBuffer[i] > lastRecSig[1] && lastRecSig[1] <= lastRecSig[0]) {
          recording = !recording;
          _deviceInterface.buttons[0].phantomHit(recording);

          if (recording && !playing) {
            playing = true;
            _deviceInterface.buttons[1].phantomHit(playing);
          }
        }
        lastRecSig[0] = lastRecSig[1];
        lastRecSig[1] = recBuffer[i];
      }

      if (_deviceInterface.backTrigger.signal != null) {
        if (backBuffer[i] > lastBackSig[1] && lastBackSig[1] <= lastBackSig[0]) {
          curBufferIndex = 0;
        }

        lastBackSig[0] = lastBackSig[1];
        lastBackSig[1] = backBuffer[i];
      }

      if (recording && playing) {
        sampleBuffer[curBufferIndex] = buffer[i];
        sampleBuffer[curBufferIndex + 1] = buffer[i + 1];
      } 

      if (playing && !recording)
      {
        buffer[i] = sampleBuffer[curBufferIndex];
        buffer[i + 1] = sampleBuffer[curBufferIndex + 1];
      }

      //if(!playing && !recording){
        // do nothing, just pass through buffers that have already been populated from incoming
      //}

      if (playing) {
        int centerH = waveheight / 2;
        if (curBufferIndex % columnMult == 0) {
          curWaveW = curBufferIndex / columnMult;

          if (curWaveW < wavewidth) {
            int curH = Mathf.FloorToInt((waveheight - 1) * .5f * Mathf.Clamp01(Mathf.Abs(sampleBuffer[curBufferIndex])));

            for (int i2 = 0; i2 < centerH; i2++) {
              if (i2 < curH) wavepixels[(centerH - i2) * wavewidth + curWaveW] = wavepixels[(centerH + i2) * wavewidth + curWaveW] = waveLine;
              else wavepixels[(centerH - i2) * wavewidth + curWaveW] = wavepixels[(centerH + i2) * wavewidth + curWaveW] = waveBG;

            }
          }

        }

        curBufferIndex = (curBufferIndex + 2);
        if (curBufferIndex >= truncatedBufferLength) {
          curBufferIndex = 0; //NOT RIGHT
          Stop();
        }

      }
    }
    samplePos = (float)curBufferIndex / truncatedBufferLength;
  }
}