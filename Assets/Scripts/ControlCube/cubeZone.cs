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
using System.Collections.Generic;

public class cubeZone : manipObject
{
    ControlCubeDeviceInterface _deviceInterface;
    public Transform[] lines;
    Material mat;

    Color onColor = new Color(0.414f, 0.444f, 0.750f, 0.466f);
    Color offColor = new Color(0, 0, 0, .466f);

    public override void Awake()
    {
        base.Awake();
        mat = GetComponent<Renderer>().material;
        _deviceInterface = GetComponentInParent<ControlCubeDeviceInterface>();
        updatePercent(p, false);

        if (lines[0] != null) lineWidth = lines[0].localScale.x;
    }

    Vector3 controllerPosAtBeginDragging;
    Transform tip;
    public override void setGrab(bool on, Transform t)
    {
        base.setGrab(on, t);
        tip = t.Find("manipCollViz").transform;
        controllerPosAtBeginDragging = tip.position;
        pAtBeginDragging = p;
    }

    Vector3 p, pAtBeginDragging = Vector3.zero;
    float lineWidth = 0f;

    public override void grabUpdate(Transform t)
    {
        p = pAtBeginDragging + transform.InverseTransformPoint(tip.position) - transform.InverseTransformPoint(controllerPosAtBeginDragging);
        p.x = Mathf.Clamp(p.x, -0.5f, 0.5f);
        p.y = Mathf.Clamp(p.y, -0.5f, 0.5f);
        p.z = Mathf.Clamp(p.z, -0.5f, 0.5f);
        updatePercent(p, true);

        if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse((ushort)(700f * (_deviceInterface.percent.x + _deviceInterface.percent.y + _deviceInterface.percent.z) / 3 + 50f));
    }

    void updatePercent(Vector3 tmp, bool invokeChange = false)
    {
        tmp.x = Mathf.Clamp01(-tmp.x + .5f);
        tmp.y = Mathf.Clamp01(tmp.y + .5f);
        tmp.z = Mathf.Clamp01(-tmp.z + .5f);

        _deviceInterface.updatePercent(tmp, invokeChange);
        updateLines(tmp);
    }

    float lineMargin = 0.0015f;
    public void updateLines(Vector3 p)
    {
        p = p * (.3f - lineMargin * 2) + Vector3.one * lineMargin; // margin to align with inside borders
        lines[0].localPosition = new Vector3(-p.x + .15f, p.y, 0);
        lines[1].localPosition = new Vector3(-p.x + .15f, .15f, -p.z + .15f);
        lines[2].localPosition = new Vector3(0, p.y, -p.z + .15f);
    }

    void colorChange(bool on)
    {
        if (on) mat.SetColor("_SpecColor", offColor);
        else mat.SetColor("_SpecColor", onColor);
    }

    public override void setState(manipState state)
    {
        if (curState == state) return;

        curState = state;

        if (curState == manipState.none)
        {
            colorChange(false);
        }
        if (curState == manipState.selected)
        {
            colorChange(true);
        }
        if (curState == manipState.grabbed)
        {
            colorChange(true);
        }
    }
}