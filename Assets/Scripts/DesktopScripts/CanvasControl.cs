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

public class CanvasControl : MonoBehaviour {

  public RectTransform menuPanel;
  public Canvas _canvas;
  bool open = false;
  public GameObject credits;
  bool creditsOn = false;

  void ToggleMenu() {
    open = !open;

    Cursor.visible = open;
    if (menuRoutine != null) StopCoroutine(menuRoutine);
    menuRoutine = StartCoroutine(menuOpenRoutine(open));
  }

  public void ToggleCredits() {
    creditsOn = !creditsOn;
    credits.SetActive(creditsOn);
  }

  public void CreditsOff() {
    creditsOn = false;
    credits.SetActive(creditsOn);
  }

  float mouseTimer;
  float mouseThreshold = 2;
  Vector3 lastMouse;
  void Update() {
    if (lastMouse != Input.mousePosition) {
      lastMouse = Input.mousePosition;
      if (!open) ToggleMenu();
      mouseTimer = 0;
    } else if (open) {
      mouseTimer += Time.deltaTime;
      if (mouseTimer >= mouseThreshold) ToggleMenu();
    }
  }

  Coroutine menuRoutine;
  IEnumerator menuOpenRoutine(bool on) {
    if (on) _canvas.enabled = true;
    float t = 0;
    Vector2 curPos = menuPanel.anchoredPosition;
    Vector2 endPos = Vector2.right * (on ? 0 : -350);

    while (t < 1) {
      t = Mathf.Clamp01(t + Time.deltaTime * 2);
      menuPanel.anchoredPosition = Vector2.Lerp(curPos, endPos, BezierBlend(t));
      yield return null;
    }
    if (!on) _canvas.enabled = false;
  }

  float BezierBlend(float t) {
    return Mathf.Pow(t, 2) * (3.0f - 2.0f * t);
  }
}
