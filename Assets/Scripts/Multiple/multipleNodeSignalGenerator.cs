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

public class multipleNodeSignalGenerator : signalGenerator {
  public Renderer symbolA, symbolB;

  bool flow = true;
  public signalGenerator mainSig;
  public omniJack jack;
  signalGenerator sig;

  public Material mixerMaterial;
  public Material splitterMaterial;

  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);

  public override void Awake() {
    sig = mainSig;
    jack = GetComponentInChildren<omniJack>();

    symbolA.sharedMaterial = mixerMaterial;
    symbolB.sharedMaterial = mixerMaterial;
  }

  public void setup(signalGenerator s, bool f) {
    sig = mainSig = s;
    setFlow(f);
  }

  public void setFlow(bool on) {
    flow = on;
    if (flow) {
      symbolA.transform.localPosition = new Vector3(.00075f, -.0016f, .0217f);
      symbolA.transform.localRotation = Quaternion.Euler(0, 180, 0);
      symbolA.sharedMaterial = mixerMaterial;

      symbolB.transform.localPosition = new Vector3(.00075f, -.0016f, -.0217f);
      symbolB.transform.localRotation = Quaternion.Euler(0, 180, 0);      
      symbolB.sharedMaterial = mixerMaterial;
    } else {
      symbolA.transform.localPosition = new Vector3(.0025f, .0012f, .0217f);
      symbolA.transform.localRotation = Quaternion.Euler(0, 0, 90);
      symbolA.sharedMaterial = splitterMaterial;
      
      symbolB.transform.localPosition = new Vector3(.0025f, .0012f, -.0217f);
      symbolB.transform.localRotation = Quaternion.Euler(0, 0, 90);
      symbolB.sharedMaterial = splitterMaterial;
    }

    if (jack.near != null) {
      jack.near.Destruct();
      jack.signal = null;
    }
    jack.outgoing = flow;

    if (flow) sig = mainSig;
    else sig = jack.signal;
  }

  void Update() {
    if (flow) return;

    if (sig != jack.signal) {
      sig = jack.signal;
    }
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    if (sig == null) {
      SetArrayToSingleValue(buffer, buffer.Length, 0.0f);
      return;
    }

    sig.processBuffer(buffer, dspTime, channels);
  }
}