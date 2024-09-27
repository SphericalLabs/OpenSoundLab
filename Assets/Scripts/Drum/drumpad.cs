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

public class drumpad : MonoBehaviour {
  public Material onMat;
  Renderer rend;
  Material offMat;
  Material glowMat;
  deviceInterface _deviceInterface;
  public Transform stickTip;

  Color glowColor = Color.HSVToRGB(.4f, .5f, .1f);
  void Awake() {
    _deviceInterface = transform.parent.GetComponent<deviceInterface>();
    rend = GetComponent<Renderer>();
    offMat = rend.material;
    glowMat = new Material(onMat);
    glowMat.SetColor("_TintColor", glowColor);
  }

  void Update() {
    if (Input.GetKeyDown(KeyCode.Space)) {
      keyHit(true);
    }
  }

  IEnumerator offRoutine() {
    yield return new WaitForSeconds(0.1f);
    _deviceInterface.hit(false);
    rend.material = offMat;
  }

  Coroutine offCoroutine;
  public bool isHit = false;
  public void keyHit(bool on) {
    isHit = on;
    if (on) {
      _deviceInterface.hit(on);
      rend.material = glowMat;
      if (offCoroutine != null) StopCoroutine(offRoutine());
      offCoroutine = StartCoroutine(offRoutine());
    }
  }
}
