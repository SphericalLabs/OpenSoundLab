// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of OpenSoundLab.
//
// OpenSoundLab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenSoundLab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenSoundLab.  If not, see <http://www.gnu.org/licenses/>.

// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of OpenSoundLab.
//
// OpenSoundLab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenSoundLab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenSoundLab.  If not, see <http://www.gnu.org/licenses/>.

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
    data.deviceType = menuItem.deviceType.ADSR;
    GetTransformData(data);

    data.ADSRdata = new Vector2[3];
    for (int i = 0; i < 3; i++) {
      data.ADSRdata[i] = _adsrInterface.xyHandles[i].percent;
    }

    data.jackOutID = output.transform.GetInstanceID();
    data.jackInID = input.transform.GetInstanceID();

    return data;
  }

  public override void Load(InstrumentData d) {
    ADSRData data = d as ADSRData;
    base.Load(data);

    output.ID = data.jackOutID;
    input.ID = data.jackInID;

    for (int i = 0; i < 3; i++) _adsrInterface.xyHandles[i].setPercent(data.ADSRdata[i]);
    _adsrInterface.setDefaults = false;

  }

}

public class ADSRData : InstrumentData {
  public Vector2[] ADSRdata;
  public int jackInID;
  public int jackOutID;
}
