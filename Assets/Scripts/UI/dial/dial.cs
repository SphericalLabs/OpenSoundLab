// Copyright 2017-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
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
using UnityEngine.XR;
using System.Collections;
using System;
using System.Collections.Generic;

public class dial : manipObject {

  public float percent = 0f;
  float defaultPercent; 

  public enum dialColors {generic, frequency, amplitude};
  public dialColors dialColor = dialColors.generic; // dropdown, defaulting to white

  GameObject littleDisk;
  glowDisk dialFeedback;

  Material glowDiskMat, littleDiskMat, littleDiskSelectedMat, littleDiskGrabbedMat;

  public float controllerRot = 0f;
  public float lastControllerRot = 0;
  public float rotAtBeginningOfGrab = 0;
  public float curRot = 0;
  
  public float realRot = 0f;
  public float prevShakeRot = 0f;
  public float fineMult = 5f;

  public bool isNotched = false;
  public int notchSteps = 4;

  public override void Awake() {
    base.Awake();    
    
    // store the first value on Awake and keep it as default
    defaultPercent = percent;

    littleDisk = transform.Find("littleDisk").gameObject;
    dialFeedback = transform.parent.Find("glowDisk").GetComponent<glowDisk>();

    loadMaterials(dialColor);
    setMaterials(glowDiskMat, littleDiskMat);
    //setGlowState(manipState.none); // no glow variant to save draw calls

    CapsuleCollider coll = GetComponent<CapsuleCollider>(); // globally make them bigger
    coll.radius *= 1.75f;

  }

  void loadMaterials(dialColors colorVariant){
    
    string str = Enum.GetName(typeof(dialColors), colorVariant); // gets the string name of the enum

    // Please note: setting a material using .material does not immediately instantiate a copy of that material - only if its properties are accessed!
    // Using sharedMaterial here, but the shader has alpha and therefore cannot be fully batched.

    glowDiskMat = Resources.Load<Material>("Materials/" + str + "GlowDisk");
    littleDiskMat = Resources.Load<Material>("Materials/" + str + "LittleDisk");
    littleDiskSelectedMat = Resources.Load<Material>("Materials/selectedLittleDisk");
    littleDiskGrabbedMat = Resources.Load<Material>("Materials/grabbedLittleDisk");

  }

  void setMaterials(Material mat1, Material mat2){
    littleDisk.GetComponent<Renderer>().sharedMaterial = mat2;
    transform.parent.Find("glowDisk").GetComponent<Renderer>().sharedMaterial = mat1;
  }


  void Start() {
    setPercent(percent);
  }

  void Update() {

    if (curState == manipState.selected || curState == manipState.grabbed)
    {
      // reset dial to default
      //if ((selectObj.parent.parent.name == "ControllerLeft" && OVRInput.Get(OVRInput.RawButton.Y)
      //|| (selectObj.parent.parent.name == "ControllerRight" && OVRInput.Get(OVRInput.RawButton.B))))
      if (OVRInput.Get(OVRInput.RawButton.Y) || OVRInput.Get(OVRInput.RawButton.B))
      {
        setPercent(defaultPercent);
        rotAtBeginningOfGrab = controllerRot - curRot;
      }
    }

    updatePercent();
  }

  public void setPercent(float p) {
    percent = Mathf.Clamp01(p);
    if (isNotched)
    {
      realRot = Utils.map(Mathf.Round(percent * (notchSteps - 1)), 0, notchSteps - 1, -150f, 150f);
    } else {
      realRot = Utils.map(percent, 0f, 1f, -150f, 150f);
    }
    
    curRot = realRot; // can be removed?
    transform.localRotation = Quaternion.Euler(0, realRot, 0);
  }

  void updatePercent() {

    percent = Utils.map(realRot, -150f, 150f, 0f, 1f);

    // viz
    dialFeedback.percent = percent * 0.85f; // why that multiplier?
    dialFeedback.PercentUpdate();
  }

  

  // begin grab, this sets the entry rotation via deltaRot
  public override void setState(manipState state) {
    curState = state;
    //setGlowState(state);

    if (curState == manipState.grabbed) {
      setMaterials(glowDiskMat, littleDiskGrabbedMat); 
      
      turnCount = 0;
      
      // trigger beginners guidance
      if (!masterControl.instance.dialUsed) { 
        if (_dialCheckRoutine != null) StopCoroutine(_dialCheckRoutine);
        _dialCheckRoutine = StartCoroutine(dialCheckRoutine());
      }
      
      Vector2 temp = dialCoordinates(manipulatorObj.up);
      controllerRot = Vector2.Angle(temp, Vector2.up) * Mathf.Sign(temp.x); 
      rotAtBeginningOfGrab = controllerRot - curRot;
      lastControllerRot = (controllerRot - rotAtBeginningOfGrab) * speedUp; // update relative values here so that it doesnt jump on first grab
      
    } else if(curState == manipState.selected) {
      setMaterials(glowDiskMat, littleDiskSelectedMat);
    } else if (curState == manipState.none){
      setMaterials(glowDiskMat, littleDiskMat);
    }
  }


  Vector2 dialCoordinates(Vector3 vec)
  {
    Vector3 flat = transform.parent.InverseTransformDirection(Vector3.ProjectOnPlane(vec, transform.parent.up));
    return new Vector2(flat.x, flat.z);
  }

  float speedUp = 1.4f;

  public override void grabUpdate(Transform t)
  {
    Vector2 temp = dialCoordinates(manipulatorObj.up);
    controllerRot = (Vector2.Angle(temp, Vector2.up) * Mathf.Sign(temp.x) - rotAtBeginningOfGrab) * speedUp;
    
    // fine tune modifier
    // https://developer.oculus.com/documentation/unity/unity-ovrinput
    // only raw input worked properly
    //if ((t.parent.parent.name == "ControllerLeft" && OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger) > 0.1f)
    //|| (t.parent.parent.name == "ControllerRight" && OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger) > 0.1f)) {
    if (OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger) > 0.1f || OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger) > 0.1f) {
      curRot += (controllerRot - lastControllerRot) / fineMult;
    } else {
      curRot += controllerRot - lastControllerRot;
    }
      
    lastControllerRot = controllerRot;

    curRot = Mathf.Clamp(curRot, -150f, 150f);

    // catch naughty fliparounds, use realRot as lastRot
    if (realRot == -150f && curRot > 0f) return;
    if (realRot ==  150f && curRot < 0f) return;


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
      if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse( isNotched ? (ushort) 3999 : (ushort) 700); // 3999 is max power
      prevShakeRot = realRot;
      turnCount++;
    }

  }




  // code for first steps guidance below, currently not enabled
  int turnCount = 0;
  Coroutine _dialCheckRoutine;
  IEnumerator dialCheckRoutine()
  {
    Vector3 lastPos = Vector3.zero;
    float cumulative = 0;
    if (manipulatorObj != null)
    {
      lastPos = manipulatorObj.position;
    }

    while (curState == manipState.grabbed && manipulatorObj != null && !masterControl.instance.dialUsed)
    {
      cumulative += Vector3.Magnitude(manipulatorObj.position - lastPos);
      lastPos = manipulatorObj.position;

      if (turnCount > 3) masterControl.instance.dialUsed = true;
      else if (cumulative > .2f)
      {
        masterControl.instance.dialUsed = true;
        Instantiate(Resources.Load("Hints/TurnVignette", typeof(GameObject)), transform.parent, false);
      }

      yield return null;
    }
    yield return null;
  }
}


