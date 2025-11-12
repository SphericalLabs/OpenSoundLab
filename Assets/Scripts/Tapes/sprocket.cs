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

public class sprocket : manipObject
{
    Renderer rend;
    Collider coll;
    Color glowColor;
    float glowEmission;
    float sprocketRadius;

    Transform masterObj;
    libraryDeviceInterface _deviceInterface;

    float mod = 0;

    public override void Awake()
    {
        base.Awake();
        rend = GetComponent<Renderer>();
        coll = GetComponent<Collider>();
        masterObj = transform.parent;
    }

    public void Setup(Transform m, float r, float e, Color g)
    {
        masterObj = m;
        _deviceInterface = m.GetComponent<libraryDeviceInterface>();
        sprocketRadius = r;
        glowEmission = e;
        glowColor = g;
    }

    float lastYdif = 0;
    public override void grabUpdate(Transform t)
    {
        Vector3 pos = masterObj.InverseTransformPoint(t.position);
        float yDif = (pos.y - startPos.y) * -180 / (Mathf.PI * sprocketRadius);


        if (!_deviceInterface.spinLocks[0] && !_deviceInterface.spinLocks[1]) transform.parent.localRotation = Quaternion.Euler(yDif, 0, 0) * startRot;
        else if (_deviceInterface.spinLocks[0])
        {

            if (yDif < lastYdif) transform.parent.localRotation = Quaternion.Euler(yDif, 0, 0) * startRot;
            else yDif = lastYdif;
        }
        else if (_deviceInterface.spinLocks[1])
        {
            if (yDif > lastYdif) transform.parent.localRotation = Quaternion.Euler(yDif, 0, 0) * startRot;
            else yDif = lastYdif;
        }

        lastYdif = yDif;

    }

    Vector3 startPos;
    Quaternion startRot;
    public override void setState(manipState state)
    {
        if (curState == manipState.grabbed && state != curState)
        {

        }
        curState = state;
        if (curState == manipState.none)
        {
            mod = 0;
            transform.localScale = Vector3.one * .005f;
        }
        else if (curState == manipState.selected)
        {
            mod = 0;
            transform.localScale = Vector3.one * .007f;
        }
        else if (curState == manipState.grabbed)
        {
            startRot = transform.parent.localRotation;
            startPos = masterObj.InverseTransformPoint(manipulatorObj.position);
            startPos.x = 0;
            mod = .5f;
        }
    }

    Vector3 lastPos;
    public void UpdatePosition()
    {
        Vector3 newPos = masterObj.InverseTransformPoint(transform.position);
        if (newPos != lastPos)
        {
            lastPos = newPos;
            UpdateAlpha(newPos.z);
        }
    }

    void UpdateAlpha(float z)
    {
        z = Mathf.Clamp01(z / sprocketRadius) + mod;
        if (z > 0)
        {
            rend.enabled = coll.enabled = true;
            rend.sharedMaterial.SetFloat("_EmissionGain", glowEmission * z);
            rend.sharedMaterial.SetColor("_TintColor", Color.Lerp(Color.clear, glowColor, z));
        }
        else
        {
            rend.enabled = coll.enabled = false;
        }
    }
}
