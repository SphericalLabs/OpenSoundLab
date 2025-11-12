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
using UnityEngine.Events;

public class key : manipObject
{

    public int keyValue = 0;
    public Material onMat;
    Renderer rend;
    Material offMat;
    Material glowMat;
    deviceInterface _deviceInterface;

    public bool sticky = true;


    public bool isKeyboard = false;

    public UnityEvent onKeyChangedEvent;

    public override void Awake()
    {
        base.Awake();
        _deviceInterface = transform.parent.GetComponent<deviceInterface>();
        rend = GetComponent<Renderer>();
        offMat = rend.sharedMaterial;
        glowMat = new Material(onMat);
    }

    bool initialized = false;
    void Start()
    {
        initialized = true;
    }

    public void setOffMat(Material m)
    {
        rend.sharedMaterial = m;
        offMat = rend.sharedMaterial;
    }

    public bool isHit = false;

    public void keyHitCheck()
    {
        if (!initialized) return;

        bool on = curState == manipState.grabbed || toggled;
        if (!isKeyboard || !sticky)
        {
            on = touching || on;
        }

        if (on != isHit)
        {
            isHit = on;
            _deviceInterface.hit(on, keyValue);
            onKeyChangedEvent.Invoke();
        }
    }

    enum keyState
    {
        off,
        touched,
        grabbedOn,
        grabbedOff,
        selectedOff,
        selectedOn
    };

    int desireSetSelect = 0;
    public void setSelectAsynch(bool on)
    {
        desireSetSelect = on ? 1 : 2;
    }

    bool phantomHitUpdate = false;
    Queue<bool> hits = new Queue<bool>();
    public void phantomHit(bool on, bool triggerDeviceInterface = false)
    {
        phantomHitUpdate = true;
        isHit = on;
        toggled = on;
        if (triggerDeviceInterface)
        {
            _deviceInterface.hit(on, keyValue);
        }
    }

    bool prevToggle = false;
    void Update()
    {
        if (phantomHitUpdate)
        {
            curSelect = 0;
            phantomHitUpdate = false;
            if (isHit)
            {
                if (isKeyboard) setKeyFeedbackState(keyState.selectedOn);
                else
                {
                    if (toggled) setKeyFeedbackState(keyState.touched);
                    else setKeyFeedbackState(keyState.selectedOff);
                }
            }
            else
            {
                setKeyFeedbackState(keyState.off);
            }
        }

        if (!sticky || !isHit) return;

        if (desireSetSelect != 0)
        {
            curSelect = desireSetSelect;
            if (desireSetSelect == 1)
            {
                if (toggled) setKeyFeedbackState(keyState.grabbedOn);
                else setKeyFeedbackState(keyState.selectedOn);
            }
            else
            {
                if (toggled) setKeyFeedbackState(keyState.touched);
                else setKeyFeedbackState(keyState.selectedOff);
            }

            desireSetSelect = 0;
        }

        if (prevToggle != toggled && isHit)
        {
            prevToggle = toggled;
            if (curSelect == 1)
            {
                if (toggled) setKeyFeedbackState(keyState.grabbedOn);
                else setKeyFeedbackState(keyState.selectedOn);
            }
            else
            {
                if (toggled) setKeyFeedbackState(keyState.touched);
                else setKeyFeedbackState(keyState.selectedOff);
            }
        }

    }

    int curSelect = 0;

    bool touching = false;
    public override void onTouch(bool on, manipulator m)
    {
        touching = on;

        if (isKeyboard && sticky)
        {
            if (m != null && m.emptyGrab && on)
            {
                m.hapticPulse(700);
                toggled = !toggled;
                keyHitCheck();
            }
            return;
        }

        if (m != null)
        {
            if (on) m.hapticPulse(700);
        }

        keyHitCheck();
    }


    public bool toggled = false;
    public override void setState(manipState state)
    {
        if (!sticky) return;

        bool lateHitCheck = false;
        if (curState == manipState.grabbed && state != manipState.grabbed)
        {
            lateHitCheck = true;
        }

        curState = state;

        if (curState == manipState.grabbed)
        {
            toggled = !toggled;
            keyHitCheck();
        }
        else if (lateHitCheck) keyHitCheck();
    }

    void setKeyFeedbackState(keyState s)
    {
        switch (s)
        {
            case keyState.off:
                rend.sharedMaterial = offMat;
                break;
            case keyState.touched:
                rend.sharedMaterial = glowMat;
                break;
            case keyState.grabbedOn:
                rend.sharedMaterial = glowMat;
                break;
            case keyState.grabbedOff:
                rend.sharedMaterial = glowMat;
                break;
            case keyState.selectedOff:
                rend.sharedMaterial = glowMat;
                break;
            case keyState.selectedOn:
                rend.sharedMaterial = glowMat;
                break;
            default:
                break;
        }
    }

}
