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
