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

﻿using UnityEngine;
using System.Collections;

public class adsrInterface : MonoBehaviour {

  public Vector2[] defaultPercents;
  public xyHandle[] xyHandles;

  public float[] durations = new float[] { 1f, 1.4f, 1.2f };
  public float[] volumes = new float[] { 1, 0.8f };

  Color lineColor = new Color(0.25f, .25f, .5f);

  Vector3[] prevPositions;

  void Awake() {
    prevPositions = new Vector3[3];
  }

  public bool setDefaults = true;
  void Start() {
    if (!setDefaults) return;

    for (int i = 0; i < 3; i++) {
      xyHandles[i].percent = defaultPercents[i];
      xyHandles[i].setPercent(defaultPercents[i]);
    }
  }

  void Update() {
    bool posChange = false;
    for (int i = 0; i < 3; i++) {
      if (xyHandles[i].transform.localPosition != prevPositions[i]) posChange = true;
    }

    if (posChange) {
      for (int i = 0; i < 3; i++) {
        prevPositions[i] = xyHandles[i].transform.localPosition;
      }

      posClamp();
    }

    durations[0] = xyHandles[0].percent.x * 7f;
    volumes[0] = xyHandles[0].percent.y;

    durations[1] = xyHandles[1].percent.x - xyHandles[0].percent.x;
    volumes[1] = xyHandles[1].percent.y;

    durations[2] = (1 - xyHandles[2].percent.x) * 7f;
  }

  void posClamp() {
    if (xyHandles[0].percent.x > xyHandles[1].percent.x) {
      if (xyHandles[0].curState == manipObject.manipState.grabbed) xyHandles[1].forceChange(xyHandles[0].percent.x, true);
      else xyHandles[0].forceChange(xyHandles[1].percent.x, true);
    }
    if (xyHandles[1].percent.x > xyHandles[2].percent.x) {
      if (xyHandles[1].curState == manipObject.manipState.grabbed) xyHandles[2].forceChange(xyHandles[1].percent.x, true);
      else xyHandles[1].forceChange(xyHandles[2].percent.x, true);
    }

    if (xyHandles[1].percent.y != xyHandles[2].percent.y) {
      if (xyHandles[2].curState == manipObject.manipState.grabbed) xyHandles[1].forceChange(xyHandles[2].percent.y, false);
      else xyHandles[2].forceChange(xyHandles[1].percent.y, false);
    }
  }

}
