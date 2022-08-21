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
    signal.transpose = Utils.map(transposeDial.percent, 0f, 1f, -1f, 1f);

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
