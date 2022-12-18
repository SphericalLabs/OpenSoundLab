// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This file is part of OpenSoundLab, which is based on SoundStage VR.
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

public class handle : manipObject {

  public Transform masterObj;
  Transform masterObjParent;
  public handle otherHandle;
  Material mat;
  public GameObject matTarg;
  public int ID = 0;

  Material highlightMat;
  Material highlightGrabbedMat;

  public override void Awake() {
    base.Awake();
    if (masterObj == null) masterObj = transform.parent;
    masterObjParent = GameObject.Find("PatchAnchor").transform; // move this to instantiation?
    if (ID == 0) {
      if (GetComponent<Renderer>() == null) mat = matTarg.GetComponent<Renderer>().material;
      else mat = GetComponent<Renderer>().sharedMaterial;
    }
    handle[] temp = transform.parent.GetComponentsInChildren<handle>();
    for (int i = 0; i < temp.Length; i++) {
      if (temp[i] != this) otherHandle = temp[i];
    }

    if (ID == 1) mat = otherHandle.GetComponent<Renderer>().sharedMaterial;
    highlightMat = Resources.Load("Materials/Highlight") as Material;
    highlightGrabbedMat = Resources.Load("Materials/HighlightGrabbed") as Material;

  }

  public void setObjectParent(Transform t) {
    masterObjParent = t;
    masterObj.parent = t;
  }

  bool scaling = false;
  public override void grabUpdate(Transform t) {
    
    if (otherHandle.curState == manipState.grabbed) {
      if (ID == 1) {
        scaleUpdate();
      }

      if (manipulatorObj.parent == masterObj.parent) posRotUpdate();
    } else {
      masterObj.parent = manipulatorObj.parent;
      scaling = false;
      doublePosRot = false;
    }
  }

  bool doublePosRot = false;
  Vector3 initOtherManipPos = Vector3.zero;
  Quaternion initRot = Quaternion.identity;
  Vector3 doubleManipOffset = Vector3.zero;
  void posRotUpdate() {
    if (!doublePosRot) {
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

  float initDistance = 0;
  Vector3 initScale = Vector3.one;
  void scaleUpdate() {
    if (!scaling) {
      initDistance = Vector3.Distance(otherHandle.manipulatorObj.position, manipulatorObj.position);
      initScale = masterObj.localScale;
    }
    scaling = true;
    float dist = Vector3.Distance(otherHandle.manipulatorObj.position, manipulatorObj.position);
    masterObj.localScale = initScale * (dist / initDistance);
  }

  public bool trashReady = false;
  public trashcan curTrash;
  void OnCollisionEnter(Collision coll) {
    if (coll.transform.name == "trashMenu" && (curState == manipState.grabbed || otherHandle.curState == manipState.grabbed) && ID == 0) {
      curTrash = coll.transform.GetComponent<trashcan>();
      curTrash.setReady(true);
      if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(1000);
      else if (otherHandle.manipulatorObjScript != null) otherHandle.manipulatorObjScript.hapticPulse(1000);
      trashReady = true;
      //soundUtils.SetupMaterialWithBlendMode(mat, soundUtils.BlendMode.Fade); // this caused a glitch where text was not visible on most objects anymore
    }
  }

  void OnCollisionExit(Collision coll) {
    if (coll.transform.name == "trashMenu" && ID == 0) {
      coll.transform.GetComponent<trashcan>().setReady(false);
      if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(1000);
      else if (otherHandle.manipulatorObjScript != null) otherHandle.manipulatorObjScript.hapticPulse(1000);
      curTrash = null;
      trashReady = false;
      //soundUtils.SetupMaterialWithBlendMode(mat, soundUtils.BlendMode.Opaque);
    }
  }

  public void toggleHandles(bool on) {
    Collider[] colls = GetComponents<Collider>();
    for (int i = 0; i < colls.Length; i++) colls[i].enabled = on;
  }

  public override void setState(manipState state) {
    if (curState == state) return;

    if (curState == manipState.grabbed && state != manipState.grabbed) {
      if (curTrash != null) {
        if (curTrash.gameObject.activeSelf) curTrash.trashEvent();
        else {
          curTrash.setReady(false);
          curTrash = null;
          trashReady = false;
          soundUtils.SetupMaterialWithBlendMode(mat, soundUtils.BlendMode.Opaque);
        }
      } else if (otherHandle.curTrash != null) {
        if (otherHandle.curTrash.gameObject.activeSelf) otherHandle.curTrash.trashEvent();
        else {
          otherHandle.curTrash.setReady(false);
          otherHandle.curTrash = null;
          otherHandle.trashReady = false;
          soundUtils.SetupMaterialWithBlendMode(mat, soundUtils.BlendMode.Opaque);
        }
      } else {
        otherHandle.trashReady = false;
        trashReady = false;
        soundUtils.SetupMaterialWithBlendMode(mat, soundUtils.BlendMode.Opaque);
      }

      if (trashReady || otherHandle.trashReady) {
        if (masterObj.gameObject.GetComponentInChildren<tape>())
          masterObj.gameObject.GetComponentInChildren<tape>().Eject();
        Destroy(masterObj.gameObject);
      } else masterObj.parent = masterObjParent;

      if (!masterControl.instance.handlesEnabled) {
        toggleHandles(false);
        otherHandle.toggleHandles(false);
      }
    }

    curState = state;

    if (ID == 0) {
      if (curState == manipState.none) {
        if (otherHandle.curState == manipState.none) GetComponent<Renderer>().sharedMaterial = mat; 
      }
      if (curState == manipState.selected) {
        GetComponent<Renderer>().sharedMaterial = highlightMat;
      }
      if (curState == manipState.grabbed) {
        GetComponent<Renderer>().sharedMaterial = highlightGrabbedMat;
      }
    } else {
      if (curState == manipState.none) {
        if (otherHandle.curState == manipState.none) otherHandle.GetComponent<Renderer>().sharedMaterial = mat;
      }
      if (curState == manipState.selected) {
        otherHandle.GetComponent<Renderer>().sharedMaterial = highlightMat;
      }
      if (curState == manipState.grabbed) {
        otherHandle.GetComponent<Renderer>().sharedMaterial = highlightGrabbedMat;
      }
    }
    if (curState == manipState.grabbed) {
      masterObj.parent = manipulatorObj.parent;
      doublePosRot = false;
    }
  }
}
