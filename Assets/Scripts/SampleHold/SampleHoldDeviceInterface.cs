// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of SoundStage Lab.
//
// SoundStage Lab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// SoundStage Lab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SoundStage Lab.  If not, see <http://www.gnu.org/licenses/>.

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
