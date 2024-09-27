// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
// 
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at 
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
// 
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
// 
//
// Copyright © 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright © 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright © 2017 Apache 2.0 Google LLC SoundStage VR
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class midiCCout : signalGenerator {
  public int ID;

  signalGenerator incoming;
  public omniJack input;

  public Transform percentQuad;
  public Renderer[] glowRends;
  public TextMesh label;

  public float curValue = 0f;
  bool updateDesired = true;

  public bool ccMessageDesired = false;

  float hue = 0;

  public override void Awake() {
    base.Awake();
    input = GetComponentInChildren<omniJack>();
  }

  void Start() {
    glowRends[2].material.SetFloat("_EmissionGain", .3f);
  }

  public void SetAppearance(string s, float h) {
    label.text = s;
    hue = h;
    for (int i = 0; i < glowRends.Length; i++) {
      glowRends[i].material.SetColor("_TintColor", Color.HSVToRGB(hue, .5f, .7f));
    }
  }

  void Update() {
    if (input.signal != incoming) incoming = input.signal;

    if (updateDesired) {
      float val = (curValue + 1) / 2f;
      percentQuad.localScale = new Vector3(val, 1, 1);
      percentQuad.localPosition = new Vector3((1 - val) / 2f, 0, 0);
      updateDesired = false;
    }
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    if (incoming == null) return;
    incoming.processBuffer(buffer, dspTime, channels);
    if (curValue != buffer[buffer.Length - 1]) {
      curValue = buffer[buffer.Length - 1];
      updateDesired = true;
      ccMessageDesired = true;
    }
  }
}
