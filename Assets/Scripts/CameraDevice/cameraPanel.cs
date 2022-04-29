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

public class cameraPanel : manipObject {
  GameObject highlight;
  Material highlightMat;
  Transform masterObj;
  public cameraDeviceInterface _deviceInterface;

  public override void Awake() {
    base.Awake();
    masterObj = transform.parent;
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
    highlight.transform.localScale = new Vector3(1.05f, 1.1f, 1.1f);//Vector3.one * 1.1f;

    Color c = Color.HSVToRGB(0f, 0f, .5f);
    highlightMat.SetColor("_TintColor", c);
    highlightMat.SetFloat("_EmissionGain", .35f);

    highlight.SetActive(false);
  }

  public override void setState(manipState state) {
    if (curState == state) return;

    if (curState == manipState.grabbed) {
      transform.parent = masterObj;
      _deviceInterface.updateResolution(transform.localScale.x > 2);
    }

    curState = state;

    if (curState == manipState.none) {
      highlight.SetActive(false);
    }
    if (curState == manipState.selected) {
      highlight.SetActive(true);
      highlightMat.SetFloat("_EmissionGain", .35f);
    }
    if (curState == manipState.grabbed) {
      highlight.SetActive(true);
      highlightMat.SetFloat("_EmissionGain", .45f);
    }

    if (curState == manipState.grabbed) {
      transform.parent = manipulatorObj.parent;
    }
  }

  float origDist = 1;
  Vector3 origScale;
  public override void grabUpdate(Transform t) {
    //float dist = Vector3.Magnitude(masterObj.InverseTransformPoint(transform.position)) * 8;
    //float s = Mathf.Clamp(dist, 2f, 16);
    //transform.localScale = Vector3.one * s;
  }
}