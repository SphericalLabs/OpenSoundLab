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

public class UIpanel : manipObject {

  public TextMesh label;
  public GameObject outline;
  public Material textMat;
  public Color onColor, offColor;

  public Renderer outlineRender;
  Renderer rend;
  public Material panelMat, panelMatSelected;

  public componentInterface _componentInterface;
  public int buttonID;

  public override void Awake() {
    base.Awake();
    if (transform.parent) _componentInterface = transform.parent.GetComponent<componentInterface>();

    onColor = Color.HSVToRGB(182f / 359, 1f, 118f / 255);
    offColor = Color.HSVToRGB(182f / 359, 0f, 118f / 255);
    textMat = label.GetComponent<Renderer>().sharedMaterial;
    setTextState(false);

    outlineRender = outline.GetComponent<Renderer>();
    outlineRender.sharedMaterial.SetColor("_TintColor", onColor);
    outlineRender.sharedMaterial.SetFloat("_EmissionGain", .428f);
    outlineRender.sharedMaterial.SetFloat("_InvFade", 1f);

    outline.SetActive(false);

    rend = GetComponent<Renderer>();
    rend.sharedMaterial = panelMat;

    AwakeB();
  }

  public virtual void AwakeB() {

  }

  public void setText(string s) {
    label.text = s;
  }

  public virtual void setTextState(bool on) {
    textMat.SetColor("_TintColor", on ? onColor : offColor);
  }

  public override void setState(manipState state) {
    if (curState == manipState.selected && curState != state) selectEvent(false);
    else if (curState == manipState.grabbed && curState != state) grabEvent(false);


    curState = state;
    if (curState == manipState.none) {
      if (!toggled) setTextState(false);
      rend.sharedMaterial = panelMat;
    } else if (curState == manipState.selected) {
      selectEvent(true);
      setTextState(true);
      rend.sharedMaterial = panelMatSelected;
    } else if (curState == manipState.grabbed) {
      setTextState(true);
      keyHit(true);
      grabEvent(true);
    }
  }

  public virtual void grabEvent(bool on) {

  }

  public virtual void selectEvent(bool on) {

  }

  public bool isHit = false;
  public bool toggled = false;
  public void keyHit(bool on) {
    isHit = on;
    toggled = on;
    if (on) {
      if (_componentInterface != null) _componentInterface.hit(on, buttonID);
      setToggleAppearance(true);
    } else {
      if (_componentInterface != null) _componentInterface.hit(on, buttonID);
      setToggleAppearance(false);
    }
  }

  public void setToggleAppearance(bool on) {
    outline.SetActive(on);
    setTextState(on);
  }

  public override void onTouch(bool on, manipulator m) {
    if (m != null) {
      if (m.emptyGrab) {
        if (on) {
          keyHit(true);
        }
      }
    }
  }
}
