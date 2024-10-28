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

public class quantizerDeviceInterface : deviceInterface {
  public omniJack input, output;
  public dial transposeDial;
  public dial octaveDial;
  public dial scaleDial; 
  quantizerSignalGenerator signal;
  

  public override void Awake() {
    base.Awake();    
    signal = GetComponent<quantizerSignalGenerator>();
  }

  void Update() {

    signal.selectedScale = Mathf.RoundToInt(scaleDial.percent * (signal.scales.Count - 1));
    signal.transpose = transposeDial.percent * 0.1f * 11f / 12f; // the last setting is not a full octave!
    signal.octave = Utils.map(octaveDial.percent, 0f, 1f, -0.4f, 0.4f); // +/- 4 octaves

    if (signal.incoming != input.signal) signal.incoming = input.signal;
  }

  public override InstrumentData GetData() {
    QuantizerData data = new QuantizerData();
    data.deviceType = DeviceType.Quantizer;
    GetTransformData(data);

    data.transposeState = transposeDial.percent;
    data.scaleState = scaleDial.percent;
    data.octaveState = octaveDial.percent;

    data.jackInID = input.transform.GetInstanceID();
    data.jackOutID = output.transform.GetInstanceID();

    return data;
  }

  public override void Load(InstrumentData d, bool copyMode) {
    QuantizerData data = d as QuantizerData;
    base.Load(data, copyMode);

    input.SetID(data.jackInID, copyMode);
    output.SetID(data.jackOutID, copyMode);

    transposeDial.setPercent(data.transposeState);
    scaleDial.setPercent(data.scaleState);
    octaveDial.setPercent(data.octaveState);
  }
}

public class QuantizerData : InstrumentData {
  public float transposeState;
  public float scaleState;
  public float octaveState;
  public int jackOutID;
  public int jackInID;
  public int jackControlID;
}
