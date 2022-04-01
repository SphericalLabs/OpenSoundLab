// Copyright 2017-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
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

public class mixer : signalGenerator {
  public List<signalGenerator> incoming = new List<signalGenerator>();

  [DllImport("SoundStageNative")] public static extern void SetArrayToSingleValue(float[] a, int length, float val);
  [DllImport("SoundStageNative")] public static extern void AddArrays(float[] a, float[] b, int length);

  float[] tempBuff;

  public override void Awake() {
    base.Awake();
    tempBuff = new float[1];
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels) {

  if (tempBuff.Length != buffer.Length)
      System.Array.Resize(ref tempBuff, buffer.Length);

    SetArrayToSingleValue(buffer, buffer.Length, 0f);

    foreach (signalGenerator gen in incoming){
      gen.processBuffer(tempBuff, dspTime, channels);
      AddArrays(buffer, tempBuff, buffer.Length);
    }
  }
}
