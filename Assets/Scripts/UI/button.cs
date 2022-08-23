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
using System.Collections.Generic;

public class button : manipObject {
  public bool isToggle = false;
  public bool toggleKey = false;
  public int buttonID;
  public int[] button2DID = new int[] { 0, 0 };
  public bool glowMatOnToggle = true;
  public Material onMat;
  public Material highlightMat;

  Renderer rend;
  Material offMat;
  Material glowMat;
  public componentInterface _componentInterface;
  public GameObject selectOverlay;
  public float glowHue = 0;
  Color glowColor = Color.HSVToRGB(0, .5f, .25f);
  Color offColor;

  public bool onlyOn = false;

  bool singleID = true;

  Renderer labelRend;
  public Color labelColor = new Color(0.75f, .75f, 1f);
  public float labelEmission = .0f;
  public float glowEmission = .3f;

  Queue<bool> hits = new Queue<bool>();
  public bool startToggled = false;
  public bool disregardStartToggled = false;

  public bool changeOverlayGlow = false;

  public override void Awake() {
    base.Awake();
    toggleKey = false;
    glowColor = Color.HSVToRGB(glowHue, .5f, .25f);

    if (_componentInterface == null) {
      if (transform.parent) _componentInterface = transform.parent.GetComponent<componentInterface>();
    }

    rend = GetComponent<Renderer>();
    offMat = rend.material;
    //offColor = offMat.GetColor("_Color");
    glowMat = new Material(onMat);
    glowMat.SetFloat("_EmissionGain", glowEmission);
    glowMat.SetColor("_TintColor", glowColor);
    selectOverlay.SetActive(false);

    if (changeOverlayGlow) {
      selectOverlay.GetComponent<Renderer>().material.SetColor("_TintColor", glowColor);
      selectOverlay.GetComponent<Renderer>().material.SetFloat("_EmissionGain", glowEmission);
    }

    if (GetComponentInChildren<TextMesh>() != null) labelRend = GetComponentInChildren<TextMesh>().transform.GetComponent<Renderer>();
    if (labelRend != null) {
      labelRend.material.SetFloat("_EmissionGain", .1f);
      labelRend.material.SetColor("_TintColor", labelColor);
    }
  }

  void Start() {
    if (disregardStartToggled) return;
    keyHit(startToggled);
  }

  // used for sequencer highlighting
  public void Highlight(bool on) {    
    if (on) {
      rend.sharedMaterial = isHit ? glowMat : highlightMat;
      //glowMat.SetColor("_TintColor", new Color(1f, 1f, 1f));
      //offMat.SetColor("_Color", glowColor);
    } else {
      rend.sharedMaterial = isHit ? glowMat : offMat;
      //glowMat.SetColor("_TintColor", new Color(0.3f, 0.3f, 0.3f));
      //offMat.SetColor("_Color", offColor);
    }
  }

  public void Setup(int IDx, int IDy, bool on, Color c) {
    singleID = false;

    if (_componentInterface == null) {
      if (transform.parent.GetComponent<componentInterface>() != null) _componentInterface = transform.parent.GetComponent<componentInterface>();
      else _componentInterface = transform.parent.parent.GetComponent<componentInterface>();
    }
    button2DID[0] = IDx;
    button2DID[1] = IDy;
    glowColor = c;
    keyHit(on);
    startToggled = on;
    glowMat.SetColor("_TintColor", glowColor);
  }

  public void setOnAtStart(bool on) {
    keyHit(on);
    startToggled = on;
  }

  public void phantomHit(bool on) {
    hits.Enqueue(on);
  }

  void Update() {
    for (int i = 0; i < hits.Count; i++) {
      bool on = hits.Dequeue();
      isHit = on;
      toggled = on;
      if (on) {
        if (glowMatOnToggle) rend.material = glowMat;
        if (labelRend != null) labelRend.material.SetFloat("_EmissionGain", labelEmission);
      } else {
        if (glowMatOnToggle) rend.material = offMat;
        if (labelRend != null) labelRend.material.SetFloat("_EmissionGain", .1f);

      }
    }
  }

  public bool isHit = false;

  public void keyHit(bool on) {
    isHit = on;
    toggled = on;

    if(manipulatorObjScript != null) manipulatorObjScript.bigHaptic((ushort)3999, 0.015f);

    if (on) {
      if (singleID) _componentInterface.hit(on, buttonID);
      else _componentInterface.hit(on, button2DID[0], button2DID[1]);

      if (glowMatOnToggle) rend.material = glowMat;
      if (labelRend != null) labelRend.material.SetFloat("_EmissionGain", labelEmission);
      selectOverlay.GetComponent<Renderer>().material.SetColor("_TintColor", new Color(1f, 1f, 1f));
    } else {
      if (singleID) _componentInterface.hit(on, buttonID);
      else _componentInterface.hit(on, button2DID[0], button2DID[1]);

      if (glowMatOnToggle) rend.material = offMat;
      if (labelRend != null) labelRend.material.SetFloat("_EmissionGain", .1f);
      selectOverlay.GetComponent<Renderer>().material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f));
    }

  }

  bool toggled = false;

  public override void setState(manipState state) {
    if (curState == manipState.grabbed && state != curState) {
      if (!isToggle) keyHit(false);
      if (!glowMatOnToggle) {
        rend.material = offMat;
      }
    }
    curState = state;
    if (curState == manipState.none) {
      if (!singleID) _componentInterface.onSelect(false, button2DID[0], button2DID[1]);      
      selectOverlay.SetActive(false);
      selectOverlay.GetComponent<Renderer>().material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f));
    } else if (curState == manipState.selected) {
      if (!singleID) _componentInterface.onSelect(true, button2DID[0], button2DID[1]);
      selectOverlay.GetComponent<Renderer>().material.SetColor("_TintColor", new Color(0.7f, 0.7f, 0.7f));
      selectOverlay.SetActive(true);
    } else if (curState == manipState.grabbed) {
      selectOverlay.GetComponent<Renderer>().material.SetColor("_TintColor", new Color(1f, 1f, 1f));
      if (!singleID) _componentInterface.onSelect(true, button2DID[0], button2DID[1]);
      if (isToggle) {
        toggled = !toggled;
        if (toggled) keyHit(true);
        else if (!onlyOn) keyHit(false);
      } else keyHit(true);

      if (!glowMatOnToggle) {
        rend.material = glowMat;
      }
    }
  }

  public override void onTouch(bool on, manipulator m) {
    if (m != null) {
      if (m.emptyGrab) {
        if (!on) {
          if (!isToggle) keyHit(false);
          if (!glowMatOnToggle) {
            rend.material = offMat;
          }
        } else {
          if (isToggle) {
            toggled = !toggled;
            if (toggled) keyHit(true);
            else if (!onlyOn) keyHit(false);
          } else keyHit(true);

          if (!glowMatOnToggle) {
            rend.material = glowMat;
          }
        }
      }
    }
  }
}
