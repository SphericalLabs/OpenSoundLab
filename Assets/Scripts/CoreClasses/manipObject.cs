// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright � 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
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
using UnityEngine.Events;

public class manipObject : MonoBehaviour
{
    public manipState curState = manipState.none;

    public Transform manipulatorObj, selectObj;
    public manipulator manipulatorObjScript;
    public bool stickyGrip = false;
    public bool canBeDeleted = false;

    protected bool selected = false;
    protected bool grabbed = false;

    public UnityEvent onStartGrabEvents;
    public UnityEvent onEndGrabEvents;

    private bool canBeGrabed = true;

    public bool CanBeGrabed { get => canBeGrabed; set => canBeGrabed = value; }

    public virtual void Awake()
    {
        gameObject.layer = 9;
    }

    public enum manipState
    {
        none,
        selected,
        grabbed
    };

    public virtual void copy(manipulator m)
    {
    }

    public virtual void selfdelete()
    {
    }

    public virtual void grabUpdate(Transform t)
    {
    }

    public virtual void selectUpdate(Transform t)
    {
    }

    public virtual void setState(manipState state)
    {
        curState = state;
    }

    public virtual void onTouch(bool on, manipulator m)
    {
    }

    public virtual void setTouch(bool on) { }
    public virtual void setPress(bool on) { }
    public virtual void updateTouchPos(Vector2 p) { }

    void OnDisable()
    {
        if (curState == manipState.grabbed) if (manipulatorObjScript != null) manipulatorObjScript.ForceRelease();
        StopAllCoroutines();
    }


    public void setSelect(bool on, Transform t)
    {
        selected = on;
        if (selected)
        {
            selectObj = t;
            setState(manipState.selected);
        }
        else
        {
            selectObj = null;
            setState(manipState.none);
        }
    }

    public virtual void setGrab(bool on, Transform t)
    {
        grabbed = on;
        if (on)
        {
            manipulatorObj = t;
            manipulatorObjScript = t.GetComponent<manipulator>();
            setState(manipState.grabbed);
            onStartGrabEvents.Invoke();
        }
        else
        {
            manipulatorObj = null;
            manipulatorObjScript = null;
            if (selected) setState(manipState.selected);
            else setState(manipState.none);
            onEndGrabEvents.Invoke();
        }
    }
}
