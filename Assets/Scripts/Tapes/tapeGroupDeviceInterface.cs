// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
// 
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at 
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
// 
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
// 
//
// Copyright © 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright © 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright © 2017 Apache 2.0 Google LLC SoundStage VR
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class tapeGroupDeviceInterface : deviceInterface {
  public GameObject tapePrefab;
  public Transform tapeHolder;
  public TextMesh label;
  int columns = 6;

  string samplegroup = "";

  Vector2 offset = new Vector2(0, -.03f);
  public void Setup(string s) {
    int count = 0;
    label.text = samplegroup = s;
    foreach (KeyValuePair<string, string> entry in sampleManager.instance.sampleDictionary[s]) {
      GameObject g = Instantiate(tapePrefab, Vector3.zero, Quaternion.identity) as GameObject;
      g.transform.parent = tapeHolder.transform;
      g.transform.localRotation = Quaternion.Euler(-90, 0, 0);
      int xMult = count % columns;
      int yMult = count / columns;
      g.transform.localPosition = new Vector3(offset.x - .115f * xMult, offset.y + -yMult * .035f, 0.02f);
      g.GetComponent<tape>().Setup(entry.Key, entry.Value);
      count++;
    }
  }

  public override InstrumentData GetData() {
    TapeGroupData data = new TapeGroupData();
    data.deviceType = DeviceType.TapeGroup;
    GetTransformData(data);

    data.samplegroup = samplegroup;

    return data;
  }

  public override void Load(InstrumentData d) {
    TapeGroupData data = d as TapeGroupData;
    base.Load(data);

    Setup(data.samplegroup);
  }
}
public class TapeGroupData : InstrumentData {
  public string samplegroup;
}