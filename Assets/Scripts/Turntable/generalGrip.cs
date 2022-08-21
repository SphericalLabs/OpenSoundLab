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

public class generalGrip : manipObject {
  public Transform masterObj;

  GameObject highlight;
  Material highlightMat;
  Color glowColor = Color.HSVToRGB(0.1f, 0.7f, 0.1f);

  public override void Awake() {
    base.Awake();
    if (masterObj == null) masterObj = transform.parent.parent;
    createHandleFeedback();
  }

  void createHandleFeedback() {
    highlight = new GameObject("highlight");
    MeshFilter m = highlight.AddComponent<MeshFilter>();

    m.mesh = GetComponent<MeshFilter>().mesh;
    MeshRenderer r = highlight.AddComponent<MeshRenderer>();
    r.material = Resources.Load("Materials/Highlight") as Material;
    highlightMat = r.material;

    highlight.transform.SetParent(transform, false);

    highlight.transform.localScale = new Vector3(1.15f, 1.05f, 1.1f);
    highlight.transform.localPosition = new Vector3(0, -.0025f, 0);
    highlightMat.SetColor("_TintColor", glowColor);
    highlightMat.SetFloat("_EmissionGain", .75f);

    highlight.SetActive(false);
  }

  public override void setState(manipState state) {
    if (curState == state) return;

    if (curState == manipState.grabbed && state != manipState.grabbed) {
      transform.parent.parent = masterObj;
    }

    curState = state;

    if (curState == manipState.none) {
      highlight.SetActive(false);
    }
    if (curState == manipState.selected) {
      highlight.SetActive(true);
      highlightMat.SetFloat("_EmissionGain", .55f);
    }
    if (curState == manipState.grabbed) {
      highlight.SetActive(true);
      transform.parent.parent = manipulatorObj.parent;
      highlightMat.SetFloat("_EmissionGain", .75f);
    }
  }
}
