// Copyright 2017 Google LLC
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

  public Texture flowTex;
  public Renderer symbolquad;

  int count = 0;

  bool flow = true;
  Color flowMixerColor = new Color(1f, 0f, 0f);
  Color flowSplitterColor = new Color(0f, 1f, 0f);

  public override void Awake() {
    signal = GetComponent<multipleSignalGenerator>();
    flowSwitch = GetComponentInChildren<basicSwitch>();
    signal.nodes = new List<multipleNodeSignalGenerator>();

    symbolquad.material.SetTexture("_MainTex", flowTex);
    symbolquad.material.SetColor("_TintColor", flowMixerColor);

    float xVal = stretchSlider.localPosition.x;

    count = Mathf.FloorToInt((xVal - .02f) / -.04f) - 1;
    updateSplitterCount();
  }

  void updateSplitterCount() {
    int cur = signal.nodes.Count;
    if (count > cur) {
      for (int i = 0; i < count - cur; i++) {
        multipleNodeSignalGenerator s = (Instantiate(splitterNodePrefab, transform, false) as GameObject).GetComponent<multipleNodeSignalGenerator>();
        s.setup(signal, flow);
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

  void setFlow(bool on) {
    if (flow == on) return;
    flow = on;

    if (flow) {
      symbolquad.transform.localPosition = new Vector3(.0025f, .0012f, .0217f);
      symbolquad.transform.localRotation = Quaternion.Euler(0, 180, 0);
      symbolquad.material.SetColor("_TintColor", flowMixerColor);
    } else {
      symbolquad.transform.localPosition = new Vector3(.00075f, -.0016f, .0217f);
      symbolquad.transform.localRotation = Quaternion.Euler(0, 0, 90);
      symbolquad.material.SetColor("_TintColor", flowSplitterColor);
    }

    if (input.near != null) {
      input.near.Destruct();
      input.signal = null;
    }
    if (output.near != null) {
      output.near.Destruct();
      output.signal = null;
    }

    input.outgoing = !flow;
    output.outgoing = flow;

    for (int i = 0; i < signal.nodes.Count; i++) signal.nodes[i].setFlow(flow);

    signal.setFlow(flow);
  }

  void Update() {

    float xVal = stretchSlider.localPosition.x;
    count = Mathf.FloorToInt((xVal - .02f) / -.04f) - 1;
    if (count != signal.nodes.Count) updateSplitterCount();

    if (flow) {
      if (signal.incoming != input.signal) signal.incoming = input.signal;
    } else if (signal.incoming != output.signal) signal.incoming = output.signal;


    if (flowSwitch.switchVal != flow) {
      setFlow(flowSwitch.switchVal);
    }
  }

  public override InstrumentData GetData() {
    MultipleData data = new MultipleData();
    data.deviceType = menuItem.deviceType.Multiple;
    GetTransformData(data);

    data.flowDir = flow;
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

    setFlow(data.flowDir);
    flowSwitch.setSwitch(flow);

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
  public bool flowDir;
  public int jackOutAID;
  public int jackOutBID;
  public int jackCount;
  public int[] jackOutID;
  public int jackInID;
}