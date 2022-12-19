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

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class drumDeviceInterface : deviceInterface {
  int ID = -1;
  drumSignalGenerator drumSig;
  drumstick[] sticks;
  public omniJack sigOut, sampOut;
  drumpad pad;
  samplerLoad samp;
  public AudioSource defaultAudioSource;
  //public AudioClip offClip;

  public override void Awake() {
    base.Awake();
    drumSig = GetComponent<drumSignalGenerator>();
    pad = GetComponentInChildren<drumpad>();
    sticks = GetComponentsInChildren<drumstick>();
    samp = GetComponentInChildren<samplerLoad>();
  }

  void Start() {
    masterControl.instance.newDrum(this);
  }

  void OnDestroy() {
    for (int i = 0; i < sticks.Length; i++) {
      Destroy(sticks[i].gameObject);
    }
  }

  public void displayDrumsticks(bool on) {
    for (int i = 0; i < 2; i++) {
      sticks[i].revealSelf(on);
    }
  }

  public override InstrumentData GetData() {
    DrumData data = new DrumData();
    data.deviceType = menuItem.deviceType.Drum;
    GetTransformData(data);
    data.jackOutSampID = sampOut.transform.GetInstanceID();
    data.jackOutSigID = sigOut.transform.GetInstanceID();
    samp.getTapeInfo(out data.tapeLabel, out data.tapeFile);
    return data;
  }

  public override void Load(InstrumentData d) {
    DrumData data = d as DrumData;
    base.Load(data);

    ID = data.ID;
    sampOut.ID = data.jackOutSampID;
    sigOut.ID = data.jackOutSigID;
    samp.SetSample(data.tapeLabel, data.tapeFile);
  }

  public override void hit(bool on, int ID = -1) {
    if (on) {
      if (sigOut.near == null && sampOut.near == null && !samp.hasTape()) {
        //defaultAudioSource.PlayOneShot(offClip, .4f);
      }
    }
    drumSig.setKeyActive(on, ID);
  }
}
public class DrumData : InstrumentData {
  public int jackOutSampID;
  public int jackOutSigID;
  public string tapeLabel, tapeFile;
}