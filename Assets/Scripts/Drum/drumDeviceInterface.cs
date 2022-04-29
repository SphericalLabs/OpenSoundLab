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
using System.Collections.Generic;

public class drumDeviceInterface : deviceInterface {
  int ID = -1;
  drumSignalGenerator drumSig;
  drumstick[] sticks;
  public omniJack sigOut, sampOut;
  drumpad pad;
  samplerLoad samp;
  public AudioSource defaultAudioSource;
  public AudioClip offClip;

  public override void Awake() {
    base.Awake();
    drumSig = GetComponent<drumSignalGenerator>();
    pad = GetComponentInChildren<drumpad>();
    sticks = GetComponentsInChildren<drumstick>();
    samp = GetComponentInChildren<samplerLoad>();
  }

  void Start() {
    masterControl.instance.newDrum(this);
  }

  void OnDestroy() {
    for (int i = 0; i < sticks.Length; i++) {
      Destroy(sticks[i].gameObject);
    }
  }

  public void displayDrumsticks(bool on) {
    for (int i = 0; i < 2; i++) {
      sticks[i].revealSelf(on);
    }
  }

  public override InstrumentData GetData() {
    DrumData data = new DrumData();
    data.deviceType = menuItem.deviceType.Drum;
    GetTransformData(data);
    data.jackOutSampID = sampOut.transform.GetInstanceID();
    data.jackOutSigID = sigOut.transform.GetInstanceID();
    samp.getTapeInfo(out data.tapeLabel, out data.tapeFile);
    return data;
  }

  public override void Load(InstrumentData d) {
    DrumData data = d as DrumData;
    base.Load(data);

    ID = data.ID;
    sampOut.ID = data.jackOutSampID;
    sigOut.ID = data.jackOutSigID;
    samp.SetSample(data.tapeLabel, data.tapeFile);
  }

  public override void hit(bool on, int ID = -1) {
    if (on) {
      if (sigOut.near == null && sampOut.near == null && !samp.hasTape()) {
        defaultAudioSource.PlayOneShot(offClip, .4f);
      }
    }
    drumSig.setKeyActive(on, ID);
  }
}
public class DrumData : InstrumentData {
  public int jackOutSampID;
  public int jackOutSigID;
  public string tapeLabel, tapeFile;
}