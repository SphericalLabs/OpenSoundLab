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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class multipleSignalGenerator : signalGenerator {
  public signalGenerator incoming;
  public List<multipleNodeSignalGenerator> nodes;

  bool flow = true;

  double lastIncomingDspTime = -1;
  float[] curBuffer;

  const int MAX_COUNT = 16;
  float[][] mergeBuffers;

  [DllImport("OSLNative")]
  public static extern void CopyArray(float[] a, float[] b, int length);
  [DllImport("OSLNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);
  [DllImport("OSLNative")]
  public static extern void AddArrays(float[] a, float[] b, int length);

  public override void Awake() {
    base.Awake();
    curBuffer = new float[MAX_BUFFER_LENGTH];
    mergeBuffers = new float[MAX_COUNT][];
    for (int i = 0; i < MAX_COUNT; ++i) {
      mergeBuffers[i] = new float[MAX_BUFFER_LENGTH];
    }
  }

  public void setFlow(bool on) {
    flow = on;
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    if (flow) {

      if (lastIncomingDspTime != dspTime) {
        lastIncomingDspTime = dspTime;
        if (buffer.Length != curBuffer.Length)
          System.Array.Resize(ref curBuffer, buffer.Length);

        if (incoming == null) SetArrayToSingleValue(buffer, buffer.Length, 0.0f);
        else incoming.processBuffer(buffer, dspTime, channels);

        SetArrayToSingleValue(curBuffer, buffer.Length, 0.0f);
        CopyArray(buffer, curBuffer, buffer.Length);
      } else CopyArray(curBuffer, buffer, buffer.Length);

    } else {
      if (incoming == null) SetArrayToSingleValue(buffer, buffer.Length, 0.0f);
      else incoming.processBuffer(buffer, dspTime, channels);

      int count = nodes.Count;

      for (int i = 0; i < count; i++) {
        if (buffer.Length != mergeBuffers[i].Length)
          System.Array.Resize(ref mergeBuffers[i], buffer.Length);

        SetArrayToSingleValue(mergeBuffers[i], buffer.Length, 0.0f);

        if (i < nodes.Count) {
          if (nodes[i] != null) nodes[i].processBuffer(mergeBuffers[i], dspTime, channels);
        }
      }

      for (int i = 0; i < count; i++) AddArrays(buffer, mergeBuffers[i], buffer.Length);

    }
  }
}