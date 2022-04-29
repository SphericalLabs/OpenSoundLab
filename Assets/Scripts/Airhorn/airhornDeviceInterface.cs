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

﻿using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class airhornDeviceInterface : deviceInterface {

  airhornSignalGenerator signal;
  airhornUI _airhornUI;

  public clipPlayerSimple[] samplers;
  public AudioSource defaultAudioSource;
  public AudioClip offClip;
  public omniJack jackOut;

  public override void Awake() {
    base.Awake();
    signal = GetComponent<airhornSignalGenerator>();
    _airhornUI = GetComponentInChildren<airhornUI>();
  }

  void Start() {
    if (alreadyLoaded) return;
    for (int i = 0; i < 4; i++) {
      samplers[i].GetComponent<samplerLoad>().SetSample("Airhorn", "APP" + System.IO.Path.DirectorySeparatorChar + "SFX" + System.IO.Path.DirectorySeparatorChar + "Airhorn.wav");
    }

  }

  public void PlaySample(bool on, int id) {
    signal.curPlayer = samplers[id];
    for (int i = 0; i < 4; i++) {
      if (on && id == i) {
        if (samplers[i].loaded) samplers[i].Play();
        else defaultAudioSource.PlayOneShot(offClip, .4f);
      } else samplers[i].Stop();
    }
  }

  void OnDestroy() {
    if (_airhornUI.transform.parent != transform) Destroy(_airhornUI.gameObject);
  }

  public override InstrumentData GetData() {
    AirhornData data = new AirhornData();
    data.deviceType = menuItem.deviceType.Airhorn;
    GetTransformData(data);
    data.jackOutID = jackOut.transform.GetInstanceID();

    data.samples = new string[4][];
    for (int i = 0; i < 4; i++) {
      data.samples[i] = new string[] { "", "" };
      samplers[i].GetComponent<samplerLoad>().getTapeInfo(out data.samples[i][0], out data.samples[i][1]);
    }
    return data;
  }

  bool alreadyLoaded = false;
  public override void Load(InstrumentData d) {
    AirhornData data = d as AirhornData;
    base.Load(data);
    for (int i = 0; i < 4; i++) {
      if (data.samples[i][0] != "") samplers[i].GetComponent<samplerLoad>().SetSample(data.samples[i][0], data.samples[i][1]);
      else samplers[i].GetComponent<samplerLoad>().ForceEject();
    }

    alreadyLoaded = true;
    jackOut.ID = data.jackOutID;
  }
}

public class AirhornData : InstrumentData {
  public int jackOutID;
  public string[][] samples;
}
