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

public class glideDeviceInterface : deviceInterface {
  public omniJack input, output;
  dial valueDial;
  glideSignalGenerator signal;

  public override void Awake() {
    base.Awake();
    valueDial = GetComponentInChildren<dial>();
//    activeSwitch = GetComponentInChildren<basicSwitch>();
    signal = GetComponent<glideSignalGenerator>();
  }

  void Update() {
    
    signal.time = valueDial.percent; // 0 to 1?
    
//    signal.active = activeSwitch.switchVal;
    if (signal.incoming != input.signal) signal.incoming = input.signal;
  }

  public override InstrumentData GetData() {
    GlideData data = new GlideData();
    data.deviceType = menuItem.deviceType.Glide;
    GetTransformData(data);

    data.dialState = valueDial.percent;
//    data.switchState = activeSwitch.switchVal;

    data.jackInID = input.transform.GetInstanceID();
    data.jackOutID = output.transform.GetInstanceID();

    return data;
  }

  public override void Load(InstrumentData d) {
    GlideData data = d as GlideData;
    base.Load(data);

    input.ID = data.jackInID;
    output.ID = data.jackOutID;

    valueDial.setPercent(data.dialState);
//    activeSwitch.setSwitch(data.switchState);
  }
}

public class GlideData : InstrumentData {
  public float dialState;
  //public bool switchState;
  public int jackOutID;
  public int jackInID;
}
