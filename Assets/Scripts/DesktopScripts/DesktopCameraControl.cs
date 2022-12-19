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

public class DesktopCameraControl : MonoBehaviour {

  public bool desktopCameraEnabled = false;
  public bool playerFOV = true;
  public bool showEnvironment = true;
  public bool cameraLock = false;

  public GameObject cameraSecondary;
  public GameObject groundPlane;

  void Awake() {
    ToggleCameraEnabled(false);
  }

  public void ChangeFOV(string s) {
    if (s == "") return;
  }

  void Update() {
    if (!desktopCameraEnabled) return;
    if (cameraLock) return;
    if (playerFOV) {
      transform.position = Camera.main.transform.position;
      transform.rotation = Camera.main.transform.rotation;
    }
  }

  public void ToggleCameraLock(bool on) {
    cameraLock = on;
  }

  public void ToggleCameraEnabled(bool on) {
    desktopCameraEnabled = on;
    cameraSecondary.SetActive(desktopCameraEnabled);
  }

  public void ToggleEnvironment(bool on) {
    showEnvironment = !on;
    Camera[] cams = FindObjectsOfType<Camera>();
    for (int i = 0; i < cams.Length; i++) {
      if (cams[i].GetComponent<Skybox>() != null) cams[i].GetComponent<Skybox>().enabled = showEnvironment;
    }
    masterControl.instance.showEnvironment = showEnvironment;
    groundPlane.SetActive(showEnvironment);
  }
}
