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

public class slider2D : manipObject {
  public Vector2 percent = Vector2.zero;
  float xBound = 0.125f;
  float yBound = 0.05f;
  Material mat;
  Color customColor;

  public override void Awake() {
    base.Awake();
    mat = GetComponent<Renderer>().material;
    customColor = new Color(Random.value, Random.value, Random.value);
    mat.SetColor("_EmissionColor", customColor * 0.25f);
  }

  void Start() {
    setPercent(percent);
  }

  public void forceChange(float value, bool Xaxis) // x axis false if it forces Y
  {

    setPercent(new Vector2(value, value), Xaxis, !Xaxis);

    if (curState == manipState.grabbed) {
      offset.x = transform.localPosition.x - transform.parent.InverseTransformPoint(manipulatorObj.position).x;
      offset.y = transform.localPosition.y - transform.parent.InverseTransformPoint(manipulatorObj.position).y;
    }

    updatePercent();
  }

  public override void grabUpdate(Transform t) {
    Vector3 p = transform.localPosition;
    p.x = Mathf.Clamp(transform.parent.InverseTransformPoint(manipulatorObj.position).x + offset.x, -xBound, xBound);
    p.y = Mathf.Clamp(transform.parent.InverseTransformPoint(manipulatorObj.position).y + offset.y, -yBound, yBound);
    transform.localPosition = p;
    updatePercent();
  }

  public void setPercent(Vector2 p, bool doX = true, bool doY = true) {
    Vector3 pos = transform.localPosition;
    if (doX) pos.x = Mathf.Lerp(-xBound, xBound, p.x);
    if (doY) pos.y = Mathf.Lerp(-yBound, yBound, p.y);
    transform.localPosition = pos;
    updatePercent();
  }

  void updatePercent() {
    percent.x = .5f + transform.localPosition.x / (2 * xBound);
    percent.y = .5f + transform.localPosition.y / (2 * yBound);
  }

  Vector2 offset = Vector2.zero;

  public override void setState(manipState state) {
    curState = state;
    if (curState == manipState.none) {
      mat.SetColor("_EmissionColor", customColor * 0.25f);
    } else if (curState == manipState.selected) {
      mat.SetColor("_EmissionColor", customColor * 0.5f);
    } else if (curState == manipState.grabbed) {
      mat.SetColor("_EmissionColor", customColor);
      offset.x = transform.localPosition.x - transform.parent.InverseTransformPoint(manipulatorObj.position).x;
      offset.y = transform.localPosition.y - transform.parent.InverseTransformPoint(manipulatorObj.position).y;
    }
  }
}
