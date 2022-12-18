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

public class exampleItem : manipObject {

  string filename = "example";
  public Transform Display;
  public Renderer label;
  public bool toggleState = false;

  menuItem.deviceType DeviceRep;
  public Material menuMat;

  float rotateSpeed = 5f;

  exampleManager manager;

  public override void Awake() {
    base.Awake();
  }

  void Start() {
    label.material.SetFloat("_EmissionGain", .15f);
  }

  public void Setup(exampleManager mgr, menuItem.deviceType rep, string filestring, string labelcopy) {
    manager = mgr;
    filename = filestring;
    DeviceRep = rep;
    label.GetComponent<TextMesh>().text = labelcopy;
    MeshSetup();
  }

  void Update() {
    Display.Rotate(0, 0, Time.deltaTime * rotateSpeed);
  }

  public void toggleSelect(bool on) {
    toggleState = on;
    label.material.SetFloat("_EmissionGain", toggleState ? .3f : .15f);
    if (on) {
      exampleItem[] items = FindObjectsOfType<exampleItem>();
      for (int i = 0; i < items.Length; i++) {
        if (items[i] != this) items[i].toggleSelect(false);
      }
    }
  }

  Coroutine spinRoutine;
  IEnumerator spinAnim() {
    float t = 0;
    while (t < 1) {
      t = Mathf.Clamp01(t + Time.deltaTime * 3);
      rotateSpeed = Mathf.Lerp(720, 5, t);
      yield return null;
    }
  }

  public void confirmSelection() {
    if (manager != null) {
      manager.LoadExample(filename);
    }
  }

  public override void setState(manipState state) {
    manipState prevState = curState;
    curState = state;

    if (curState == manipState.none) {
      Display.localScale = Vector3.one;
    } else if (curState == manipState.selected) {
      if (prevState != manipState.grabbed) {
        if (spinRoutine != null) StopCoroutine(spinRoutine);
        spinRoutine = StartCoroutine(spinAnim());
      }
      Display.localScale = Vector3.one * 1.1f;
    } else if (curState == manipState.grabbed) {
      confirmSelection();
    }
  }

  void MeshSetup() {
    GameObject g = Instantiate(menuManager.instance.refObjects[DeviceRep], Display.position, Display.rotation) as GameObject;
    g.transform.parent = Display;
    g.transform.Rotate(90, 0, 0, Space.Self);
    g.transform.localScale = g.transform.localScale * Display.localScale.x * 1.5f;
  }
}
