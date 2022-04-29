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
    data.deviceType = menuItem.deviceType.Reverb;
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