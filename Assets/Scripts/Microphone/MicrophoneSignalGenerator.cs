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
using System;

public class MicrophoneSignalGenerator : signalGenerator {

  [DllImport("OSLNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);
  [DllImport("OSLNative")]
  public static extern void CopyArray(float[] a, float[] b, int length);
  [DllImport("OSLNative")]
  public static extern void MicFunction(float[] a, float[] b, int length, float val);


  AudioClip micClip;
  AudioSource audioSource;
  float[] sharedBuffer;
  bool activated = false;

  public Dictionary<float, float[]> freqBuffers = new Dictionary<float, float[]>();

  public float amp = 1;

  public bool active = true;

  int micID = 0;
  string selectedMic;

  private float checkInterval = 0.1f; // Time in seconds to wait between checks
  private float nextCheckTime = 0f;

  private float driftThreshold = 0; // Sample threshold to consider a drift correction
  private int lastWritePos = 0;
  private int lastReadPos = 0;
  private long totalWriteDistance = 0; // Total distance traveled by the mic recording
  private long totalReadDistance = 0; // Total distance traveled by the playback

  int targetBuffering;

  public override void Awake() {
    base.Awake();
    sharedBuffer = new float[MAX_BUFFER_LENGTH];

    AudioSettings.GetDSPBufferSize(out bufferSize, out numBuffers);
    targetBuffering = bufferSize * 8;

    //int minFreq = 0, maxFreq = 0;
    //Microphone.GetDeviceCaps(Microphone.devices[micID], out minFreq, out maxFreq);
    //Debug.Log("Microphone: " + micID + ", minFreq: " + minFreq + ", maxFreq: " + maxFreq);
  }

  int bufferSize, numBuffers;

  void Start() {
    audioSource = GetComponent<AudioSource>();    
    OVRManager.TrackingAcquired += trackingAcquired;
    OVRManager.TrackingAcquired += trackingLost;

    SelectMic(0);

  }

  void Update()
  {
    CheckAndCompensateDrift();
  }

  int micClipLength;
  int readPos, writePos;
  long correctedPlaybackPosition, correctedMicPosition, drift;
  void CheckAndCompensateDrift()
  {
    if (selectedMic == null) return;

    micClipLength = audioSource.clip.samples; // Total samples in the mic clip
    readPos = audioSource.timeSamples;
    writePos = Microphone.GetPosition(selectedMic);

    // Update total distances traveled, accounting for loops
    if (readPos < lastReadPos)
    {
      totalReadDistance += micClipLength; // Assuming playback clip is the same length as the recording
    }
    if (writePos < lastWritePos)
    {
      totalWriteDistance += micClipLength;
    }

    lastReadPos = readPos;
    lastWritePos = writePos;

    correctedPlaybackPosition = totalReadDistance + readPos;
    correctedMicPosition = totalWriteDistance + writePos;

    drift = correctedMicPosition - targetBuffering - correctedPlaybackPosition;

    if (Mathf.Abs(drift) > driftThreshold)
    {
      //audioSource.pitch = drift < 0 ? 0.98f : 1.02f;
      audioSource.pitch = Utils.map(drift, -2000, 2000, 0.95f, 1.05f);
    }
    else
    {
      audioSource.pitch = 1.0f;
    }

    //Debug.LogFormat("readHead: {0}, writeHead: {1}, distance: {2}, pitch: {3}", correctedPlaybackPosition.ToString("0000000000"), correctedMicPosition.ToString("0000000000"), drift.ToString("0000000000"), audioSource.pitch);
  }


  void OnApplicationFocus(bool hasFocus)
  {
    if(hasFocus) SelectMic(0); // restart mic in order to clear its buffer and avoid severe latency
  }

  void OnApplicationPause(bool isPaused)
  {
    if (!isPaused) SelectMic(0); // restart mic in order to clear its buffer and avoid severe latency
  }


  public void trackingAcquired(){
    SelectMic(0); // restart mic in order to clear its buffer and avoid severe latency
  }

  public void trackingLost(){
    SelectMic(0); // restart mic in order to clear its buffer and avoid severe latency
  }
  

  Coroutine _MicActivateRoutine;
  void SelectMic(int num) {
    if (num >= Microphone.devices.Length) {
      return;
    }

    if (_MicActivateRoutine != null) StopCoroutine(_MicActivateRoutine);
    _MicActivateRoutine = StartCoroutine(MicActivateRoutine(num));
  }

  IEnumerator MicActivateRoutine(int num) {
    audioSource.Stop();
    selectedMic = Microphone.devices[micID];
    Microphone.End(selectedMic);
    
    micClip = Microphone.Start(selectedMic, true, 10, AudioSettings.outputSampleRate);

    yield return null;
    if (micClip != null) {
      audioSource.clip = micClip;
      audioSource.loop = true;
      while (!(Microphone.GetPosition(selectedMic) > targetBuffering)) { } 
      // waits until there are n samples in the buffer in order to avoid drop outs
      audioSource.Play();
    }

    yield return null;

  }

  

  private void OnAudioFilterRead(float[] buffer, int channels) {

    activated = true;
    if (sharedBuffer.Length != buffer.Length)
      System.Array.Resize(ref sharedBuffer, buffer.Length);

    CopyArray(buffer, sharedBuffer, buffer.Length);
    SetArrayToSingleValue(buffer, buffer.Length, 0.0f);
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    if (!active || !activated) {
      return;
    }

    MicFunction(buffer, sharedBuffer, buffer.Length, amp);
  }
}
