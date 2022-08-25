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

public class filterDeviceInterface : deviceInterface {

  int ID = 0;
  public omniJack input, controlInput, output;
  public dial frequencyDial, resonanceDial, modeDial, bandwidthDial;

  filterSignalGenerator filter;

  float freqPercent, resPercent, modePercent = -1f; 
  //public float bandwidthPercent = 0f;


  public override void Awake() {
    base.Awake();
    filter = GetComponent<filterSignalGenerator>();
  }

  void Update()
  {
    

    if (filter.incoming != input.signal)
    {
      filter.incoming = input.signal;
    }

    if (filter.freqIncoming != controlInput.signal)
    {
      filter.freqIncoming = controlInput.signal;
    }

    if (freqPercent != frequencyDial.percent) updateFrequency();
    if (resPercent != resonanceDial.percent) updateResonance();
    if (modePercent != modeDial.percent) updateMode();


    //filter.bandWidthHalfed = bandwidthPercent = Utils.map(bandwidthDial.percent, 0f, 1f, 0f, 0.4f) / 2f; // up to 4 octaves
    //filter.bandWidthHalfed = Mathf.Pow(bandwidthPercent, 3f) / 2f;
  }

  void updateFrequency(){
    freqPercent = frequencyDial.percent;
    filter.cutoffFrequency = Utils.map(frequencyDial.percent, 0f, 1f, -0.5f, 0.5f); // 13 octaves around C4
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

    data.resonance = resonanceDial.percent;
    data.frequency = frequencyDial.percent;
    data.filterMode = modeDial.percent;

    return data;
  }

  public override void Load(InstrumentData d) {
    FilterData data = d as FilterData;
    base.Load(data);

    ID = data.ID;
    input.ID = data.jackInID;
    output.ID = data.jackOutID;
    controlInput.ID = data.jackControlInID;

    resonanceDial.setPercent(data.resonance);
    frequencyDial.setPercent(data.frequency);
    modeDial.setPercent(data.filterMode);
  }


}


public class FilterData : InstrumentData {
  public float resonance, frequency; // width is for BP
  //public int filterMode; // 0 = LP, 1 == BP, 2 = HP, 4 = NO(TCH)
  //public filterSignalGenerator.filterType filterMode; // possible?
  public float filterMode;
  public int jackOutID;
  public int jackInID;
  public int jackControlInID;
}
