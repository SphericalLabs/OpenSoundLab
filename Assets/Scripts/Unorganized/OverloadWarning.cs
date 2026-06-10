// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2026 OSLLv1 Sphericals OpenSoundLab
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
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class OverloadWarning : MonoBehaviour
{

    [DllImport("OSLNative")]
    static extern float MasterBusRecorder_GetLevel_Lin();
    [DllImport("OSLNative")]
    static extern float MasterBusRecorder_GetLevel_dB();

    public GameObject overloadText;
    public GameObject hrtfText;
    public float hrtfDisplaySeconds = 2f;
    TextMesh hrtfMesh;
    string lastHrtfSubjectName = "";
    bool hrtfSubjectInitialized = false;
    float hrtfTextHideTime = -1f;

    void Awake()
    {
        overloadText.SetActive(false);
        if (hrtfText != null)
        {
            hrtfText.SetActive(false);
            hrtfMesh = hrtfText.GetComponent<TextMesh>();
        }
    }

    void Update()
    {
        overloadText.SetActive(MasterBusRecorder_GetLevel_dB() > -3f);
        updateHrtfText();
    }

    void updateHrtfText()
    {
        if (hrtfText == null)
        {
            return;
        }

        if (masterControl.instance == null)
        {
            hrtfText.SetActive(false);
            return;
        }

        string hrtfSubjectName = masterControl.instance.getHrtfSubjectLabel();
        if (string.IsNullOrEmpty(hrtfSubjectName))
        {
            hrtfText.SetActive(false);
            return;
        }

        if (!hrtfSubjectInitialized)
        {
            lastHrtfSubjectName = hrtfSubjectName;
            hrtfSubjectInitialized = true;
            return;
        }

        if (hrtfSubjectName != lastHrtfSubjectName)
        {
            if (hrtfMesh == null)
            {
                hrtfMesh = hrtfText.GetComponent<TextMesh>();
            }

            if (hrtfMesh != null)
            {
                hrtfMesh.text = "HRTF\n\n  " + hrtfSubjectName.ToUpperInvariant();
            }

            hrtfText.SetActive(true);
            hrtfTextHideTime = Time.unscaledTime + hrtfDisplaySeconds;
            lastHrtfSubjectName = hrtfSubjectName;
            return;
        }

        if (hrtfText.activeSelf && Time.unscaledTime >= hrtfTextHideTime)
        {
            hrtfText.SetActive(false);
        }
    }
}
