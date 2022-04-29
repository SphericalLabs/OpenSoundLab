// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of SoundStage Lab.
//
// SoundStage Lab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// SoundStage Lab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SoundStage Lab.  If not, see <http://www.gnu.org/licenses/>.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class MicrophoneSignalGenerator : signalGenerator {

  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);
  [DllImport("SoundStageNative")]
  public static extern void CopyArray(float[] a, float[] b, int length);
  [DllImport("SoundStageNative")]
  public static extern void MicFunction(float[] a, float[] b, int length, float val);

  AudioClip micClip;
  AudioSource source;
  float[] sharedBuffer;
  bool activated = false;

  public Dictionary<float, float[]> freqBuffers = new Dictionary<float, float[]>();

  public float amp = 1;
  int micChannels = 1;
  public bool active = true;
  int curMicID = 0;

  public override void Awake() {
    base.Awake();
    sharedBuffer = new float[MAX_BUFFER_LENGTH];
  }

  void Start() {
    source = GetComponent<AudioSource>();
    SelectMic(0);
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
    source.Stop();
    Microphone.End(Microphone.devices[curMicID]);
    curMicID = num;

    micClip = Microphone.Start(Microphone.devices[num], true, 1, AudioSettings.outputSampleRate);

    yield return null;
    if (micClip != null) {
      source.clip = micClip;
      source.loop = true;
      while (!(Microphone.GetPosition(null) > 0)) { }
      source.Play();
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
