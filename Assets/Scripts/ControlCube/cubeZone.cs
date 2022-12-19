// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright � 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
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
// Copyright � 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright � 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright � 2017 Apache 2.0 Google LLC SoundStage VR
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

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class cubeZone : manipObject {
  ControlCubeDeviceInterface _deviceInterface;
  public Transform[] lines;
  Material mat;

  Color onColor = new Color(0.414f, 0.444f, 0.750f, 0.466f);
  Color offColor = new Color(0, 0, 0, .466f);

  public override void Awake() {
    base.Awake();
    mat = GetComponent<Renderer>().material;
    _deviceInterface = GetComponentInParent<ControlCubeDeviceInterface>();
  }

  Vector3 p;
  public override void grabUpdate(Transform t) {
    Transform tip = t.Find("manipCollViz").transform; 
    p = transform.InverseTransformPoint(tip.position);
    updatePercent(p);
    manipulatorObjScript.hapticPulse((ushort)(750f * (_deviceInterface.percent.x + _deviceInterface.percent.y + _deviceInterface.percent.z) / 3));
  }

  Vector3 percent = Vector3.one;
  void updatePercent(Vector3 p) {
    p.x = Mathf.Clamp01(-p.x + .5f);
    p.y = Mathf.Clamp01(p.y + .5f);
    p.z = Mathf.Clamp01(-p.z + .5f);
    _deviceInterface.updatePercent(p);

    updateLines(p);
  }

  public void updateLines(Vector3 p) {
    p = p * .3f;
    lines[0].localPosition = new Vector3(-p.x + .15f, p.y, 0);
    lines[1].localPosition = new Vector3(-p.x + .15f, .15f, -p.z + .15f);
    lines[2].localPosition = new Vector3(0, p.y, -p.z + .15f);
  }

  void colorChange(bool on) {
    if (on) mat.SetColor("_SpecColor", offColor);
    else mat.SetColor("_SpecColor", onColor);
  }

  public override void setState(manipState state) {
    if (curState == state) return;

    curState = state;

    if (curState == manipState.none) {
      colorChange(false);
    }
    if (curState == manipState.selected) {
      colorChange(true);
    }
    if (curState == manipState.grabbed) {
      colorChange(true);
    }
  }
}