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

    Color glowColor = Color.HSVToRGB(.4f, .5f, .1f);

    public bool isKeyboard = false;

    public UnityEvent onKeyChangedEvent;

    public override void Awake()
    {
        base.Awake();
        _deviceInterface = transform.parent.GetComponent<deviceInterface>();
        rend = GetComponent<Renderer>();
        offMat = rend.material;
        glowMat = new Material(onMat);
        glowMat.SetColor("_TintColor", glowColor);
    }

    bool initialized = false;
    void Start()
    {
        initialized = true;
    }

    public void setOffMat(Material m)
    {
        rend.material = m;
        offMat = rend.material;
    }

    public bool isHit = false;

    public void keyHitCheck()
    {
        if (!initialized) return;
        bool on = touching || curState == manipState.grabbed || toggled;

        if (on != isHit)
        {
            isHit = on;
            _deviceInterface.hit(on, keyValue);
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
    public void phantomHit(bool on)
    {
        phantomHitUpdate = true;
        isHit = on;
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
        if (m != null)
        {
            if (on) m.hapticPulse(3000);
            else m.hapticPulse(700);
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
                rend.material = offMat;
                break;
            case keyState.touched:
                rend.material = glowMat;
                setKeyColor(.65f, .3f);
                break;
            case keyState.grabbedOn:
                rend.material = glowMat;
                setKeyColor(.4f, .4f);
                break;
            case keyState.grabbedOff:
                rend.material = glowMat;
                setKeyColor(.65f, .4f);
                break;
            case keyState.selectedOff:
                rend.material = glowMat;
                setKeyColor(.4f, .3f);
                break;
            case keyState.selectedOn:
                rend.material = glowMat;
                setKeyColor(.5f, .4f);
                break;
            default:
                break;
        }
    }

    public void setKeyColor(float hue, float gain)
    {
        rend.material.SetColor("_TintColor", Color.HSVToRGB(hue, .9f, .5f));
        rend.material.SetFloat("_EmissionGain", gain);
    }
}