// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This file is part of OpenSoundLab, which is based on SoundStage VR.
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

// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This file is part of OpenSoundLab, which is based on SoundStage VR.
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
