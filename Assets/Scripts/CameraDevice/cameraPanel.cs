// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This file is part of OpenSoundLab, which is based on SoundStage VR.
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