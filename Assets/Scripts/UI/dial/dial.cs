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
using System;
using System.Collections.Generic;
using UnityEngine.Events;

public class dial : manipObject
{

    public float percent = 0f;
    public float defaultPercent;

    public enum dialColor { generic, frequency, amplitude };
    public dialColor currentDialColor = dialColor.generic; // dropdown, defaulting to white

    GameObject littleDisk;
    glowDisk dialFeedback;

    public glowDisk DialFeedback { get => dialFeedback; set => dialFeedback = value; }

    Material glowDiskMat, littleDiskMat, littleDiskSelectedMat, littleDiskGrabbedMat;

    public float controllerRot = 0f;
    public float lastControllerRot = 0;
    public float rotAtBeginningOfGrab = 0;
    public float curRot = 0;

    public float realRot = 0f;
    public float prevShakeRot = 0f;
    public float fineMult = 5f;

    public bool isNotched = false;
    public bool isBipolar = false;
    public int notchSteps = 4;

    private OSLInput oslInput;

    public UnityEvent onPercentChangedEvent;

    public override void Awake()
    {
        base.Awake();

        // store the first value on Awake and keep it as default
        defaultPercent = percent;

        littleDisk = transform.Find("littleDisk").gameObject;
        dialFeedback = transform.parent.Find("glowDisk").GetComponent<glowDisk>();

        loadMaterials(currentDialColor);
        setMaterials(glowDiskMat, littleDiskMat);
        //setGlowState(manipState.none); // no glow variant to save draw calls

        CapsuleCollider coll = GetComponent<CapsuleCollider>(); // globally make them bigger
        coll.radius *= 1.1f;

    }

    void loadMaterials(dialColor colorVariant)
    {

        string str = Enum.GetName(typeof(dialColor), colorVariant); // gets the string name of the enum

        // Please note: setting a material using .material does not immediately instantiate a copy of that material - only if its properties are accessed!
        // Using sharedMaterial here, but the shader has alpha and therefore cannot be fully batched.

        glowDiskMat = Resources.Load<Material>("Materials/" + str + "GlowDisk");
        littleDiskMat = Resources.Load<Material>("Materials/" + str + "LittleDisk");
        littleDiskSelectedMat = Resources.Load<Material>("Materials/selectedLittleDisk");
        littleDiskGrabbedMat = Resources.Load<Material>("Materials/grabbedLittleDisk");

    }

    void setMaterials(Material mat1, Material mat2)
    {
        littleDisk.GetComponent<Renderer>().sharedMaterial = mat2;
        transform.parent.Find("glowDisk").GetComponent<Renderer>().sharedMaterial = mat1;
    }


    void Start()
    {
        setPercent(percent);
        //updatePercent();
    }

    manipulator selectManipulatorObjScript; // this should actually be made available from manipObject.cs

    void Update()
    {
    
        if (percent != defaultPercent)
        {
            if (selectObj != null && selectObj.GetComponent<manipulator>() != null)
            {
                selectManipulatorObjScript = selectObj.GetComponent<manipulator>();
                if (curState == manipState.selected || curState == manipState.grabbed) // these checks might be redundant at this time
                {
                    if (OSLInput.getInstance().isSecondaryPressed(selectManipulatorObjScript.controllerIndex))
                    {
                        setPercent(defaultPercent, true);
                    }
                }
            }

            else if (gazedObjectTracker.Instance.gazedAtManipObject == this && manipulator.NoneTouched()) // this is being gazed at and both controller don't have touch, this ensures physical touch first and only
            {
                if (OSLInput.getInstance().isAnySecondaryPressed())
                {
                    setPercent(defaultPercent, true);
                }
            }
        }

        if (curState == manipState.grabbed)
        {
            updatePercent(true);
        }
    }

    public void setPercent(float p, bool invokeEvent = false)
    {

        if (float.IsNaN(p)) return; // this is used for skipping missing data fields in old save xmls

        percent = Mathf.Clamp01(p);
        Debug.Log($"Set percent {percent}");
        if (isNotched)
        {
            realRot = Utils.map(Mathf.Round(percent * (notchSteps - 1)), 0, notchSteps - 1, -150f, 150f);
        }
        else
        {
            realRot = Utils.map(percent, 0f, 1f, -150f, 150f);
        }

        curRot = realRot; // can be removed?
        transform.localRotation = Quaternion.Euler(0, realRot, 0);

        // viz
        if (dialFeedback != null)
        {
            dialFeedback.percent = percent * 0.85f; // why that multiplier?
            dialFeedback.PercentUpdate();
        }

        updatePercent();

        if (invokeEvent)
        {
            onPercentChangedEvent.Invoke();
        }

    }

    dialColor newDialColor;

    void updatePercent(bool invokeEvent = false)
    {

        percent = Utils.map(realRot, -150f, 150f, 0f, 1f);

        // viz
        dialFeedback.percent = percent * 0.85f; // why that multiplier?
        dialFeedback.PercentUpdate();

        if (isBipolar)
        {
            if (percent == 0.5f)
            {
                newDialColor = dialColor.generic;
            }
            else if (percent > 0.5f)
            {
                newDialColor = dialColor.frequency;
            }
            else
            {
                newDialColor = dialColor.amplitude;
            }
            if (newDialColor != currentDialColor)
            {
                currentDialColor = newDialColor;
                loadMaterials(currentDialColor);
                setMaterials(glowDiskMat, littleDiskMat);
            }

        }

        if (invokeEvent)
        {
            onPercentChangedEvent.Invoke();
        }
    }


    // begin grab, this sets the entry rotation via deltaRot
    public override void setState(manipState state)
    {
        curState = state;
        //setGlowState(state);

        if (curState == manipState.grabbed)
        {
            setMaterials(glowDiskMat, littleDiskGrabbedMat);

            turnCount = 0;

            //// trigger beginners guidance
            //if (!masterControl.instance.dialUsed) { 
            //  if (_dialCheckRoutine != null) StopCoroutine(_dialCheckRoutine);
            //  _dialCheckRoutine = StartCoroutine(dialCheckRoutine());
            //}

            Vector2 temp = dialCoordinates(manipulatorObj.up);
            controllerRot = Vector2.Angle(temp, Vector2.up) * Mathf.Sign(temp.x);

            rotAtBeginningOfGrab = controllerRot - curRot;
            lastControllerRot = (controllerRot - rotAtBeginningOfGrab) * speedUp; // update relative values here so that it doesnt jump on first grab

        }
        else if (curState == manipState.selected)
        {
            setMaterials(glowDiskMat, littleDiskSelectedMat);
        }
        else if (curState == manipState.none)
        {
            setMaterials(glowDiskMat, littleDiskMat);
        }
    }



    public override void grabUpdate(Transform t)
    {
        Vector2 temp = dialCoordinates(manipulatorObj.up);
        controllerRot = (Vector2.Angle(temp, Vector2.up) * Mathf.Sign(temp.x) - rotAtBeginningOfGrab) * speedUp;


        if (OSLInput.getInstance().isSidePressed(manipulatorObjScript.controllerIndex))
        {
            curRot += (controllerRot - lastControllerRot) / fineMult;
        }
        else
        {
            curRot += controllerRot - lastControllerRot;
        }


        lastControllerRot = controllerRot;

        curRot = Mathf.Clamp(curRot, -150f, 150f);

        // catch naughty fliparounds, use realRot as lastRot
        if (realRot == -150f && curRot > 0f) return;
        if (realRot == 150f && curRot < 0f) return;


        realRot = curRot; // for percent storing and viz

        if (isNotched)
        {
            realRot = Utils.map(Mathf.Round(Utils.map(curRot, -150f, 150f, 0f, 1f) * (notchSteps - 1)), 0, notchSteps - 1, -150f, 150f);
        }

        // apply
        transform.localRotation = Quaternion.Euler(0, realRot, 0);

        // haptics
        if (Mathf.Abs(realRot - prevShakeRot) > 10f)
        {
            if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(isNotched ? (ushort)3999 : (ushort)700); // 3999 is max power
            prevShakeRot = realRot;
            turnCount++;
        }


    }


    Vector2 dialCoordinates(Vector3 vec)
    {
        Vector3 flat = transform.parent.InverseTransformDirection(Vector3.ProjectOnPlane(vec, transform.parent.up));
        return new Vector2(flat.x, flat.z);
    }

    float speedUp = 1.4f;
    int turnCount = 0;


    //// code for first steps guidance below, currently not enabled
    //Coroutine _dialCheckRoutine;
    //IEnumerator dialCheckRoutine()
    //{
    //  Vector3 lastPos = Vector3.zero;
    //  float cumulative = 0;
    //  if (manipulatorObj != null)
    //  {
    //    lastPos = manipulatorObj.position;
    //  }

    //  while (curState == manipState.grabbed && manipulatorObj != null && !masterControl.instance.dialUsed)
    //  {
    //    cumulative += Vector3.Magnitude(manipulatorObj.position - lastPos);
    //    lastPos = manipulatorObj.position;

    //    if (turnCount > 3) masterControl.instance.dialUsed = true;
    //    else if (cumulative > .2f)
    //    {
    //      masterControl.instance.dialUsed = true;
    //      Instantiate(Resources.Load("Hints/TurnVignette", typeof(GameObject)), transform.parent, false);
    //    }

    //    yield return null;
    //  }
    //  yield return null;
    //}
}


