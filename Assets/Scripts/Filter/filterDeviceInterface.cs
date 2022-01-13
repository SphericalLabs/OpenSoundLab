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

using UnityEngine;
using System.Collections;

public class filterDeviceInterface : deviceInterface {

  int ID = 0;
  public omniJack input, controlInput, output;
  public dial frequencyDial, resonanceDial, modeDial;

  spectrumDisplay spectrum;
  filterSignalGenerator filter;

  float freqPercent, resPercent, modePercent = 0f; 


  public override void Awake() {
    base.Awake();
    filter = GetComponent<filterSignalGenerator>();
    spectrum = GetComponentInChildren<spectrumDisplay>();
  }

  void Update()
  {

    if (filter.incoming != input.signal)
    {
      filter.incoming = input.signal;
      spectrum.toggleActive(filter.incoming != null);
    }

    if (filter.controlIncoming != controlInput.signal)
    {
      filter.controlIncoming = controlInput.signal;
    }

    if (freqPercent != frequencyDial.percent) updateFrequency();
    if (resPercent != resonanceDial.percent) updateResonance();
    if (modePercent != modeDial.percent) updateMode();
  }

  void updateFrequency(){
    freqPercent = frequencyDial.percent;
    filter.cutoffFrequency = Utils.map(frequencyDial.percent, 0f, 1f, -1f, 1f);
  }

  void updateResonance(){
    resPercent = resonanceDial.percent;
    filter.resonance = resonanceDial.percent;
  }
  void updateMode(){
    modePercent = modeDial.percent;
    
    switch(Mathf.RoundToInt(modePercent * 3)){
      case 0:
        filter.curType = filterSignalGenerator.filterType.LP;
        break;
      case 1:
        filter.curType = filterSignalGenerator.filterType.HP;
        break;
      case 2:
        filter.curType = filterSignalGenerator.filterType.BP;
        break;
      case 3:
        filter.curType = filterSignalGenerator.filterType.Notch;
        break;
    }
    
  }

  public override InstrumentData GetData() {
    FilterData data = new FilterData();
    data.deviceType = menuItem.deviceType.Filter;
    GetTransformData(data);

    data.jackInID = input.transform.GetInstanceID();
    data.jackOutID = output.transform.GetInstanceID();
    data.jackControlInID = controlInput.transform.GetInstanceID();

    return data;
  }

  public override void Load(InstrumentData d) {
    FilterData data = d as FilterData;
    base.Load(data);

    ID = data.ID;
    input.ID = data.jackInID;
    output.ID = data.jackOutID;
    controlInput.ID = data.jackControlInID;
  }


}


public class FilterData : InstrumentData {
  public float resonance, frequency, width; // width is for BP
  //public int filterMode; // 0 = LP, 1 == BP, 2 = HP, 4 = NO(TCH)
  public filterSignalGenerator.filterType filterMode; // possible?
  public int jackOutID;
  public int jackInID;
  public int jackControlInID;
}
