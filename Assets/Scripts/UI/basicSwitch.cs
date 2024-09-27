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
using UnityEngine.Events;

public class basicSwitch : manipObject
{
    public Transform onLabel, offLabel;
    Material[] labelMats;
    public bool switchVal = false;
    public Transform switchObject;
    float rotationIncrement = 45f;
    public Transform glowTrans;
    Material mat;

    public bool redOption = true;

    public UnityEvent onSwitchChangedEvent;

    public override void Awake()
    {
        base.Awake();
        glowTrans.gameObject.SetActive(false);
        //mat = glowTrans.GetComponent<Renderer>().sharedmaterial;    
        //mat.SetColor("_TintColor", Color.black);

        if (onLabel != null && offLabel != null)
        {
            labelMats = new Material[2];
            labelMats[0] = onLabel.GetComponent<Renderer>().material;
            labelMats[1] = offLabel.GetComponent<Renderer>().material;

            labelMats[0].SetColor("_TintColor", Color.HSVToRGB(.4f, 0f, 1f));
            labelMats[0].SetFloat("_EmissionGain", .0f);

            labelMats[1].SetColor("_TintColor", Color.HSVToRGB(redOption ? 0 : .4f, 0f, 1f));
            labelMats[1].SetFloat("_EmissionGain", .0f);
        }

        setSwitch(switchVal, true);
    }

    public void setSwitch(bool on, bool forced = false, bool invokeEvent = false)
    {
        if (switchVal == on && !forced) return;
        if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(1000);
        switchVal = on;
        float rot = rotationIncrement * (switchVal ? 1 : -1);
        switchObject.localRotation = Quaternion.Euler(rot, 0, 0);

        //if (onLabel != null && offLabel != null) {
        //  labelMats[0].SetColor("_TintColor", Color.HSVToRGB(.4f, .7f, on ? .9f : .1f));
        //  labelMats[0].SetFloat("_EmissionGain", on ? .0f : .0f);

        //  labelMats[1].SetColor("_TintColor", Color.HSVToRGB(redOption ? 0 : .4f, .7f, !on ? .9f : .1f));
        //  labelMats[1].SetFloat("_EmissionGain", !on ? .0f : .0f);
        //}

        if (invokeEvent)
        {
            onSwitchChangedEvent.Invoke();
        }
    }

    public override void grabUpdate(Transform t)
    {
        float curY = transform.InverseTransformPoint(manipulatorObj.position).z - offset;
        if (Mathf.Abs(curY) > 0.01f) setSwitch(curY > 0, false, true);
    }

    float offset;
    public override void setState(manipState state)
    {
        curState = state;
        if (curState == manipState.none)
        {
            glowTrans.gameObject.SetActive(false);
        }
        else if (curState == manipState.selected)
        {
            glowTrans.gameObject.SetActive(true);
        }
        else if (curState == manipState.grabbed)
        {
            glowTrans.gameObject.SetActive(true);
            offset = transform.InverseTransformPoint(manipulatorObj.position).z;
        }
    }
}
