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
