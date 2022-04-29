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
