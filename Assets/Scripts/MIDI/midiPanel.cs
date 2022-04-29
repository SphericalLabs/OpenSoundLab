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

public class midiPanel : manipObject {
  public TextMesh label;
  public Renderer outline;

  Material mat;
  Material textMat;
  public componentInterface _componentInterface;
  public int buttonID = -1;

  Color normalColor;

  Vector2 gains = new Vector2(.3f, .45f);

  public override void Awake() {
    base.Awake();
    mat = outline.material;

    normalColor = Color.HSVToRGB(.25f, .7f, .9f);

    textMat = label.GetComponent<Renderer>().material;
    textMat.SetColor("_TintColor", normalColor);
    textMat.SetFloat("_EmissionGain", gains.x);

    mat.SetColor("_TintColor", normalColor);
    mat.SetFloat("_EmissionGain", gains.x);
  }

  void Start() {
    if (transform.parent) _componentInterface = transform.parent.GetComponent<componentInterface>();
  }

  public override void setState(manipState state) {
    if (curState == manipState.grabbed && state != manipState.grabbed) {
      if (_componentInterface != null) _componentInterface.hit(false, buttonID);
    }
    curState = state;
    if (curState == manipState.none) {
      mat.SetColor("_TintColor", normalColor);
      mat.SetFloat("_EmissionGain", gains.x);

      if (textMat != null) {
        textMat.SetColor("_TintColor", normalColor);
        textMat.SetFloat("_EmissionGain", gains.x);
      }
    } else if (curState == manipState.selected) {
      mat.SetColor("_TintColor", normalColor);
      mat.SetFloat("_EmissionGain", gains.y);

      if (textMat != null) {
        textMat.SetColor("_TintColor", normalColor);
        textMat.SetFloat("_EmissionGain", gains.x);
      }
    } else if (curState == manipState.grabbed) {
      mat.SetColor("_TintColor", Color.white);

      if (textMat != null) {
        textMat.SetColor("_TintColor", normalColor);
        textMat.SetFloat("_EmissionGain", gains.y);
      }
      if (_componentInterface != null) _componentInterface.hit(true, buttonID);
    }
  }
}
