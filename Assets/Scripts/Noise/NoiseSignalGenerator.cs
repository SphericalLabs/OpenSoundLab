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
using System.Runtime.InteropServices;

public class NoiseSignalGenerator : signalGenerator {

  float speedPercent = 1;
  int speedFrames = 1;

  int maxLength = 11025 * 16; //  max length of one random value in samples
  int counter = 0;

  float curSample = -1.0f;

  [DllImport("SoundStageNative")]
  public static extern int NoiseProcessBuffer(float[] buffer, ref float sample, int length, int channels, float frequency, int counter, int speedFrames, ref bool updated);

  public bool updated = false;

  public void updatePercent(float per) {
    if (speedPercent == per) return;
    speedPercent = per;
    speedFrames = Mathf.RoundToInt(maxLength * Mathf.Pow(Mathf.Clamp01(1f - per / 0.95f), 4));
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    counter = NoiseProcessBuffer(buffer, ref curSample, buffer.Length, channels, speedPercent, counter, speedFrames, ref updated);
  }
}
