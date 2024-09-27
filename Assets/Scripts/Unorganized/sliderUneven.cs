// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
// 
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at 
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
// 
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
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

public class sliderUneven : manipObject {
  public float percent = 0f;
  public Vector2 bounds = new Vector2(-0.04f, 0.04f);
  public Vector2 percentageBounds = new Vector2(-0.04f, 0.04f);
  Color customColor;
  public Material onMat;
  public Renderer rend;
  Material offMat;
  Material glowMat;
  Color labelColor = new Color(0, 0.65f, 0.15f);

  public float glowhue = -1;

  public override void Awake() {
    base.Awake();
    if (rend == null) rend = GetComponent<Renderer>();
    offMat = rend.material;
    if (glowhue == -1) glowhue = Random.value;
    customColor = Color.HSVToRGB(glowhue, 0.8f, .2f);

    glowMat = new Material(onMat);
    glowMat.SetFloat("_EmissionGain", .7f);
    glowMat.SetColor("_TintColor", customColor);

    setPercent(percent);
  }

  int lastNotch = -1;
  public override void grabUpdate(Transform t) {
    Vector3 p = transform.localPosition;
    p.x = Mathf.Clamp(transform.parent.InverseTransformPoint(manipulatorObj.position).x + offset, bounds.x, bounds.y);
    transform.localPosition = p;
    updatePercent();

    if (Mathf.FloorToInt(percent / .05f) != lastNotch) {
      if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(500);
      lastNotch = Mathf.FloorToInt(percent / .05f);
    }
  }

  public void setPercent(float p) {
    Vector3 pos = transform.localPosition;
    pos.x = Mathf.Lerp(percentageBounds.x, percentageBounds.y, p);
    transform.localPosition = pos;
    updatePercent();
  }

  void updatePercent() {
    percent = Mathf.InverseLerp(percentageBounds.x, percentageBounds.y, transform.localPosition.x);
  }

  float offset = 0;

  public override void setState(manipState state) {
    curState = state;
    if (curState == manipState.none) {
      rend.material = offMat;
    } else if (curState == manipState.selected) {
      rend.material = glowMat;
      glowMat.SetFloat("_EmissionGain", .5f);
    } else if (curState == manipState.grabbed) {
      rend.material = glowMat;
      glowMat.SetFloat("_EmissionGain", .7f);
      offset = transform.localPosition.x - transform.parent.InverseTransformPoint(manipulatorObj.position).x;
    }
  }
}