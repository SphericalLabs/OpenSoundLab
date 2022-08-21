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

public class manipObject : MonoBehaviour {
  public manipState curState = manipState.none;

  public Transform manipulatorObj, selectObj;
  public manipulator manipulatorObjScript;
  public bool stickyGrip = false;
  public bool canBeDeleted = false;

  bool selected = false;
  bool grabbed = false;

  public virtual void Awake() {
    gameObject.layer = 9;
  }

  public enum manipState {
    none,
    selected,
    grabbed
  };

  public virtual void copy(manipulator m) {
  }

  public virtual void selfdelete() {
  }

  public virtual void grabUpdate(Transform t) {
  }

  public virtual void selectUpdate(Transform t) {
  }

  public virtual void setState(manipState state) {
    curState = state;
  }

  public virtual void onTouch(bool on, manipulator m) {
  }

  public virtual void setTouch(bool on) { }
  public virtual void setPress(bool on) { }
  public virtual void updateTouchPos(Vector2 p) { }

  void OnDisable() {
    if (curState == manipState.grabbed) if (manipulatorObjScript != null) manipulatorObjScript.ForceRelease();
    StopAllCoroutines();
  }


  public void setSelect(bool on, Transform t) {
    selected = on;
    if (selected) {
      selectObj = t;
      setState(manipState.selected);
    } else {
      selectObj = null;
      setState(manipState.none);
    }
  }

  public void setGrab(bool on, Transform t) {
    grabbed = on;
    if (on) {

      manipulatorObj = t;
      manipulatorObjScript = t.GetComponent<manipulator>();
      setState(manipState.grabbed);
    } else {
      manipulatorObj = null;
      manipulatorObjScript = null;
      if (selected) setState(manipState.selected);
      else setState(manipState.none);
    }
  }
}
