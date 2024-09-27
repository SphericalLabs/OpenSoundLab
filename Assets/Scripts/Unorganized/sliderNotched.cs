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
using System.Collections.Generic;
using System.Linq;

public class sliderNotched : manipObject {
  public float percent = 0f;
  public float xBound = 0.04f;

  public int switchVal = 0;
  public int notchCount = 4;
  public Transform labelHolder;

  Color customColor;
  public Material glowMat;
  Renderer rend;
  Material offMat;
  public bool labelsPresent = true;

  public GameObject[] labelObjects;

  public GameObject titleLabel;

  Material[] labels;
  Color labelColor = new Color(0, 0.65f, 0.15f);

  public override void Awake() {
    base.Awake();
    rend = GetComponent<Renderer>();
    offMat = rend.sharedMaterial;
    customColor = Color.HSVToRGB(Random.value, 0.8f, .2f);

    //glowMat = new Material(onMat);
    //glowMat.SetFloat("_EmissionGain", .7f);
    //glowMat.SetColor("_TintColor", customColor);

    if (labelsPresent) {
      labels = new Material[labelObjects.Length];
      for (int i = 0; i < labelObjects.Length; i++) {
        labels[i] = labelObjects[labelObjects.Length - 1 - i].GetComponent<Renderer>().material;
        labels[i].SetColor("_TintColor", labelColor);
      }
    }

    if (titleLabel != null) {
      titleLabel.GetComponent<Renderer>().material.SetColor("_TintColor", labelColor);
    }
    setVal(switchVal);
    
    
}

float targetX = 0;
  int lastSwitchVal = 0;
  float goalX = 0f;
  public override void grabUpdate(Transform t) {
    targetX = Mathf.Clamp(transform.parent.InverseTransformPoint(manipulatorObj.position).x + offset, -xBound, xBound);
    percent = .5f + targetX / (2 * xBound);
    updateSwitchVal();
  }

  void updateSwitchVal() {
    
    switchVal = Mathf.RoundToInt(percent * (notchCount - 1));
    if(switchVal != lastSwitchVal) manipulatorObjScript.hapticPulse();

    float modPercent = switchVal / (float)(notchCount - 1);
    modPercent = Mathf.Lerp(modPercent, percent, .25f);

    float modX = (modPercent - .5f) * (2 * xBound);
    goalX = Mathf.Lerp(goalX, modX, 0.5f);

    Vector3 p = transform.localPosition;
    p.x = goalX;
    transform.localPosition = p;
    updateLabels();
    lastSwitchVal = switchVal;
  }

  public void setValByPercent(float p){
    setVal(Mathf.RoundToInt(p * (notchCount - 1)));
  }

  public void setVal(int v) {
    switchVal = v;
    percent = (float)v / (notchCount - 1);
    Vector3 pos = transform.localPosition;
    pos.x = Mathf.Lerp(-xBound, xBound, percent);
    transform.localPosition = pos;
    updateLabels();
  }

  void updateLabels() {
    if (labelsPresent) {
      for (int i = 0; i < labels.Length; i++) {
        labels[i].SetColor("_TintColor", Color.HSVToRGB(.4f, .7f, (i == switchVal) ? .9f : .1f));
        labels[i].SetFloat("_EmissionGain", (i == switchVal) ? .3f : .05f);
      }
    }
  }

  float offset = 0;

  public override void setState(manipState state) {
    curState = state;
    if (curState == manipState.none) {
      rend.sharedMaterial = offMat;
    } else if (curState == manipState.selected) {
      rend.sharedMaterial = glowMat;
      //glowMat.SetFloat("_EmissionGain", .5f);
    } else if (curState == manipState.grabbed) {
      rend.sharedMaterial = glowMat;
      //glowMat.SetFloat("_EmissionGain", .7f);
      offset = transform.localPosition.x - transform.parent.InverseTransformPoint(manipulatorObj.position).x;
    }
  }
}
