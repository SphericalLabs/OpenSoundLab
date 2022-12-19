// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
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
