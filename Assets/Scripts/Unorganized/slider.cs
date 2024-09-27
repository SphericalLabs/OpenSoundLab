// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright � 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
// 
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at 
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
// 
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
// 
//
// Copyright � 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright � 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright � 2017 Apache 2.0 Google LLC SoundStage VR
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

public class slider : manipObject {
  public float percent = 0f;
  public float xBound = 0.04f;

  public float sliderHue = .44f;

  public Color customColor;
  public Material onMat;
  public Renderer rend;
  Material offMat;
  Material glowMat;
  public bool labelsPresent = true;

  Material[] labels;

  public Color labelColor = new Color(0f, 0.8f, 1f);

  public override void Awake() {
    base.Awake();
    if (rend == null) rend = GetComponent<Renderer>();
    offMat = rend.material;
    customColor = labelColor;

    glowMat = new Material(onMat);
    glowMat.SetFloat("_EmissionGain", .0f);
    glowMat.SetColor("_TintColor", labelColor);

    //if (labelsPresent) {
    //  labels = new Material[3];
    //  labels[0] = transform.parent.Find("Label1").GetComponent<Renderer>().material;
    //  labels[1] = transform.parent.Find("Label2").GetComponent<Renderer>().material;
    //  labels[2] = transform.parent.Find("Label3").GetComponent<Renderer>().material;
      
    //  for (int i = 0; i < 3; i++) {
    //    labels[i].SetColor("_TintColor", labelColor);
    //  }
    //}
    setPercent(percent);
  }

  int lastNotch = -1;
  public override void grabUpdate(Transform t) {
    Vector3 p = transform.localPosition;
    p.x = Mathf.Clamp(transform.parent.InverseTransformPoint(manipulatorObj.position).x + offset, -xBound, xBound);
    transform.localPosition = p;
    updatePercent();

    if (Mathf.FloorToInt(percent / .05f) != lastNotch) {
      if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(500);
      lastNotch = Mathf.FloorToInt(percent / .05f);
    }
  }

  public void setPercent(float p) {
    Vector3 pos = transform.localPosition;
    pos.x = Mathf.Lerp(-xBound, xBound, p);
    transform.localPosition = pos;
    updatePercent();
  }

  void updatePercent() {
    percent = .5f + transform.localPosition.x / (2 * xBound);

    //if (labelsPresent) {
    //  if (percent <= 0.5f) {
    //    labels[0].SetColor("_TintColor", labelColor * (0.15f + 0.5f - percent) * 2);
    //    labels[1].SetColor("_TintColor", labelColor * (0.15f + percent) * 2);
    //    labels[2].SetColor("_TintColor", labelColor * 0.15f);
    //  } else {
    //    labels[0].SetColor("_TintColor", labelColor * 0.15f);
    //    labels[1].SetColor("_TintColor", labelColor * (1.15f - percent) * 2);
    //    labels[2].SetColor("_TintColor", labelColor * (percent - 0.35f) * 2);
    //  }
    //}
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