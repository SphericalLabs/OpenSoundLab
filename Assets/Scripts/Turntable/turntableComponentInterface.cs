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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class turntableComponentInterface : MonoBehaviour {

  public Transform turntable;
  public clipPlayerComplex player;
  turntableUI _UI;

  float DPS = 270; //degrees per second

  void Awake() {
    _UI = GetComponentInChildren<turntableUI>();
  }

  public void addDelta(float d) {
    player.updateTurntableDelta(d / DPS);
  }

  public void updateTurntableGrab(bool on) {
    player.turntableGrabbed = on;
  }

  void Update() {
    if (player.active && !player.turntableGrabbed && !player.scrubGrabbed) turntable.Rotate(0, Time.deltaTime * 270 * player.playbackSpeed, 0); //.75*360 = 270
    else if (player.scrubGrabbed) {
      float s = player.getScrubAmount();
      turntable.Rotate(0, 270 * s, 0);
    }
  }
}
