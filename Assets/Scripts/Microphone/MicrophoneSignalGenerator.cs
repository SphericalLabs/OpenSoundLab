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
  int micChannels = 1;
  public bool active = true;
  int curMicID = 0;

  int selectedMic = 0;
  private float checkInterval = 0.1f; // Time in seconds to wait between checks
  private float nextCheckTime = 0f;
  private int lastMicPosition = 0;
  private float driftThreshold = 1000; // Sample threshold to consider a drift correction
  int desiredBuffering;

  public override void Awake() {
    base.Awake();
    sharedBuffer = new float[MAX_BUFFER_LENGTH];

    AudioSettings.GetDSPBufferSize(out bufferSize, out numBuffers);
    desiredBuffering = bufferSize * 32;
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
    if (Time.time >= nextCheckTime)
    {
      CheckAndCompensateDrift();
      nextCheckTime = Time.time + checkInterval;
    }
  }

  void CheckAndCompensateDrift()
  {
    
    int micPosition = Microphone.GetPosition(Microphone.devices[selectedMic]);
    int playbackPosition = audioSource.timeSamples;

    // Calculate drift
    int drift = micPosition - desiredBuffering - (playbackPosition + lastMicPosition - micPosition);
    lastMicPosition = micPosition;

    // Check if drift is significant
    if (Mathf.Abs(drift) > driftThreshold && Mathf.Abs(micPosition - playbackPosition) < AudioSettings.outputSampleRate * 2) // avoid compensating when recordHead was just looping!
    {
      // Compensate for drift by adjusting playback speed slightly
      // This is a very basic form of compensation and may not be perfect
      audioSource.pitch = drift < 0 ? 0.99f : 1.01f;
    }
    else
    {
      // If drift is within acceptable limits, ensure normal playback speed
      audioSource.pitch = 1.0f;
    }

    // modulo for looping!
    // can you count globally somehow?
    // can you detect looping for reading and writing?

    //if (playbackPosition > micPosition) {
    //  audioSource.time -= 0.2f;
    //}

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
    Microphone.End(Microphone.devices[curMicID]);
    curMicID = num;

    micClip = Microphone.Start(Microphone.devices[num], true, 10, AudioSettings.outputSampleRate);

    yield return null;
    if (micClip != null) {
      audioSource.clip = micClip;
      audioSource.loop = true;
      while (!(Microphone.GetPosition(Microphone.devices[num]) > desiredBuffering)) { } 
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
