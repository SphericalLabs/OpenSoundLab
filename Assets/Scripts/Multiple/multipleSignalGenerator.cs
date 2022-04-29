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

  [DllImport("SoundStageNative")]
  public static extern void CopyArray(float[] a, float[] b, int length);
  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);
  [DllImport("SoundStageNative")]
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