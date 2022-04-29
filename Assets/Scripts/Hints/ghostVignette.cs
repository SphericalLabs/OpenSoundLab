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
using System.Collections.Generic;

public class ghostVignette : MonoBehaviour {

  public Transform node, pad, trigger, controller;
  List<Renderer> ghostRends = new List<Renderer>();
  Color ghostColor;

  public Renderer triggerRend, padRend;

  public bool startFade = false;

  public float fadeAmount = 0;
  void Awake() {
    Renderer[] rends = GetComponentsInChildren<Renderer>();

    for (int i = 0; i < rends.Length; i++) {
      if (rends[i].material.name.Contains("ghostController")) {
        ghostRends.Add(rends[i]);
        ghostColor = rends[i].material.GetColor("_TintColor");
      }
    }

    triggerRend = trigger.GetComponent<Renderer>();
    padRend = pad.GetComponent<Renderer>();

    if (startFade) {
      for (int i = 0; i < ghostRends.Count; i++) {
        ghostRends[i].material.SetColor("_TintColor", ghostColor * new Color(1, 1, 1, 0));
      }
    }
  }

  Coroutine _flashRoutine;
  bool renderFlashing = true;
  public void flashRender(Renderer rend, bool on) {
    if (on) {
      if (_flashRoutine != null) StopCoroutine(_flashRoutine);
      _flashRoutine = StartCoroutine(flashRendererRoutine(rend));
    } else renderFlashing = false;
  }

  IEnumerator flashRendererRoutine(Renderer rend) {
    float timer = 0;
    float curVal = .1f;
    renderFlashing = true;

    while (renderFlashing) {
      timer = Mathf.Repeat(timer + Time.deltaTime * 6, Mathf.PI * 2);
      curVal = flashFormula(timer);
      rend.material.SetFloat("_EmissionGain", curVal);
      yield return null;
    }

    timer = 0;
    while (timer < 1) {
      timer = Mathf.Clamp01(timer + Time.deltaTime * 5);
      rend.material.SetFloat("_EmissionGain", Mathf.Lerp(curVal, .1f, timer));
      yield return null;
    }
  }

  float flashFormula(float timer) {
    return .45f + .2f * Mathf.Sin(timer);
  }

  Coroutine _triggerRoutine;
  public void setTrigger(bool on) {
    if (_triggerRoutine != null) StopCoroutine(_triggerRoutine);
    StartCoroutine(triggerRoutine(on));
  }

  IEnumerator triggerRoutine(bool on) {
    Quaternion startRot = trigger.localRotation;
    Quaternion endRot = on ? Quaternion.Euler(45, 180, 0) : Quaternion.Euler(0, 180, 0);
    float timer = 0;

    triggerRend.material.SetFloat("_EmissionGain", .6f);

    while (timer < 1) {
      timer = Mathf.Clamp01(timer + Time.deltaTime * 4);
      trigger.localRotation = Quaternion.Lerp(startRot, endRot, timer);
      yield return null;
    }

    triggerRend.material.SetFloat("_EmissionGain", .1f);
  }

  Coroutine _fadeRoutine;
  public void fade(bool on) {
    if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
    _fadeRoutine = StartCoroutine(fadeRoutine(on));
  }

  IEnumerator fadeRoutine(bool on) {
    float t = 0;
    Color multColor = Color.white;
    while (t < 1) {
      t = Mathf.Clamp01(t + Time.deltaTime / 2f);
      multColor.a = fadeAmount = on ? t : 1 - t;
      for (int i = 0; i < ghostRends.Count; i++) {
        ghostRends[i].material.SetColor("_TintColor", ghostColor * multColor);

      }
      yield return null;
    }

    if (!on) Destroy(gameObject);
  }

  void OnDestroy() {
    StopAllCoroutines();
  }
}
