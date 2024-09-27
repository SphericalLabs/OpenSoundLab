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

public class reverbDeviceInterface : deviceInterface {
  reverbSignalGenerator signal;
  dial level;
  slider reverbControl;
  public omniJack input, output;
  int ID = -1;

  public override void Awake() {
    base.Awake();
    signal = GetComponent<reverbSignalGenerator>();
    level = GetComponentInChildren<dial>();
    reverbControl = GetComponentInChildren<slider>();
  }

  void Update() {
    signal.sendLevel = level.percent;
    signal.decayTime = Mathf.Lerp(signal.decayTime, Mathf.Lerp(0, 4, reverbControl.percent), .1f);

    if (input.signal != signal.incoming) signal.incoming = input.signal;
  }

  public override InstrumentData GetData() {
    ReverbData data = new ReverbData();
    data.deviceType = DeviceType.Reverb;
    GetTransformData(data);

    data.dialState = level.percent;
    data.reverbPercent = reverbControl.percent;
    data.jackInID = input.transform.GetInstanceID();
    data.jackOutID = output.transform.GetInstanceID();

    return data;
  }

  public override void Load(InstrumentData d) {
    ReverbData data = d as ReverbData;
    base.Load(data);
    input.ID = data.jackInID;
    output.ID = data.jackOutID;

    level.setPercent(data.dialState);
    reverbControl.setPercent(data.reverbPercent);
  }
}

public class ReverbData : InstrumentData {
  public float dialState;
  public float reverbPercent;
  public int jackOutID;
  public int jackInID;
}