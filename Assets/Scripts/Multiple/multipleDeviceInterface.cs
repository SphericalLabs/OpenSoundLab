// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This file is part of OpenSoundLab, which is based on SoundStage VR.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class multipleDeviceInterface : deviceInterface {

  public GameObject splitterNodePrefab;
  public omniJack input, output;
  public Transform stretchSlider, handleA, handleB;
  multipleSignalGenerator signal;
  basicSwitch flowSwitch;

  public Renderer symbolA;
  public Renderer symbolB;

  int count = 0;

  public bool isSplitter = true;

  public Material mixerMaterial;
  public Material splitterMaterial;

  public override void Awake() {
    signal = GetComponent<multipleSignalGenerator>();
    //flowSwitch = GetComponentInChildren<basicSwitch>();
    signal.nodes = new List<multipleNodeSignalGenerator>();

    symbolA.sharedMaterial = mixerMaterial;
    symbolB.sharedMaterial = mixerMaterial;

    float xVal = stretchSlider.localPosition.x;

    count = Mathf.FloorToInt((xVal - .02f) / -.04f) - 1;
    updateSplitterCount();
    setFlow(isSplitter, true); // reads in default value from the prefab to enable MultiMix and MultiSplit
  }

  void updateSplitterCount() {
    int cur = signal.nodes.Count;
    if (count > cur) {
      for (int i = 0; i < count - cur; i++) {
        multipleNodeSignalGenerator s = (Instantiate(splitterNodePrefab, transform, false) as GameObject).GetComponent<multipleNodeSignalGenerator>();
        s.setup(signal, isSplitter);
        signal.nodes.Add(s);
        s.transform.localPosition = new Vector3(-.04f * signal.nodes.Count, 0, 0);
      }
    } else {
      for (int i = 0; i < cur - count; i++) {
        signalGenerator s = signal.nodes.Last();
        signal.nodes.RemoveAt(signal.nodes.Count - 1);
        Destroy(s.gameObject);
      }
    }

    handleA.localPosition = new Vector3(-.02f * signal.nodes.Count, 0, 0);
    handleB.localPosition = new Vector3(-.02f * signal.nodes.Count, 0, 0);

    handleA.localScale = new Vector3(.04f * (signal.nodes.Count + 1), 0.04f, 0.04f);
    handleB.localScale = new Vector3(.04f * (signal.nodes.Count + 1), 0.04f, 0.04f);
  }

  void setFlow(bool on, bool init = false) {
    if (isSplitter == on && !init) return;
    isSplitter = on;

    if (isSplitter) {
      symbolA.transform.localPosition = new Vector3(.0025f, .0012f, .0217f);
      symbolA.transform.localRotation = Quaternion.Euler(0, 180, 0);
      symbolA.sharedMaterial = mixerMaterial;

      symbolB.transform.localPosition = new Vector3(.0025f, .0012f, -.0217f);
      symbolB.transform.localRotation = Quaternion.Euler(0, 180, 0);
      symbolB.sharedMaterial = mixerMaterial;
    } else {
      symbolA.transform.localPosition = new Vector3(.00075f, -.0016f, .0217f);
      symbolA.transform.localRotation = Quaternion.Euler(0, 0, 90);
      symbolA.sharedMaterial = splitterMaterial;

      symbolB.transform.localPosition = new Vector3(.00075f, -.0016f, -.0217f);
      symbolB.transform.localRotation = Quaternion.Euler(0, 0, 90);
      symbolB.sharedMaterial = splitterMaterial;
    }

    if (input.near != null) {
      input.near.Destruct();
      input.signal = null;
    }
    if (output.near != null) {
      output.near.Destruct();
      output.signal = null;
    }

    input.outgoing = !isSplitter;
    output.outgoing = isSplitter;

    for (int i = 0; i < signal.nodes.Count; i++) signal.nodes[i].setFlow(isSplitter);

    signal.setFlow(isSplitter);
  }

  void Update() {

    float xVal = stretchSlider.localPosition.x;
    count = Mathf.FloorToInt((xVal - .02f) / -.04f) - 1;
    if (count != signal.nodes.Count) updateSplitterCount();

    if (isSplitter) {
      if (signal.incoming != input.signal) signal.incoming = input.signal;
    } else if (signal.incoming != output.signal) signal.incoming = output.signal;


    //if (flowSwitch.switchVal != isSplitter) {
    //  setFlow(flowSwitch.switchVal);
    //}
  }

  public override InstrumentData GetData() {
    MultipleData data = new MultipleData();
    data.deviceType = menuItem.deviceType.Multiple;
    GetTransformData(data);

    data.isSplitter = isSplitter;
    data.jackInID = input.transform.GetInstanceID();

    data.jackCount = count + 1;
    data.jackOutID = new int[data.jackCount];

    data.jackOutID[0] = output.transform.GetInstanceID();

    for (int i = 1; i < data.jackCount; i++) {
      data.jackOutID[i] = signal.nodes[i - 1].jack.transform.GetInstanceID();
    }

    return data;
  }

  public override void Load(InstrumentData d) {
    MultipleData data = d as MultipleData;
    base.Load(data);

    input.ID = data.jackInID;

    setFlow(data.isSplitter);
    //flowSwitch.setSwitch(isSplitter);

    if (data.jackCount < 2) {
      count = 1;
      Vector3 pos = stretchSlider.localPosition;
      pos.x = (count + 1) * -.04f;
      stretchSlider.localPosition = pos;
      updateSplitterCount();

      output.ID = data.jackOutAID;
      signal.nodes[0].jack.ID = data.jackOutBID;
    } else {
      count = data.jackCount - 1;
      Vector3 pos = stretchSlider.localPosition;
      pos.x = (count + 1) * -.04f;
      stretchSlider.localPosition = pos;
      updateSplitterCount();

      output.ID = data.jackOutID[0];

      for (int i = 1; i < data.jackCount; i++) {
        signal.nodes[i - 1].jack.ID = data.jackOutID[i];
      }

    }
  }
}

public class MultipleData : InstrumentData {
  public bool isSplitter;
  public int jackOutAID;
  public int jackOutBID;
  public int jackCount;
  public int[] jackOutID;
  public int jackInID;
}