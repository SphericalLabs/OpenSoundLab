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
