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

public class maracaSignalGenerator : signalGenerator {
  public float curShake = 0;
  float amp = 1f;

  [DllImport("SoundStageNative")]
  public static extern void MaracaProcessBuffer(float[] buffer, int length, int channels, float amp, ref double _phase, double _sampleDuration);

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    amp = curShake;
    MaracaProcessBuffer(buffer, buffer.Length, channels, amp, ref _phase, _sampleDuration);
  }
}
