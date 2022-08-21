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

public class speakerDeviceInterface : deviceInterface {
  public int ID = -1;
  public omniJack input;
  speaker output;
  public basicSwitch channelSwitcher;
  public GameObject speakerRim;
  public AudioSource audio;

  SpeakerData data;

  public override void Awake() {
    base.Awake();
    output = GetComponent<speaker>();
    input = GetComponentInChildren<omniJack>();
    speakerRim.GetComponent<Renderer>().material.SetFloat("_EmissionGain", .45f);
    speakerRim.SetActive(false);
  }

  void Start() {
    audio.spatialize = (masterControl.instance.BinauralSetting != masterControl.BinauralMode.None);
  }

  public void Activate(int[] prevIDs) {
    ID = prevIDs[0];
    input.ID = prevIDs[1];
  }

  float lastScale = 0;

  void Update() {
    if (output.incoming != input.signal) {
      output.incoming = input.signal;
      if (output.incoming == null) speakerRim.SetActive(false);
      else speakerRim.SetActive(true);
    }

    if (output.incoming != null) {
      if (lastScale != transform.localScale.x) {
        lastScale = transform.localScale.x;
        output.volume = Mathf.Pow(lastScale + .2f, 2);
      }
    }

    output.leftOn = channelSwitcher.switchVal;

  }

  public override InstrumentData GetData() {
    SpeakerData data = new SpeakerData();
    data.deviceType = menuItem.deviceType.Speaker;
    GetTransformData(data);
    data.jackInID = input.transform.GetInstanceID();
    data.channelState = channelSwitcher.switchVal;
    return data;
  }

  public override void Load(InstrumentData d) {
    SpeakerData data = d as SpeakerData;

    transform.localPosition = data.position;
    transform.localRotation = data.rotation;
    transform.localScale = data.scale;

    ID = data.ID;
    input.ID = data.jackInID;
    channelSwitcher.setSwitch(data.channelState, true);

  }
}

public class SpeakerData : InstrumentData {
  public int jackInID;
  public bool channelState;
}