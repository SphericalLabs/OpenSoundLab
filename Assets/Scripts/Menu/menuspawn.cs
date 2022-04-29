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

public class menuspawn : MonoBehaviour {
  int controllerIndex = -1;
  bool active = false;

  public GameObject glowNode;
  Material glowRender;

  public menuManager menu;

  public void SetDeviceIndex(int index) {
    controllerIndex = index;
  }

  void Start() {
    glowRender = glowNode.GetComponent<Renderer>().material;
    glowNode.SetActive(false);
    menu = menuManager.instance;
  }

  Coroutine toggleCoroutine;
  public void togglePad() {
    bool on = menu.buttonEvent(controllerIndex, transform);
    if (toggleCoroutine != null) StopCoroutine(toggleCoroutine);
    toggleCoroutine = StartCoroutine(toggleRoutine(on));
  }

  IEnumerator toggleRoutine(bool on) {
    glowNode.SetActive(true);
    Vector3 big = Vector3.one * 2.16f;
    Vector3 small = new Vector3(.01f, 2.16f, .01f);
    float timer = 0;

    if (on) {
      glowNode.transform.localScale = small;
      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 3);
        glowNode.transform.localScale = Vector3.Lerp(small, big, timer);
        glowRender.SetFloat("_EmissionGain", Mathf.Lerp(.3f, .7f, timer));
        yield return null;
      }
      glowNode.SetActive(false);
    } else {
      glowNode.transform.localScale = small;
      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 3);
        glowNode.transform.localScale = Vector3.Lerp(small, big, 1 - timer);
        glowRender.SetFloat("_EmissionGain", Mathf.Lerp(.3f, .7f, 1 - timer));
        yield return null;
      }
      glowNode.SetActive(false);
    }
  }
}
