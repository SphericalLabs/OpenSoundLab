// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright � 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
// 
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at 
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
// 
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
// 
//
// Copyright � 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright � 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright � 2017 Apache 2.0 Google LLC SoundStage VR
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
  public List<signalGenerator> incomingSignals = new List<signalGenerator>();

  [DllImport("OSLNative")] public static extern void SetArrayToSingleValue(float[] a, int length, float val);
  [DllImport("OSLNative")] public static extern void AddArrays(float[] a, float[] b, int length);

  float[] tempBuff;

  public override void Awake() {
    base.Awake();
    tempBuff = new float[1];
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels) {

  if (tempBuff.Length != buffer.Length)
      System.Array.Resize(ref tempBuff, buffer.Length);

    SetArrayToSingleValue(buffer, buffer.Length, 0f);

    foreach (signalGenerator gen in incomingSignals){
      gen.processBuffer(tempBuff, dspTime, channels);
      AddArrays(buffer, tempBuff, buffer.Length);
    }
  }
}
