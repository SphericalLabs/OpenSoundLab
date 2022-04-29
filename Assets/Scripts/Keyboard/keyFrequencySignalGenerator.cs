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