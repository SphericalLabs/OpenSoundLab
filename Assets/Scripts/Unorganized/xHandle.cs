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

public class xHandle : manipObject {

  Material mat;
  public Vector2 xBounds = new Vector2(-Mathf.Infinity, Mathf.Infinity);

  public Material onMat;
  public Renderer rend;
  Material offMat;
  Material glowMat;

  public bool invisibleMesh = false;

  Color glowColor = Color.HSVToRGB(.55f, .8f, .3f);

  public override void Awake() {
    base.Awake();
    if (rend == null) rend = GetComponent<Renderer>();
    offMat = rend.material;
    glowMat = new Material(onMat);
    glowMat.SetFloat("_EmissionGain", .5f);
    glowMat.SetColor("_TintColor", glowColor);

    if (invisibleMesh) rend.enabled = false;
  }

  public void pulse() {
    if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(750);
  }

  public override void grabUpdate(Transform t) {
    Vector3 p = transform.localPosition;
    p.x = Mathf.Clamp(transform.parent.InverseTransformPoint(manipulatorObj.position).x + offset, xBounds.x, xBounds.y);
    transform.localPosition = p;
  }

  public void updatePos(float pos) {
    Vector3 p = transform.localPosition;
    p.x = Mathf.Clamp(pos, xBounds.x, xBounds.y);

    transform.localPosition = p;
  }

  public void recalcOffset() {
    offset = transform.localPosition.x - transform.parent.InverseTransformPoint(manipulatorObj.position).x;
  }

  float offset = 0;

  public override void setState(manipState state) {
    curState = state;
    if (curState == manipState.none) {
      if (!invisibleMesh) rend.material = offMat;
      else rend.enabled = false;
    } else if (curState == manipState.selected) {
      rend.material = glowMat;
      glowMat.SetFloat("_EmissionGain", .4f);
      if (invisibleMesh) rend.enabled = true;
    } else if (curState == manipState.grabbed) {
      rend.material = glowMat;
      glowMat.SetFloat("_EmissionGain", .6f);
      offset = transform.localPosition.x - transform.parent.InverseTransformPoint(manipulatorObj.position).x;
      if (invisibleMesh) rend.enabled = true;
    }
  }
}
