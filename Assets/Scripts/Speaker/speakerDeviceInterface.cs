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

using UnityEngine;
using System.Collections;

public class speakerDeviceInterface : deviceInterface {
  public int ID = -1;
  public omniJack input;
  speaker output;
  public basicSwitch channelSwitcher;
  public GameObject speakerRim;
  public AudioSource audio;

  SpeakerData data;

  public override void Awake() {
    base.Awake();
    output = GetComponent<speaker>();
    input = GetComponentInChildren<omniJack>();
    speakerRim.GetComponent<Renderer>().material.SetFloat("_EmissionGain", .45f);
    speakerRim.SetActive(false);
  }

  void Start() {
    audio.spatialize = true;
  }

  public void Activate(int[] prevIDs) {
    ID = prevIDs[0];
    input.ID = prevIDs[1];
  }
    
  void Update() {
    if (output.incoming != input.signal) {
      output.incoming = input.signal;
      if (output.incoming == null) speakerRim.SetActive(false);
      else speakerRim.SetActive(true);
    }

    output.leftOn = channelSwitcher.switchVal;

  }

  public override InstrumentData GetData() {
    SpeakerData data = new SpeakerData();
    data.deviceType = DeviceType.Speaker;
    GetTransformData(data);
    data.jackInID = input.transform.GetInstanceID();
    data.channelState = channelSwitcher.switchVal;
    return data;
  }

  public override void Load(InstrumentData d) {
    SpeakerData data = d as SpeakerData;

    transform.localPosition = data.position;
    transform.localRotation = data.rotation;
    transform.localScale = data.scale;

    ID = data.ID;
    input.ID = data.jackInID;
    channelSwitcher.setSwitch(data.channelState, true);

  }
}

public class SpeakerData : InstrumentData {
  public int jackInID;
  public bool channelState;
}