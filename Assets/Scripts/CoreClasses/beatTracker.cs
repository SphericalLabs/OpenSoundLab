// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 
// 
// Copyright © 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright © 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright © 2017 Apache 2.0 Google LLC SoundStage VR
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

public class beatTracker : ScriptableObject {

    // beatTracker maps the 1bar 0-1 phase of masterControl to a series of steps defined in resolutions
    // the more steps you select, the faster your sequencer will go
    // swing is just offsetting every odd step by a percentage

    // right now the beatTracker can only go forward and loop around

    // triplets seem not implemented here, but that just means resolutions that are divisible by 3

    // how can it be that sequencers have another phase than the masterControl?
    // it just creates nextStep calls, but does not directly map the ramp phasor to the time in the sequence!

  // sequencers subscribe to these  
  public delegate void TriggerEvent();
  public TriggerEvent triggerEvent;

  // this is actually just forwarded from masterControl, since resets are not calculated here
  
  public delegate void ResetEvent();
  public ResetEvent resetEvent;

  int[] resolutions = new int[] { 1, 2, 4, 8, 12, 16, 24, 32, 64 }; 
  int curStep = 0;
  float[] calculatedSubSteps = new float[] { };
  float lastTime = 0;

  int curResolutionIndex = 0;
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
    curStep = 0;
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
    //if (n == 4) n = 5; // 12 -> 16
    //else if (n == 5) n = 7; // 16 -> 32
    //else if (n == 6) n = 8; // 24 -> 64
    setup(n, curSwingVal);
  }

  public void updateSwing(float s) {
    setup(curResolutionIndex, s);
  }

  public void setup(int n, float swing) {
    curResolutionIndex = n;
    curSwingVal = swing;
    calculatedSubSteps = new float[resolutions[curResolutionIndex] * 2];
    float stepDuration = .5f / resolutions[curResolutionIndex];
    for (int i = 0; i < calculatedSubSteps.Length; i++) calculatedSubSteps[i] = stepDuration * i;

    // swing offsets for every odd note
    if (swing != .5f) {
      float swingOffset = swing - .5f;
      for (int i = 0; i < calculatedSubSteps.Length; i++) {
        if (i % 2 == 1) calculatedSubSteps[i] = calculatedSubSteps[i] + swingOffset * stepDuration;
      }
    }

    int candidate = 0;
    for (int i = 0; i < calculatedSubSteps.Length; i++) {
      if (calculatedSubSteps[i] < lastTime) candidate = i;
    }

    curStep = candidate;
  }

  public void beatUpdateEvent(float t) {
    if (calculatedSubSteps.Length == 0) return;

    if (resetRequested) {
      resetRequested = false;
      if (active) triggerEvent();
    }

    lastTime = t;
    int candidate = (curStep + 1) % calculatedSubSteps.Length;
    if (candidate != 0) {
    // checks if we freshly passed beyond the next steps
      if (calculatedSubSteps[candidate] < t) {
        curStep = candidate;
        if (active) triggerEvent();
      }
    // checks if the wraparound has happened
    } else if (calculatedSubSteps[candidate] < t && t < calculatedSubSteps[curStep]) {
      curStep = candidate;
      if (active) triggerEvent();
    }
  }
}