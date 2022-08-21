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

public class glowDisk : MonoBehaviour {
  Vector2[] originalUVs;
  Vector2[] newUVs;
  Mesh mesh;
  public float percent = 0;

  bool initialized = false;

  void Start() {
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
