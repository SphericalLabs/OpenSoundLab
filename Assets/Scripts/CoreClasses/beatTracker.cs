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

public class beatTracker : ScriptableObject {
  public delegate void TriggerEvent();
  public TriggerEvent triggerEvent;

  public delegate void ResetEvent();
  public ResetEvent resetEvent;

  int[] beatVals = new int[] { 1, 2, 4, 8, 12, 16, 24, 32, 64 }; //1,2,4,8,16,32,64
  int curBeat = 0;
  float[] timeValues = new float[] { };
  float lastTime = 0;

  int curBeatVal = 0;
  float curSwingVal = .5f;

  bool active = true;

  public bool MC = false;
  public void toggleMC(bool on) {
    if (MC == on) return;
    MC = on;
    if (MC) {
      masterControl.instance.beatUpdateEvent += beatUpdateEvent;
      masterControl.instance.beatResetEvent += beatResetEvent;
    } else {
      masterControl.instance.beatUpdateEvent -= beatUpdateEvent;
      masterControl.instance.beatResetEvent -= beatResetEvent;
    }
  }

  bool resetRequested = false;

  public void beatResetEvent() {
    lastTime = 0;
    curBeat = 0;
    resetRequested = true;
    resetEvent();
  }

  public void setTrigger(TriggerEvent t) {
    triggerEvent = t;
    toggleMC(true);
  }


  public void setTriggers(TriggerEvent t, ResetEvent r) {
    triggerEvent = t;
    resetEvent = r;
    toggleMC(true);
  }

  void OnDestroy() {
    if (MC) toggleMC(false);
  }

  public void toggle(bool on) {
    if (on == active) return;
    active = on;
  }

  public void updateBeat(int n) {
    setup(n, curSwingVal);
  }

  public void updateBeatNoTriplets(int n) {
    if (n == 4) n = 5;
    else if (n == 5) n = 7;
    else if (n == 6) n = 8;
    setup(n, curSwingVal);
  }

  public void updateSwing(float s) {
    setup(curBeatVal, s);
  }

  public void setup(int n, float swing) {
    curBeatVal = n;
    curSwingVal = swing;
    timeValues = new float[beatVals[n] * 2];
    float tempVal = .5f / beatVals[n];
    for (int i = 0; i < timeValues.Length; i++) timeValues[i] = tempVal * i;

    if (swing != .5f) {
      float tempoffset = swing - .5f;
      for (int i = 0; i < timeValues.Length; i++) {
        if (i % 2 == 1) timeValues[i] = timeValues[i] + tempoffset * tempVal;
      }
    }

    int candidate = 0;
    for (int i = 0; i < timeValues.Length; i++) {
      if (timeValues[i] < lastTime) candidate = i;
    }

    curBeat = candidate;
  }

  public void beatUpdateEvent(float t) {
    if (timeValues.Length == 0) return;

    if (resetRequested) {
      resetRequested = false;
      if (active) triggerEvent();
    }

    lastTime = t;
    int candidate = (curBeat + 1) % timeValues.Length;
    if (candidate != 0) {
      if (timeValues[candidate] < t) {
        curBeat = candidate;
        if (active) triggerEvent();
      }
    } else if (timeValues[candidate] < t && t < timeValues[curBeat]) {
      curBeat = candidate;
      if (active) triggerEvent();
    }
  }
}