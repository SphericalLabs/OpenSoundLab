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

using UnityEngine;
using System.Collections;

public class uiPanelSinglePress : manipObject {
  public TextMesh label;
  public Renderer outline;

  Material mat;
  Material textMat;
  public componentInterface _componentInterface;
  public int buttonID = -1;

  Color normalColor;

  public override void Awake() {
    base.Awake();
    mat = outline.material;

    normalColor = Color.HSVToRGB(.6f, .7f, .9f);
    textMat = label.GetComponent<Renderer>().material;
    textMat.SetColor("_TintColor", normalColor);
    mat.SetColor("_TintColor", normalColor);
    mat.SetFloat("_EmissionGain", .3f);
  }

  public void newColor(Color c) {
    normalColor = c;
    textMat.SetColor("_TintColor", normalColor);
    mat.SetColor("_TintColor", normalColor);
  }

  void Start() {
    if (transform.parent) _componentInterface = transform.parent.GetComponent<componentInterface>();
  }

  public override void setState(manipState state) {
    if (curState == manipState.grabbed && state != manipState.grabbed) {
      if (_componentInterface != null) _componentInterface.hit(false, buttonID);
    }
    curState = state;
    if (curState == manipState.none) {
      mat.SetColor("_TintColor", normalColor);
      mat.SetFloat("_EmissionGain", .3f);


      if (textMat != null) {
        textMat.SetColor("_TintColor", normalColor);
        textMat.SetFloat("_EmissionGain", .3f);
      }
    } else if (curState == manipState.selected) {
      mat.SetColor("_TintColor", normalColor);
      mat.SetFloat("_EmissionGain", .6f);

      if (textMat != null) {
        textMat.SetColor("_TintColor", normalColor);
        textMat.SetFloat("_EmissionGain", .3f);
      }
    } else if (curState == manipState.grabbed) {
      mat.SetColor("_TintColor", Color.white);

      if (textMat != null) {
        textMat.SetColor("_TintColor", normalColor);
        textMat.SetFloat("_EmissionGain", .6f);
      }
      if (_componentInterface != null) _componentInterface.hit(true, buttonID);
    }
  }
}
