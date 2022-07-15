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

public class trashcan : MonoBehaviour {
  public bool ready = false;
  Material mat;
  menuManager manager;

  public AudioClip trashAct, trashOn, trashOff;

  Color offColor = new Color(.6f, .6f, .6f);
  Color onColor = new Color(1, 1, 1);

  void Awake() {
    mat = transform.GetChild(0).GetComponent<Renderer>().material;
    manager = transform.parent.parent.GetComponent<menuManager>();
    //offColor = new Color(.85f, .85f, .85f);
    mat.SetColor("_TintColor", offColor);
    mat.SetFloat("_EmissionGain", .2f);
  }

  public void trashEvent() {
    //manager.GetComponent<AudioSource>().PlayOneShot(trashAct, .5f);
    StartCoroutine(flash());
  }

  IEnumerator flash() {
    float t = 0;
    mat.SetFloat("_EmissionGain", .6f);
    while (t < 1) {
      t = Mathf.Clamp01(t + Time.deltaTime * 2);
      mat.SetFloat("_EmissionGain", Mathf.Lerp(.6f, .2f, t));
      mat.SetColor("_TintColor", Color.Lerp(onColor, offColor, t));
      yield return null;
    }
  }

  public void setReady(bool on) {
    if (on) {
      mat.SetColor("_TintColor", onColor);
      //manager.GetComponent<AudioSource>().PlayOneShot(trashOn, .15f);
    } else {
      //manager.GetComponent<AudioSource>().PlayOneShot(trashOff, .15f);
      mat.SetColor("_TintColor", offColor);
    }
  }
}
