// Copyright 2017-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
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

public class SampleHoldDeviceInterface : deviceInterface {
  public omniJack input, output, trigInput;
  
  SampleHoldSignalGenerator signal;

  public override void Awake() {
    base.Awake();
    signal = GetComponent<SampleHoldSignalGenerator>();
  }

  void Update() {
    if (signal.incoming != input.signal) signal.incoming = input.signal;
    if (signal.trigSig != trigInput.signal) signal.trigSig = trigInput.signal;
  }

  public override InstrumentData GetData() {
    SampleHoldData data = new SampleHoldData();
    data.deviceType = menuItem.deviceType.SampleHold;
    GetTransformData(data);

    data.jackInID = input.transform.GetInstanceID();
    data.jackOutID = output.transform.GetInstanceID();
    data.jackTriggerID = trigInput.transform.GetInstanceID();

    return data;
  }

  public override void Load(InstrumentData d) {
    SampleHoldData data = d as SampleHoldData;
    base.Load(data);

    input.ID = data.jackInID;
    output.ID = data.jackOutID;
    trigInput.ID = data.jackTriggerID;

  }
}

public class SampleHoldData : InstrumentData {
  public float dialState;

  public int jackOutID;
  public int jackInID;
  public int jackTriggerID;
}
