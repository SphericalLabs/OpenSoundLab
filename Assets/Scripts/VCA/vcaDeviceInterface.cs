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

public class vcaDeviceInterface : deviceInterface {
  public omniJack input, output, controlInput;
  dial ampDial;

  vcaSignalGenerator signal;

  public override void Awake() {
    base.Awake();
    ampDial = GetComponentInChildren<dial>();
    signal = GetComponent<vcaSignalGenerator>();
  }

  void Update() {
    signal.amp = ampDial.percent;

    if (signal.incoming != input.signal) signal.incoming = input.signal;
    if (signal.controlSig != controlInput.signal) signal.controlSig = controlInput.signal;
  }

  public override InstrumentData GetData() {
    vcaData data = new vcaData();
    data.deviceType = menuItem.deviceType.VCA;
    GetTransformData(data);

    data.dialState = ampDial.percent;

    data.jackInID = input.transform.GetInstanceID();
    data.jackOutID = output.transform.GetInstanceID();
    data.jackControlID = controlInput.transform.GetInstanceID();

    return data;
  }

  public override void Load(InstrumentData d) {
    vcaData data = d as vcaData;
    base.Load(data);

    input.ID = data.jackInID;
    output.ID = data.jackOutID;
    controlInput.ID = data.jackControlID;

    ampDial.setPercent(data.dialState);

  }
}

public class vcaData : InstrumentData {
  public float dialState;

  public int jackOutID;
  public int jackInID;
  public int jackControlID;
}
