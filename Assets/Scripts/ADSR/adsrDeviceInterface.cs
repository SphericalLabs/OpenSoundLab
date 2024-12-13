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

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class adsrDeviceInterface : deviceInterface {
  adsrInterface _adsrInterface;
  adsrSignalGenerator adsrSignal;
  public omniJack input, output;

  public override void Awake() {
    base.Awake();
    _adsrInterface = GetComponentInChildren<adsrInterface>();
    adsrSignal = GetComponent<adsrSignalGenerator>();
    adsrSignal._devinterface = this;
    adsrSignal.durations = _adsrInterface.durations;
    adsrSignal.volumes = _adsrInterface.volumes;
  }

  void Update() {
    if (input.signal != adsrSignal.incoming) {
      adsrSignal.incoming = input.signal;
    }

  }


  public override InstrumentData GetData() {
    ADSRData data = new ADSRData();
    data.deviceType = DeviceType.ADSR;
    GetTransformData(data);

    data.ADSRdata = new Vector2[3];
    for (int i = 0; i < 3; i++) {
      data.ADSRdata[i] = _adsrInterface.xyHandles[i].percent;
    }

    data.jackOutID = output.transform.GetInstanceID();
    data.jackInID = input.transform.GetInstanceID();

    return data;
  }

  public override void Load(InstrumentData d, bool copyMode) {
    ADSRData data = d as ADSRData;
    base.Load(data, copyMode);

    output.SetID(data.jackOutID, copyMode);
    input.SetID(data.jackInID, copyMode);

    for (int i = 0; i < 3; i++) _adsrInterface.xyHandles[i].setPercent(data.ADSRdata[i]);
    _adsrInterface.setDefaults = false;

  }

}

public class ADSRData : InstrumentData {
  public Vector2[] ADSRdata;
  public int jackInID;
  public int jackOutID;
}
