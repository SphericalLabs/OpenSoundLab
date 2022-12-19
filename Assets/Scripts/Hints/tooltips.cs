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

public class tooltips : manipObject {
  public GameObject vidPlayerPrefab, vidContainer;
  public string vidFile;

  Material mat;
  Color normalColor = new Color(.5f, .5f, 1);
  Color normalOnColor = new Color(.5f, .5f, 1);
  Color selectColor = new Color(.85f, .85f, 1);
  Color grabColor = new Color(.85f, .85f, 1);

  public bool tipOn = false;
  bool tipLoaded = false;

  public override void Awake() {
    base.Awake();
    normalColor = Color.HSVToRGB(.6f, 1f, .06f);
    normalOnColor = Color.HSVToRGB(.6f, .95f, .2f);
    selectColor = Color.HSVToRGB(.6f, .95f, .5f);
    grabColor = Color.HSVToRGB(.6f, .9f, 1f);

    mat = GetComponent<Renderer>().material;
    mat.SetColor("_TintColor", normalColor);

    if (masterControl.instance != null) {
      ShowTooltips(masterControl.instance.tooltipsOn);
    } else {
      ShowTooltips(FindObjectOfType<masterControl>().tooltipsOn);
    }
    otherTipCheck();
  }

  void otherTipCheck() {
    tooltips[] _othertips = FindObjectsOfType<tooltips>();
    for (int i = 0; i < _othertips.Length; i++) {
      if (_othertips[i].vidFile == vidFile && _othertips[i] != this) {
        if (!_othertips[i].tipOn) _othertips[i].ShowTooltips(false);
        else {
          ShowTooltips(false);
          break;
        }
      }
    }
  }

  public void ShowTooltips(bool on) {
    if (on) otherTipCheck();

    GetComponent<Renderer>().enabled = on;
    GetComponent<Collider>().enabled = on;

    if (!on && tipOn) ToggleVideo(!tipOn);
  }

  public void ToggleVideo(bool on) {
    tipOn = on;

    vidContainer.SetActive(on);
    if (tipOn && !tipLoaded) {
      tipLoaded = true;
      GameObject g = Instantiate(vidPlayerPrefab, vidContainer.transform, false);
      g.transform.localPosition = Vector3.zero;
      g.transform.localRotation = Quaternion.identity;
      g.transform.localScale = Vector3.one;
    }

    if (tipOn) vidContainer.GetComponentInChildren<VideoPlayerDeviceInterface>().Autoplay(vidFile, this);
  }

  public override void setState(manipState state) {
    if (curState == state) return;

    curState = state;

    if (curState == manipState.none) {
      mat.SetColor("_TintColor", normalColor);
    } else if (curState == manipState.selected) {
      mat.SetColor("_TintColor", selectColor);
    } else if (curState == manipState.grabbed) {
      mat.SetColor("_TintColor", grabColor);
      ToggleVideo(!tipOn);
    }
  }
}
