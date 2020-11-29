// Copyright 2017 Google LLC
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

  public override void Awake() {
    base.Awake();
    ampDial = GetComponentInChildren<dial>();
    activeSwitch = GetComponentInChildren<basicSwitch>();
    signal = GetComponent<gainSignalGenerator>();
  }

  void Update() {
    
    signal.amp = 1f + ampDial.percent * 32; // 30db! 6 db per duplication, sqrt(32) * 6 = 5 * 6 = 30
    
    signal.active = activeSwitch.switchVal;
    if (signal.incoming != input.signal) signal.incoming = input.signal;
  }

  public override InstrumentData GetData() {
    GainData data = new GainData();
    data.deviceType = menuItem.deviceType.Gain;
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
