// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright � 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
// 
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at 
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
// 
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
// 
//
// Copyright � 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright � 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright � 2017 Apache 2.0 Google LLC SoundStage VR
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

    //Color onColor = new Color(0.1346236f + 0.1f, 0.1921309f + 0.1f, 0.2205881f + 0.1f, 0.3333333f + 0.1f);
    //Color offColor = new Color(0.1346236f, 0.1921309f, 0.2205881f, 0.3333333f);
    Color untouchedColor;
    Color touchedColor;

    public override void Awake()
    {
        base.Awake();
        mat = GetComponent<Renderer>().material;
        touchedColor = mat.GetColor("_BaseColor");
        untouchedColor = incrementColor(touchedColor, 0.12f);
        mat.SetColor("_BaseColor", untouchedColor);
        _deviceInterface = GetComponentInParent<ControlCubeDeviceInterface>();
        updatePercent(p, false);

        if (lines[0] != null) lineWidth = lines[0].localScale.x;
    }

    private Color incrementColor(Color color, float increment)
    {
        float r = Mathf.Min(color.r + increment, 1.0f);
        float g = Mathf.Min(color.g + increment, 1.0f);
        float b = Mathf.Min(color.b + increment, 1.0f);
        float a = Mathf.Min(color.a + increment, 1.0f);
        return new Color(r, g, b, a);
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
        if (on) mat.SetColor("_BaseColor", touchedColor);
        else mat.SetColor("_BaseColor", untouchedColor);
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

    public override void onTouch(bool on, manipulator m)
    {
        if (m == null) return;

        if (on)
        {
            if (!m.triggerDown || !m.emptyGrab) return;

            if (m.SelectedObject != null && m.SelectedObject != this) return;

            m.selectedTransform = transform;
            m.SelectedObject = this;
            setSelect(true, m.transform);
            m.emptyGrab = false;

            setGrab(true, m.transform);

            if (tip == null) return;

            Vector3 localTip = transform.InverseTransformPoint(tip.position);
            p = new Vector3(
                Mathf.Clamp(localTip.x, -0.5f, 0.5f),
                Mathf.Clamp(localTip.y, -0.5f, 0.5f),
                Mathf.Clamp(localTip.z, -0.5f, 0.5f));
            pAtBeginDragging = p;
            controllerPosAtBeginDragging = tip.position;
            updatePercent(p, true);
        }
        else if (m.SelectedObject == this)
        {
            setGrab(false, m.transform);
            setSelect(false, m.transform);
            m.selectedTransform = null;
            m.SelectedObject = null;
            m.emptyGrab = true;
            m.wasGazeBased = false;
            tip = null;
        }
    }
}
