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
using System.Runtime.InteropServices;

public class keyGateSignalGenerator : signalGenerator {

  public bool isHigh = false;
  public bool newKeyWasPressed = false;

  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] buffer, int length, float value);
    
  public override void processBuffer(float[] buffer, double dspTime, int channels) {

    SetArrayToSingleValue(buffer, buffer.Length, isHigh ? 1f : 0f);
    if (newKeyWasPressed)
    {
      buffer[buffer.Length - 1] = buffer[buffer.Length - 2] = 0f;
      newKeyWasPressed = false;
    }

  }

}