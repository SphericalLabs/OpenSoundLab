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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class midiOutOfRange : MonoBehaviour
{
    public Transform arrowA, arrowB;

    public void Activate()
    {
        if (mainRoutine != null) StopCoroutine(mainRoutine);
        mainRoutine = StartCoroutine(MainRoutine());
    }

    Coroutine mainRoutine;

    Vector3 vecA = new Vector3(-.03f, -.09f, 0);
    Vector3 vecB = new Vector3(-.075f, -.09f, 0);
    Vector3 vecC = new Vector3(-.12f, -.09f, 0);
    IEnumerator MainRoutine()
    {
        float t = 0;
        while (t < 1)
        {
            t = Mathf.Clamp01(t + Time.deltaTime * 3);
            arrowB.localPosition = arrowA.localPosition = Vector3.Lerp(vecA, vecB, t);
            yield return null;
        }
        t = 0;
        while (t < 1)
        {
            t = Mathf.Clamp01(t + Time.deltaTime * 3);
            arrowB.localPosition = Vector3.Lerp(vecB, vecC, t);
            yield return null;
        }

        gameObject.SetActive(false);
    }
}
