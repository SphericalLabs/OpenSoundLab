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

public class DCDeviceInterface : deviceInterface {
  public omniJack /*input, */output;
  //basicSwitch isBipolar;
  dial attenDial;
  //AudioSource speaker;

  DCSignalGenerator signal;

  public override void Awake() {
    base.Awake();
    //isBipolar = GetComponentInChildren<basicSwitch>();
    attenDial = GetComponentInChildren<dial>();
    //speaker = GetComponentInChildren<AudioSource>();
    signal = GetComponent<DCSignalGenerator>();

    //speaker.volume = signal.incoming == null ? 0f : 1f;
    //attenDial.defaultPercent = isBipolar.switchVal ? 0.5f : 0f;
  }

  void Update() {
    //if(signal.isBipolar != isBipolar.switchVal)
    //{
    //  signal.isBipolar = isBipolar.switchVal;
    //  attenDial.defaultPercent = isBipolar.switchVal ? 0.5f : 0f;
    //}
    
    signal.attenDialValue = attenDial.percent;

    //if (signal.incoming != input.signal)
    //{
    //  signal.incoming = input.signal;
    //  //speaker.volume = signal.incoming == null ? 0f : 1f;
    //}
  }

  public override InstrumentData GetData() {
    DCData data = new DCData();
    data.deviceType = menuItem.deviceType.DC;
    GetTransformData(data);

    //data.isBipolar = isBipolar.switchVal;
    data.dial = attenDial.percent;

    //data.jackInID = input.transform.GetInstanceID();
    data.jackOutID = output.transform.GetInstanceID();

    return data;
  }

  public override void Load(InstrumentData d) {
    DCData data = d as DCData;
    base.Load(data);

    //input.ID = data.jackInID;
    output.ID = data.jackOutID;

    //isBipolar.setSwitch(data.isBipolar, true);
    attenDial.setPercent(data.dial);

  }
}

public class DCData : InstrumentData {
  //public bool isBipolar;
  public float dial;

  public int jackOutID;
  //public int jackInID;
}
