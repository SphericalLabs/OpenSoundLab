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
using UnityEngine.SceneManagement;

public class microphoneSignalGenerator : signalGenerator {

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
  string activeDeviceString; 

  private float driftThreshold = 0; // Sample threshold to consider a drift correction
  private int lastWritePos = 0;
  private int lastReadPos = 0;
  private long totalWriteDistance = 0; // Total distance traveled by the mic recording
  private long totalReadDistance = 0; // Total distance traveled by the playback

  int targetBuffering;

  public override void Awake() {
    if (SceneManager.GetActiveScene().buildIndex == (int)masterControl.Scenes.Relay)
    {
        Destroy(gameObject);
    }

    base.Awake();

    sharedBuffer = new float[MAX_BUFFER_LENGTH];

    AudioSettings.GetDSPBufferSize(out bufferSize, out numBuffers);
    targetBuffering = bufferSize * 2;

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
    if (!activated) return;
    CheckAndCompensateDrift();
  }

  int micClipLength;
  int readPos, writePos;
  long correctedPlaybackPosition, correctedMicPosition, drift;
  void CheckAndCompensateDrift()
  {
    if (activeDeviceString == null) return;

    micClipLength = audioSource.clip.samples; // Total samples in the mic clip
    readPos = audioSource.timeSamples;
    writePos = Microphone.GetPosition(activeDeviceString);

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
    activeDeviceString = Microphone.devices[micID];
    Microphone.End(activeDeviceString);
    
    micClip = Microphone.Start(activeDeviceString, true, 5, 16000);

    yield return null;
    if (micClip != null) {
      audioSource.clip = micClip;
      audioSource.loop = true;
      while (!(Microphone.GetPosition(activeDeviceString) > targetBuffering)) { } 
      // waits until there are n samples in the buffer in order to avoid drop outs
      audioSource.Play();
    }
    
    // trigger a global sync, since spawning the Microphone can introduce severe lag
    // please note that this will be called from host and all clients, so the sync will be called each time a Microphone is instantiated and fully initialized
    NetworkSyncEventManager.Instance.UpdateSync(); 

    yield return null;

  }

  

  private void OnAudioFilterRead(float[] buffer, int channels) {

    activated = true;
    if (sharedBuffer.Length != buffer.Length)
      System.Array.Resize(ref sharedBuffer, buffer.Length);

    CopyArray(buffer, sharedBuffer, buffer.Length); // copy over for processBuffer, this is probably thread-safe since both calls are supposed to be in the audio thread
    SetArrayToSingleValue(buffer, buffer.Length, 0.0f); // this zeroes the buffer that is directly played from the AudioSource, so that only the OSL patch is heard
  }

  public override void processBufferImpl(float[] buffer, double dspTime, int channels) {
    if (!active || !activated) {
      return;
    }

    MicFunction(buffer, sharedBuffer, buffer.Length, amp);
  }
}
