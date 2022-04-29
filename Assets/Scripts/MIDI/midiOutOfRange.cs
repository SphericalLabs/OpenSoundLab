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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class midiOutOfRange : MonoBehaviour {
  public Transform arrowA, arrowB;

  public void Activate() {
    if (mainRoutine != null) StopCoroutine(mainRoutine);
    mainRoutine = StartCoroutine(MainRoutine());
  }

  Coroutine mainRoutine;

  Vector3 vecA = new Vector3(-.03f, -.09f, 0);
  Vector3 vecB = new Vector3(-.075f, -.09f, 0);
  Vector3 vecC = new Vector3(-.12f, -.09f, 0);
  IEnumerator MainRoutine() {
    float t = 0;
    while (t < 1) {
      t = Mathf.Clamp01(t + Time.deltaTime * 3);
      arrowB.localPosition = arrowA.localPosition = Vector3.Lerp(vecA, vecB, t);
      yield return null;
    }
    t = 0;
    while (t < 1) {
      t = Mathf.Clamp01(t + Time.deltaTime * 3);
      arrowB.localPosition = Vector3.Lerp(vecB, vecC, t);
      yield return null;
    }

    gameObject.SetActive(false);
  }
}
