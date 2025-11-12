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

public class DesktopCameraControl : MonoBehaviour
{

    public bool desktopCameraEnabled = false;
    public bool playerFOV = true;
    public bool showEnvironment = true;
    public bool cameraLock = false;

    public GameObject cameraSecondary;
    public GameObject groundPlane;

    void Awake()
    {
        ToggleCameraEnabled(false);
    }

    public void ChangeFOV(string s)
    {
        if (s == "") return;
    }

    void Update()
    {
        if (!desktopCameraEnabled) return;
        if (cameraLock) return;
        if (playerFOV)
        {
            transform.position = Camera.main.transform.position;
            transform.rotation = Camera.main.transform.rotation;
        }
    }

    public void ToggleCameraLock(bool on)
    {
        cameraLock = on;
    }

    public void ToggleCameraEnabled(bool on)
    {
        desktopCameraEnabled = on;
        cameraSecondary.SetActive(desktopCameraEnabled);
    }

    public void ToggleEnvironment(bool on)
    {
        showEnvironment = !on;
        Camera[] cams = FindObjectsOfType<Camera>();
        for (int i = 0; i < cams.Length; i++)
        {
            if (cams[i].GetComponent<Skybox>() != null) cams[i].GetComponent<Skybox>().enabled = showEnvironment;
        }
        masterControl.instance.showEnvironment = showEnvironment;
        groundPlane.SetActive(showEnvironment);
    }
}
