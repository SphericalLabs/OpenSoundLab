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

public class UIpanelCancel : manipObject {

  public TextMesh label;
  public GameObject outline;
  Renderer panelRend;
  public Material panelMat, panelMatSelected;
  Material textMat;
  Color onColor, offColor;

  public componentInterface _compInterface;
  public int buttonID;

  public override void Awake() {
    base.Awake();

    panelRend = transform.Find("Outline").gameObject.GetComponent<Renderer>();
    panelRend.sharedMaterial = panelMat;

    if (transform.parent) _compInterface = transform.parent.GetComponent<componentInterface>();

    offColor = Color.HSVToRGB(0, 230f / 255, 118f / 255);
    onColor = Color.HSVToRGB(14f / 255, 0f, 118f / 255);
    label.text = "CANCEL";
    textMat = label.GetComponent<Renderer>().material;
    textMat.SetColor("_TintColor", offColor);

    outline.GetComponent<Renderer>().material.SetColor("_TintColor", offColor);
    outline.GetComponent<Renderer>().material.SetFloat("_EmissionGain", .428f);
    outline.GetComponent<Renderer>().material.SetFloat("_InvFade", 1f);
  }

  public void setText(string s) {
    label.text = s;
  }

  public override void setState(manipState state) {
    if (curState == manipState.grabbed && curState != state) {
      keyHit(false);
    }

    curState = state;

    if (curState == manipState.none) {
      if (!toggled) textMat.SetColor("_TintColor", offColor);
      panelRend.sharedMaterial = panelMat;
    } else if (curState == manipState.selected) {
      textMat.SetColor("_TintColor", onColor);
      panelRend.sharedMaterial = panelMatSelected;
    } else if (curState == manipState.grabbed) {
      textMat.SetColor("_TintColor", onColor);
      keyHit(true);
    }
  }

  public bool isHit = false;
  bool toggled = false;
  public void keyHit(bool on) {
    isHit = on;
    toggled = on;
    if (on) {
      if (_compInterface != null) _compInterface.hit(on, buttonID);
      outline.GetComponent<Renderer>().material.SetColor("_TintColor", onColor);
      textMat.SetColor("_TintColor", onColor);
    } else {
      if (_compInterface != null) _compInterface.hit(on, buttonID);
      outline.GetComponent<Renderer>().material.SetColor("_TintColor", offColor);
      textMat.SetColor("_TintColor", offColor);
    }
  }

  public override void onTouch(bool on, manipulator m) {
    if (m != null) {
      if (m.emptyGrab) {
        keyHit(on);
      }
    }
  }
}
