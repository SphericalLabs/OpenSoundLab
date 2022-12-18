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

public class UIpanelLoad : manipObject {

  public TextMesh label;
  public GameObject outline;
  Renderer panelRend;
  public Material panelMat, panelMatSelected;
  Material textMat;
  Color onColor, offColor;

  public bool saveButton = false;

  public componentInterface _compInterface;
  public int buttonID;

  public override void Awake() {
    base.Awake();

    panelRend = transform.Find("Outline").gameObject.GetComponent<Renderer>();
    panelRend.sharedMaterial = panelMat;

    if (transform.parent) _compInterface = transform.parent.GetComponent<componentInterface>();

    offColor = Color.HSVToRGB(.5f, 230f / 255, 118f / 255);
    onColor = Color.HSVToRGB(.5f, 0f, 118f / 255);
    textMat = label.GetComponent<Renderer>().material;
    textMat.SetColor("_TintColor", offColor);

    outline.GetComponent<Renderer>().material.SetColor("_TintColor", offColor);
    outline.GetComponent<Renderer>().material.SetFloat("_EmissionGain", .428f);
    outline.GetComponent<Renderer>().material.SetFloat("_InvFade", 1f);
  }

  public void setText(string s) {
    label.text = s;
  }

  public override void setState(manipState state) {
    if (curState == manipState.grabbed && curState != state) {
      keyHit(false);
    }

    curState = state;

    if (curState == manipState.none) {
      if (!toggled) textMat.SetColor("_TintColor", offColor);
      panelRend.sharedMaterial = panelMat;
    } else if (curState == manipState.selected) {
      textMat.SetColor("_TintColor", onColor);
      panelRend.sharedMaterial = panelMatSelected;
    } else if (curState == manipState.grabbed) {
      textMat.SetColor("_TintColor", onColor);
      keyHit(true);
    }
  }

  public bool isHit = false;
  bool toggled = false;
  public void keyHit(bool on) {
    isHit = on;
    toggled = on;
    if (on) {
      if (_compInterface != null) _compInterface.hit(on, buttonID);
      outline.GetComponent<Renderer>().material.SetColor("_TintColor", onColor);
      textMat.SetColor("_TintColor", onColor);
    } else {
      if (_compInterface != null) _compInterface.hit(on, buttonID);
      outline.GetComponent<Renderer>().material.SetColor("_TintColor", offColor);
      textMat.SetColor("_TintColor", offColor);
    }
  }

  public override void onTouch(bool on, manipulator m) {
    if (m != null) {
      if (m.emptyGrab) {
        keyHit(on);
      }
    }
  }
}
