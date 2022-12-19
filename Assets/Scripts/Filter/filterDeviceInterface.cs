// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
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
