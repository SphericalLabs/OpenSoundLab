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

﻿using UnityEngine;
using System.Collections;

public class pauseMenuItem : manipObject {
  public TextMesh label;
  //public Texture2D tex;
  Renderer panelRend;
  public Material panelMat, panelMatSelected;
  Material textMat;
  pauseMenu mainmenu;
  public pauseMenu.itemType itemType;
  public int ID = -1;

  Color normalColor;
  public override void Awake() {
    base.Awake();

    panelRend = transform.Find("Quad").gameObject.GetComponent<Renderer>();
    panelRend.sharedMaterial = panelMat;

    normalColor = Color.HSVToRGB(.6f, .7f, .9f);
    if (itemType == pauseMenu.itemType.confirmItem) normalColor = Color.HSVToRGB(.4f, .7f, .9f);
    else if (itemType == pauseMenu.itemType.cancelItem) normalColor = Color.HSVToRGB(0, .7f, .9f);

    if (label != null) {
      textMat = label.GetComponent<Renderer>().sharedMaterial;
      textMat.SetColor("_TintColor", normalColor);
    }
    if (itemType == pauseMenu.itemType.tooltipItem) {
      if (label != null) {
        label.text = "TOOLTIP\nON";
      }
    }
    if (itemType == pauseMenu.itemType.exampleItem) {
      if (label != null) {
        label.text = "HIDE\nEXAMPLES";
      }
    }
    if (itemType == pauseMenu.itemType.confirmItem || itemType == pauseMenu.itemType.cancelItem) {
      if (label != null) {
        textMat.SetColor("_TintColor", normalColor);
      }
    }

  }

  void Start() {
    mainmenu = transform.parent.parent.GetComponent<pauseMenu>();
    if (mainmenu == null) mainmenu = transform.parent.parent.parent.GetComponent<pauseMenu>();

  }

  void Update() {
    if (itemType == pauseMenu.itemType.tooltipItem && label != null) {
      if (masterControl.instance.tooltipsOn) label.text = "TOOLTIPS\nON";
      else label.text = "TOOLTIPS\nOFF";
    }

    if (itemType == pauseMenu.itemType.exampleItem && label != null) {
      if (masterControl.instance.examplesOn) label.text = "HIDE\nEXAMPLES";
      else label.text = "SHOW\nEXAMPLES";
    }
  }

  public override void setState(manipState state) {
    curState = state;
    if (curState == manipState.none) {
      panelRend.sharedMaterial = panelMat;
      if (textMat != null) {
        textMat.SetColor("_TintColor", normalColor);
        textMat.SetFloat("_EmissionGain", .3f);
      }
    } else if (curState == manipState.selected) {
      panelRend.sharedMaterial = panelMatSelected;
      if (textMat != null) {
        textMat.SetColor("_TintColor", normalColor);
        textMat.SetFloat("_EmissionGain", .3f);
      }
    } else if (curState == manipState.grabbed) {

      if (textMat != null) {
        textMat.SetColor("_TintColor", normalColor);
        textMat.SetFloat("_EmissionGain", .6f);
      }
      mainmenu.itemSelect(itemType, ID);
    }
  }

  IEnumerator appearRoutine(bool on) {
    float timer = 0;
    if (on) {
      Vector3[] sizes = new Vector3[]
      {
                new Vector3(.05f,.05f,.05f),
                new Vector3(1f,.1f,1f),
                new Vector3(1,1,1)
      };

      transform.localScale = sizes[0];
      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 12);
        transform.localScale = Vector3.Lerp(sizes[0], sizes[1], timer);
        yield return null;
      }

      timer = 0;
      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 6);
        transform.localScale = Vector3.Lerp(sizes[1], sizes[2], timer);
        yield return null;
      }

      timer = 0;
      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 10);
        yield return null;
      }
    } else {
      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 6);
        transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, timer);
        yield return null;
      }
    }
  }

  void OnDisable() {
    StopAllCoroutines();
  }

  Coroutine appearCoroutine;
  public void Appear(bool on) {
    if (!gameObject.activeInHierarchy) return;
    if (appearCoroutine != null) StopCoroutine(appearCoroutine);
    appearCoroutine = StartCoroutine(appearRoutine(on));
  }
}