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

public class scopeSignalGenerator : signalGenerator
{

  public signalGenerator incoming;
  float[] bufferCopy;

  [DllImport("SoundStageNative")]
  public static extern void CopyArray(float[] a, float[] b, int length);
  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);


  public override void Awake()
  {
    base.Awake();
    bufferCopy = new float[MAX_BUFFER_LENGTH];
  }

  // needed for scope, feeds it in AudioSource on Scope for FFT
  private void OnAudioFilterRead(float[] buffer, int channels)
  {
    if (incoming == null || bufferCopy == null) return;
    CopyArray(bufferCopy, buffer, buffer.Length);
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels)
  {
    if (bufferCopy.Length != buffer.Length)
      System.Array.Resize(ref bufferCopy, buffer.Length);

    // if silent, 0 out and return
    if (!incoming)
    {
      SetArrayToSingleValue(buffer, buffer.Length, 0.0f);
      SetArrayToSingleValue(bufferCopy, bufferCopy.Length, 0.0f);
      return;
    }

    if (incoming != null){
      incoming.processBuffer(buffer, dspTime, channels);
    }

    CopyArray(buffer, bufferCopy, buffer.Length); // copy over for FFT
  }
}
