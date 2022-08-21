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
