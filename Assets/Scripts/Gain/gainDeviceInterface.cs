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
