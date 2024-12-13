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
using Mirror;

public enum FollowType { Follow, Parenting};

public class handle : manipObject
{
    public FollowType followType = FollowType.Follow;
    public Transform masterObj;
    Transform masterObjParent;
    public handle otherHandle;
    Material mat;
    public GameObject matTarg;
    public int ID = 0;

    Material highlightMat;
    Material highlightGrabbedMat;


    Vector3 initialOffset;
    Quaternion initialRotationOffset;

    bool wasPrecisionGazeGrabbed = false; // at last frame

    bool doublePosRot = false;
    Vector3 initOtherManipPos = Vector3.zero;
    Quaternion initRot = Quaternion.identity;
    Vector3 doubleManipOffset = Vector3.zero;

    float initDistance = 0;
    Vector3 initScale = Vector3.one;

    public bool trashReady = false;
    public trashcan curTrash;
    public UnityEvent onTrashEvents;

    public override void Awake()
    {
        base.Awake();
        if (masterObj == null) masterObj = transform.parent;
        masterObjParent = GameObject.Find("PatchAnchor").transform; // move this to instantiation?
        if (ID == 0)
        {
            if (GetComponent<Renderer>() == null) mat = matTarg.GetComponent<Renderer>().material;
            else mat = GetComponent<Renderer>().sharedMaterial;
        }
        handle[] temp = transform.parent.GetComponentsInChildren<handle>();
        for (int i = 0; i < temp.Length; i++)
        {
            if (temp[i] != this) otherHandle = temp[i];
        }

        if (ID == 1) mat = otherHandle.GetComponent<Renderer>().sharedMaterial;
        highlightMat = Resources.Load("Materials/Highlight") as Material;
        highlightGrabbedMat = Resources.Load("Materials/HighlightGrabbed") as Material;

    }

    public void setObjectParent(Transform t)
    {
        masterObjParent = t;
        masterObj.parent = t;
    }

    bool scaling = false;
    public override void grabUpdate(Transform t)
    {

    if (manipulatorObjScript != null && manipulatorObjScript.wasGazeBased)
    {
      gazeBasedPosRotUpdate();
      return;
    }

        if (otherHandle.curState == manipState.grabbed)
        {
            if (ID == 1)
            {
                scaleUpdate();
            }

            if (manipulatorObj.parent == masterObj.parent) posRotUpdate();
        }
        else
        {
            switch (followType)
            {
                case FollowType.Follow:
                    masterObj.transform.position = manipulatorObjScript.GrabbedFollowPointTransform.position;
                    masterObj.transform.rotation = manipulatorObjScript.GrabbedFollowPointTransform.rotation;
                    break;
                case FollowType.Parenting:
                    if (masterObj.parent != manipulatorObj.parent)
                    {
                        masterObj.parent = manipulatorObj.parent;
                    }
                    break;
            }
            scaling = false;
            doublePosRot = false;
        }
    }

    public override void setGrab(bool on, Transform t)
    {
        //base.setGrab(on, t);grabbed = on;
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
            if (!trashReady && !otherHandle.trashReady)
            {
                onEndGrabEvents.Invoke();
            }
        }
    }

    void gazeBasedPosRotStart()
    {

        Transform go1 = manipulatorObj.transform;
        Transform go2 = this.transform.parent;

        initialOffset = go2.position - go1.position;
        initialRotationOffset = Quaternion.Inverse(go1.rotation) * go2.rotation;
    }


    void gazeBasedPosRotUpdate()
    {

        if (OSLInput.getInstance().isSidePressed(manipulatorObjScript.controllerIndex) == false) // fine by default
        {
            masterObj.parent = masterObjParent;

            if (!wasPrecisionGazeGrabbed)
            {
                gazeBasedPosRotStart();
            }

            Transform go1 = manipulatorObj.transform;
            Transform go2 = this.transform.parent;

            // Calculate the desired position in world space for go2 based on the changes you want
            Vector3 desiredPosition = go1.position + initialOffset;

            // Apply changes to the local position of go2 based on the desired position
            go2.localPosition = go2.parent.InverseTransformPoint(desiredPosition);

            // Calculate the desired rotation for go2 relative to go1
            Quaternion desiredRotation = go1.rotation * initialRotationOffset;

            // Apply changes to the local rotation of go2 relative to go1
            go2.localRotation = Quaternion.Inverse(go1.localRotation) * desiredRotation;

            wasPrecisionGazeGrabbed = true;
        }
        else // coarse
        {
            masterObj.parent = manipulatorObj.parent;
            wasPrecisionGazeGrabbed = false;
        }

    }


    void posRotUpdate()
    {
        if (!doublePosRot)
        {
            initOtherManipPos = manipulatorObj.InverseTransformPoint(otherHandle.manipulatorObj.position);
            initRot = masterObj.localRotation;
            doubleManipOffset = masterObj.position - Vector3.Lerp(manipulatorObj.position, otherHandle.manipulatorObj.position, .5f);
        }
        doublePosRot = true;

        Vector3 otherManipPos = manipulatorObj.InverseTransformPoint(otherHandle.manipulatorObj.position);
        Quaternion q = Quaternion.FromToRotation(initOtherManipPos, otherManipPos);
        masterObj.localRotation = q * initRot;
        masterObj.position = Vector3.Lerp(manipulatorObj.position, otherHandle.manipulatorObj.position, .5f) + doubleManipOffset;
    }

    void scaleUpdate()
    {
        if (!scaling)
        {
            initDistance = Vector3.Distance(otherHandle.manipulatorObj.position, manipulatorObj.position);
            initScale = masterObj.localScale;
        }
        scaling = true;
        float dist = Vector3.Distance(otherHandle.manipulatorObj.position, manipulatorObj.position);
        masterObj.localScale = initScale * (dist / initDistance);
    }

    void OnCollisionEnter(Collision coll)
    {
        if (coll.transform.name == "trashMenu" && (curState == manipState.grabbed || otherHandle.curState == manipState.grabbed) && ID == 0)
        {
            curTrash = coll.transform.GetComponent<trashcan>();
            curTrash.setReady(true);
            if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(1000);
            else if (otherHandle.manipulatorObjScript != null) otherHandle.manipulatorObjScript.hapticPulse(1000);
            trashReady = true;
            //soundUtils.SetupMaterialWithBlendMode(mat, soundUtils.BlendMode.Fade); // this caused a glitch where text was not visible on most objects anymore
        }
    }

    void OnCollisionExit(Collision coll)
    {
        if (coll.transform.name == "trashMenu" && ID == 0)
        {
            coll.transform.GetComponent<trashcan>().setReady(false);
            if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(1000);
            else if (otherHandle.manipulatorObjScript != null) otherHandle.manipulatorObjScript.hapticPulse(1000);
            curTrash = null;
            trashReady = false;
            //soundUtils.SetupMaterialWithBlendMode(mat, soundUtils.BlendMode.Opaque);
        }
    }

    public void toggleHandles(bool on)
    {
        Collider[] colls = GetComponents<Collider>();
        for (int i = 0; i < colls.Length; i++) colls[i].enabled = on;
    }

    public override void setState(manipState state)
    {
        if (curState == state) return;

        if (curState == manipState.grabbed && state != manipState.grabbed)
        {
            if (curTrash != null)
            {
                if (curTrash.gameObject.activeSelf) curTrash.trashEvent();
                else
                {
                    curTrash.setReady(false);
                    curTrash = null;
                    trashReady = false;
                    soundUtils.SetupMaterialWithBlendMode(mat, soundUtils.BlendMode.Opaque);
                }
            }
            else if (otherHandle.curTrash != null)
            {
                if (otherHandle.curTrash.gameObject.activeSelf) otherHandle.curTrash.trashEvent();
                else
                {
                    otherHandle.curTrash.setReady(false);
                    otherHandle.curTrash = null;
                    otherHandle.trashReady = false;
                    soundUtils.SetupMaterialWithBlendMode(mat, soundUtils.BlendMode.Opaque);
                }
            }
            else
            {
                otherHandle.trashReady = false;
                trashReady = false;
                soundUtils.SetupMaterialWithBlendMode(mat, soundUtils.BlendMode.Opaque);
            }

            if (trashReady || otherHandle.trashReady)
            {
                if (masterObj.gameObject.GetComponentInChildren<tape>())
                    masterObj.gameObject.GetComponentInChildren<tape>().Eject();
                if (NetworkManager.singleton != null && NetworkManager.singleton.mode != NetworkManagerMode.Offline)
                {
                    onTrashEvents.Invoke();
                }
                else
                {
                    onTrashEvents.Invoke();
                    Destroy(masterObj.gameObject);
                }
            }
            else masterObj.parent = masterObjParent;

            if (!masterControl.instance.handlesEnabled)
            {
                toggleHandles(false);
                otherHandle.toggleHandles(false);
            }
        }

        curState = state;

        if (ID == 0)
        {
            if (curState == manipState.none)
            {
                if (otherHandle.curState == manipState.none) GetComponent<Renderer>().sharedMaterial = mat;
            }
            if (curState == manipState.selected)
            {
                GetComponent<Renderer>().sharedMaterial = highlightMat;
            }
            if (curState == manipState.grabbed)
            {
                GetComponent<Renderer>().sharedMaterial = highlightGrabbedMat;
            }
        }
        else
        {
            if (curState == manipState.none)
            {
                if (otherHandle.curState == manipState.none) otherHandle.GetComponent<Renderer>().sharedMaterial = mat;
            }
            if (curState == manipState.selected)
            {
                otherHandle.GetComponent<Renderer>().sharedMaterial = highlightMat;
            }
            if (curState == manipState.grabbed)
            {
                otherHandle.GetComponent<Renderer>().sharedMaterial = highlightGrabbedMat;
            }
        }
        if (curState == manipState.grabbed && !manipulatorObjScript.wasGazeBased)
        {
            switch (followType)
            {
                case FollowType.Follow:
                    manipulatorObjScript.GrabbedFollowPointTransform.position = masterObj.transform.position;
                    manipulatorObjScript.GrabbedFollowPointTransform.rotation = masterObj.transform.rotation;
                    break;
                case FollowType.Parenting:
                    masterObj.parent = manipulatorObj.parent;
                    break;
            }
            doublePosRot = false;
        }

        if (curState == manipState.grabbed && manipulatorObjScript.wasGazeBased)
        {
            gazeBasedPosRotStart();
        }
    }
}
