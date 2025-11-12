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
using UnityEngine.UI;

public class ManipUiObject : manipObject
{
    [Header("UI")]
    public float buttonThickness = 20f;

    protected Image image;
    protected Color normalColor;
    public Color selectedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    public Color grabbedColor = new Color(0.8f, 0.8f, 0.8f, 1f);


    protected virtual void Start()
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }

        normalColor = image.color;

        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        var collider = gameObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y, buttonThickness);

        Vector3 offset = new Vector3(rectTransform.sizeDelta.x * (rectTransform.pivot.x - 0.5f), rectTransform.sizeDelta.y * (rectTransform.pivot.y - 0.5f), 0f);
        offset *= -1f;
        collider.center = offset;
    }

    public override void setState(manipState state)
    {
        curState = state;
        if (curState == manipState.none)
        {
            image.color = normalColor;
        }
        else if (curState == manipState.selected)
        {
            image.color = selectedColor;
        }
        else if (curState == manipState.grabbed)
        {
            image.color = grabbedColor;
            OnGrab();
        }
    }

    public virtual void OnGrab()
    {

    }
}
