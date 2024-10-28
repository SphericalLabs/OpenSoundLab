// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright � 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
// 
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at 
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
// 
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
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
    data.deviceType = DeviceType.Drum;
    GetTransformData(data);
    data.jackOutSampID = sampOut.transform.GetInstanceID();
    data.jackOutSigID = sigOut.transform.GetInstanceID();
    samp.getTapeInfo(out data.tapeLabel, out data.tapeFile);
    return data;
  }

  public override void Load(InstrumentData d, bool copyMode) {
    DrumData data = d as DrumData;
    base.Load(data, copyMode);

    ID = data.ID;
    sampOut.SetID(data.jackOutSampID, copyMode);
    sigOut.SetID(data.jackOutSigID, copyMode);
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