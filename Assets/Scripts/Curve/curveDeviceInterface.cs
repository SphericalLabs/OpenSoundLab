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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class curveDeviceInterface : deviceInterface {

  public Transform backgroundQuad;
  LineRenderer lr;
  Vector2 quadDimensions = new Vector2(.5f, .25f);

  public override void Awake() {
    base.Awake();
    lr = GetComponentInChildren<LineRenderer>();
    updateDimensions();
    setupLine();
  }

  void setupLine() {
    int nodes = 65;
    lr.positionCount = nodes;
    Vector3[] points = new Vector3[nodes];
    for (int i = 0; i < nodes; i++) {
      float per = (float)i / (nodes - 1);
      float x = Mathf.Lerp(-.5f, .5f, per);
      float y = .5f * Mathf.Sin(Mathf.PI * 2 * per);
      points[i] = new Vector3(x, y, 0);
    }
    lr.SetPositions(points);
  }

  void updateDimensions() {
    backgroundQuad.localScale = new Vector3(quadDimensions.x, quadDimensions.y, 1);
    backgroundQuad.localPosition = new Vector3(-quadDimensions.x / 2f, 0, 0);
  }
}
