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
using System.Runtime.InteropServices;

public class keyGateSignalGenerator : signalGenerator {

  public bool isHigh = false;
  public bool newKeyWasPressed = false;

  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] buffer, int length, float value);
    
  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    if (!recursionCheckPre()) return; // checks and avoids fatal recursions
    SetArrayToSingleValue(buffer, buffer.Length, isHigh ? 1f : 0f);
    if (newKeyWasPressed)
    {
      buffer[buffer.Length - 1] = buffer[buffer.Length - 2] = 0f;
      newKeyWasPressed = false;
    }
    recursionCheckPost();
  }

}