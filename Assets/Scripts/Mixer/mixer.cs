// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of OpenSoundLab.
//
// OpenSoundLab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenSoundLab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenSoundLab.  If not, see <http://www.gnu.org/licenses/>.

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
