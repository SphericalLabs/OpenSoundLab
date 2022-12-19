// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
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
using System.Runtime.InteropServices;

public class midiCC : signalGenerator {
  public int channel;
  public int ID;

  public Transform percentQuad;
  public Renderer[] glowRends;
  public TextMesh label;

  omniJack jackOut;
  public float curValue = .5f;

  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);

  public override void Awake() {
    base.Awake();
    jackOut = GetComponentInChildren<omniJack>();
    UpdateValue(0);
  }

  public void UpdateJackID(int ID) {
    if (ID != -1) jackOut.ID = ID;
  }

  public int GetJackID() {
    return jackOut.transform.GetInstanceID();
  }

  void Start() {
    glowRends[2].material.SetFloat("_EmissionGain", .3f);
  }

  public void SetAppearance(string s, float h) {
    label.text = s;

    for (int i = 0; i < glowRends.Length; i++) {
      glowRends[i].material.SetColor("_TintColor", Color.HSVToRGB(h, .5f, .7f));
    }
  }

  void Update() {
    if (updateDesired) {
      float val = (curValue + 1) / 2f;
      percentQuad.localScale = new Vector3(val, 1, 1);
      percentQuad.localPosition = new Vector3((1 - val) / 2f, 0, 0);
      updateDesired = false;
    }
  }

  bool updateDesired = false;
  public void UpdateValue(int b) {
    updateDesired = true;
    curValue = (b / 127f * 2) - 1;
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    SetArrayToSingleValue(buffer, buffer.Length, curValue);
  }
}
