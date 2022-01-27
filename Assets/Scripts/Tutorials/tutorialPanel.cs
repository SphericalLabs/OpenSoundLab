// Copyright 2017 Google LLC
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

public class tutorialPanel : manipObject {

  public TextMesh label;

  public Renderer panelRenderer;

  public Material normalMat, selectedMat, activeMat;

  public tutorialsDeviceInterface tutorialsManager;

  public bool isActive = false;
  public string videoString = "";
  
  public override void Awake() {
    base.Awake();    
    if (transform.parent.parent) tutorialsManager = transform.parent.parent.GetComponent<tutorialsDeviceInterface>();
  }

  public void setLabel(string labelString){
    label.text = labelString;
  }

  public void setVideo(string str)
  {
    videoString = str;
  }

  public void setActivated(bool active, bool notifyManager = true){
    if (active && !isActive && notifyManager){ // freshly activated, tell the manager once
      tutorialsManager.triggerOpenTutorial(this);
    }
    isActive = active;
    panelRenderer.sharedMaterial = isActive ? activeMat : normalMat;
  }


  public override void setState(manipState state) {
    if(state == manipState.none && !isActive){
      panelRenderer.sharedMaterial = normalMat;
    } else if(state == manipState.selected){
      panelRenderer.sharedMaterial = selectedMat;
    } else if(state == manipState.grabbed){
      setActivated(true);
      manipulatorObjScript.hapticPulse(700);
    }
    //if (curState == manipState.selected && curState != state) selectEvent(false);
    //else if (curState == manipState.grabbed && curState != state) grabEvent(false);


    //curState = state;
    //if (curState == manipState.none) {
    //  if (!toggled) setTextState(false);
    //} else if (curState == manipState.selected) {
    //  selectEvent(true);
    //  setTextState(true);
    //} else if (curState == manipState.grabbed) {
    //  setTextState(true);
    //  keyHit(true);
    //  grabEvent(true);
    //}
  }

  //public virtual void grabEvent(bool on) {

  //}

  //public virtual void selectEvent(bool on) {

  //}

  //public bool isHit = false;
  //public bool toggled = false;
  //public void keyHit(bool on) {
  //  isHit = on;
  //  toggled = on;
  //  if (on) {
  //    if (_componentInterface != null) _componentInterface.hit(on, buttonID);
  //    setToggleAppearance(true);
  //  } else {
  //    if (_componentInterface != null) _componentInterface.hit(on, buttonID);
  //    setToggleAppearance(false);
  //  }
  //}

  //public void setToggleAppearance(bool on) {
  //  outline.SetActive(on);
  //  setTextState(on);
  //}

  //public override void onTouch(bool on, manipulator m) {
  //  if (m != null) {
  //    if (m.emptyGrab) {
  //      if (on) {
  //        keyHit(true);
  //      }
  //    }
  //  }
  //}
}
