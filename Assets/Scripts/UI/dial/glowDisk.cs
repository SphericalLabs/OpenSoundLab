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

public class glowDisk : MonoBehaviour {
  Vector2[] originalUVs;
  Vector2[] newUVs;
  Mesh mesh;
  public float percent = 0;

  bool initialized = false;

  void Awake() {
    mesh = GetComponent<MeshFilter>().mesh;
    originalUVs = mesh.uv;
    newUVs = new Vector2[originalUVs.Length];

    for (int i = 0; i < originalUVs.Length; i++) {
      newUVs[i] = originalUVs[i];
      if (originalUVs[i].x > .49f) originalUVs[i].x -= 0.01f;
    }

    mesh.uv = originalUVs;
    initialized = true;
  }

  float lastPercent = -1;
  public void PercentUpdate() {
    if (lastPercent == percent || !initialized) return;
    lastPercent = percent;

    percent = Mathf.Clamp01(percent);

    for (int i = 0; i < originalUVs.Length; i++) {
      if (originalUVs[i].y == 1) newUVs[i].x = originalUVs[i].x + percent / 2f;
    }
    mesh.uv = newUVs;
  }
}
