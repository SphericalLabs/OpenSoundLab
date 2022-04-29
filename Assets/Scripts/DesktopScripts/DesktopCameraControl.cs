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
