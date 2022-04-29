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
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class timelineTrackSignalGenerator : signalGenerator {

  bool newSignal = false;
  bool signalOn = false;

  timelineTrackComponentInterface _interface;
  public signalGenerator incoming;

  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);

  [DllImport("SoundStageNative")]
  public static extern bool GetBinaryState(float[] buffer, int length, int channels, ref float lastBuf);

  [DllImport("SoundStageNative")]
  public static extern bool IsPulse(float[] buffer, int length);

  public override void Awake() {
    base.Awake();
    _interface = GetComponent<timelineTrackComponentInterface>();
  }

  public void setSignal(bool on) //from outside
  {
    newSignal = true;
    signalOn = on;
  }

  float lastBuffer = 0;
  public override void processBuffer(float[] buffer, double dspTime, int channels) {

    if (incoming != null) {
      incoming.processBuffer(buffer, dspTime, channels);

      if (IsPulse(buffer, buffer.Length)) {
        _interface.updateSignal(false, timelineTrackComponentInterface.sigSource.signal);
        _interface.updateSignal(true, timelineTrackComponentInterface.sigSource.signal);
      } else {
        bool on = GetBinaryState(buffer, buffer.Length, channels, ref lastBuffer);
        _interface.updateSignal(on, timelineTrackComponentInterface.sigSource.signal);
      }
    }

    float val = signalOn ? 1.0f : -1.0f;
    SetArrayToSingleValue(buffer, buffer.Length, val);

    if (newSignal) {
      buffer[0] = buffer[1] = -1;
      newSignal = false;
    }
  }
}

