// Copyright 2017-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
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

public class quantizerDeviceInterface : deviceInterface {
  public omniJack input, output;
  public dial transposeDial;
  public dial scaleDial; 
  quantizerSignalGenerator signal;
  

  public override void Awake() {
    base.Awake();    
    signal = GetComponent<quantizerSignalGenerator>();
  }

  void Update() {

    signal.selectedScale = Mathf.RoundToInt(scaleDial.percent * 3);
    signal.transpose = Utils.map(transposeDial.percent, 0f, 1f, -0.3f, 0.3f);

    if (signal.incoming != input.signal) signal.incoming = input.signal;
  }

  public override InstrumentData GetData() {
    QuantizerData data = new QuantizerData();
    data.deviceType = menuItem.deviceType.Quantizer;
    GetTransformData(data);

    data.transposeState = transposeDial.percent;
    data.scaleState = scaleDial.percent;

    data.jackInID = input.transform.GetInstanceID();
    data.jackOutID = output.transform.GetInstanceID();

    return data;
  }

  public override void Load(InstrumentData d) {
    QuantizerData data = d as QuantizerData;
    base.Load(data);

    input.ID = data.jackInID;
    output.ID = data.jackOutID;

    transposeDial.setPercent(data.transposeState);
    scaleDial.setPercent(data.scaleState);
  }
}

public class QuantizerData : InstrumentData {
  public float transposeState;
  public float scaleState;
  public int jackOutID;
  public int jackInID;
  public int jackControlID;
}
