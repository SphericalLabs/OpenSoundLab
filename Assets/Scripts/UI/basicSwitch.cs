// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of SoundStage Lab.
//
// SoundStage Lab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// SoundStage Lab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SoundStage Lab.  If not, see <http://www.gnu.org/licenses/>.

using UnityEngine;
using System.Collections;

public class basicSwitch : manipObject {
  public Transform onLabel, offLabel;
  Material[] labelMats;
  public bool switchVal = false;
  public Transform switchObject;
  float rotationIncrement = 45f;
  public Transform glowTrans;
  Material mat;

  public bool redOption = true;

  public override void Awake() {
    base.Awake();
    glowTrans.gameObject.SetActive(false);
    //mat = glowTrans.GetComponent<Renderer>().sharedmaterial;    
    //mat.SetColor("_TintColor", Color.black);

    if (onLabel != null && offLabel != null) {
      labelMats = new Material[2];
      labelMats[0] = onLabel.GetComponent<Renderer>().material;
      labelMats[1] = offLabel.GetComponent<Renderer>().material;

      labelMats[0].SetColor("_TintColor", Color.HSVToRGB(.4f, 0f, 1f));
      labelMats[0].SetFloat("_EmissionGain", .0f);

      labelMats[1].SetColor("_TintColor", Color.HSVToRGB(redOption ? 0 : .4f, 0f, 1f));
      labelMats[1].SetFloat("_EmissionGain", .0f);
    }

    setSwitch(switchVal, true);
  }

  public void setSwitch(bool on, bool forced = false) {
    if (switchVal == on && !forced) return;
    if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(1000);
    switchVal = on;
    float rot = rotationIncrement * (switchVal ? 1 : -1);
    switchObject.localRotation = Quaternion.Euler(rot, 0, 0);
    
    //if (onLabel != null && offLabel != null) {
    //  labelMats[0].SetColor("_TintColor", Color.HSVToRGB(.4f, .7f, on ? .9f : .1f));
    //  labelMats[0].SetFloat("_EmissionGain", on ? .0f : .0f);

    //  labelMats[1].SetColor("_TintColor", Color.HSVToRGB(redOption ? 0 : .4f, .7f, !on ? .9f : .1f));
    //  labelMats[1].SetFloat("_EmissionGain", !on ? .0f : .0f);
    //}
  }

  public override void grabUpdate(Transform t) {
    float curY = transform.InverseTransformPoint(manipulatorObj.position).z - offset;
    if (Mathf.Abs(curY) > 0.01f) setSwitch(curY > 0);
  }

  float offset;
  public override void setState(manipState state) {
    curState = state;
    if (curState == manipState.none) {
      glowTrans.gameObject.SetActive(false);
    } else if (curState == manipState.selected) {
      glowTrans.gameObject.SetActive(true);
    } else if (curState == manipState.grabbed) {
      glowTrans.gameObject.SetActive(true);
      offset = transform.InverseTransformPoint(manipulatorObj.position).z;
    }
  }
}
