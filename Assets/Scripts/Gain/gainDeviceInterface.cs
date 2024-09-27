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

public class gainDeviceInterface : deviceInterface {
  public omniJack input, output;
  dial ampDial;
  gainSignalGenerator signal;
  basicSwitch activeSwitch;
  float lastAmpValue = -1f;

  public override void Awake() {
    base.Awake();
    ampDial = GetComponentInChildren<dial>();
    activeSwitch = GetComponentInChildren<basicSwitch>();
    signal = GetComponent<gainSignalGenerator>();
  }

  void Update() {

    if (ampDial.percent != lastAmpValue)
    {
      signal.amp = Mathf.Pow(2, ampDial.percent * 6); // 36db gain, 6 db per duplication, log(36) * 6 = 6 * 6 = 36
      lastAmpValue = ampDial.percent;
    }

    signal.active = activeSwitch.switchVal;
    if (signal.incoming != input.signal) signal.incoming = input.signal;
  }

  public override InstrumentData GetData() {
    GainData data = new GainData();
    data.deviceType = DeviceType.Gain;
    GetTransformData(data);

    data.dialState = ampDial.percent;
    data.switchState = activeSwitch.switchVal;

    data.jackInID = input.transform.GetInstanceID();
    data.jackOutID = output.transform.GetInstanceID();

    return data;
  }

  public override void Load(InstrumentData d) {
    GainData data = d as GainData;
    base.Load(data);

    input.ID = data.jackInID;
    output.ID = data.jackOutID;

    ampDial.setPercent(data.dialState);
    activeSwitch.setSwitch(data.switchState);
  }
}

public class GainData : InstrumentData {
  public float dialState;
  public bool switchState;
  public int jackOutID;
  public int jackInID;
  public int jackControlID;
}
