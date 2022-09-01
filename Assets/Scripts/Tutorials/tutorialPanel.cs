// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of OpenSoundLab.
//
// OpenSoundLab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenSoundLab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenSoundLab.  If not, see <http://www.gnu.org/licenses/>.

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
    } else if(state == manipState.selected && !isActive){
      panelRenderer.sharedMaterial = selectedMat;
    } else if(state == manipState.grabbed){
      setActivated(true);
      manipulatorObjScript.hapticPulse(700);
    }

  }


  public override void onTouch(bool on, manipulator m)
  {
    if (m != null)
    {
      if (m.emptyGrab)
      {
        setActivated(true);
        m.hapticPulse(2000);
      }
    }
  }
}
