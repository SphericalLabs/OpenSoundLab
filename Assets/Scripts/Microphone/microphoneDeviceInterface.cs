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
using System.Collections.Generic;

public class microphoneDeviceInterface : deviceInterface {

  MicrophoneSignalGenerator signal;
  omniJack output;
  omniPlug outputplug;
  dial ampDial;

  float amp = -1f;

  public override void Awake() {
    base.Awake();
    signal = GetComponent<MicrophoneSignalGenerator>();
    output = GetComponentInChildren<omniJack>();
    ampDial = GetComponentInChildren<dial>();
    microphoneDeviceInterface[] otherMics = FindObjectsOfType<microphoneDeviceInterface>();
    for (int i = 0; i < otherMics.Length; i++) {
      if (otherMics[i] != this) Destroy(otherMics[i].gameObject);
    }
  }

  void Update() {
    if (output.near != outputplug) {
      outputplug = output.near;
      if (outputplug == null) {
        signal.freqBuffers.Clear();
        System.GC.Collect();
      }
    }

    if (amp != ampDial.percent) {
      amp = ampDial.percent;
      signal.amp = amp * 4;
    }
  }

  public override void hit(bool on, int ID = -1) {
    signal.active = on;
  }

  public override InstrumentData GetData() {
    MicrophoneData data = new MicrophoneData();
    data.deviceType = menuItem.deviceType.Microphone;
    GetTransformData(data);
    data.jackOutID = output.transform.GetInstanceID();
    data.amp = amp;
    return data;
  }

  public override void Load(InstrumentData d) {
    MicrophoneData data = d as MicrophoneData;
    base.Load(data);
    output.ID = data.jackOutID;
    ampDial.setPercent(data.amp);
  }
}

public class MicrophoneData : InstrumentData {
  public int jackOutID;
  public float amp;
}