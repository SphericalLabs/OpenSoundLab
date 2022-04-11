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

public class keyFrequencySignalGenerator : signalGenerator {
  
  float keyMultConst = Mathf.Pow(2, 1f / 12); // not used anymore, update native api?

  public int octave = 0;
  int curKey = -1;
  int semitone = 0; // as key number, not in hertz

  [DllImport("SoundStageNative")]
  public static extern void KeyFrequencySignalGenerator(float[] buffer, int length, int channels, int semitone, float keyMultConst, ref float filteredVal);

  public void UpdateKey(int k) {
    curKey = k;
    semitone = k + octave * 12;
  }

  public float getMult(int k) { // only used in xyloroll
    semitone = k + octave * 12;
    return semitone;
    //return Mathf.Pow(keyMultConst, semitone);
  }

  public void updateOctave(int n) {
    octave = n;
    semitone = curKey + octave * 12;
  }
  float filteredVal = 0;

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    if (!recursionCheckPre()) return; // checks and avoids fatal recursions
    KeyFrequencySignalGenerator(buffer, buffer.Length, channels, semitone, keyMultConst, ref filteredVal);
    recursionCheckPost();
  }
}