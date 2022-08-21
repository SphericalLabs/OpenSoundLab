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
