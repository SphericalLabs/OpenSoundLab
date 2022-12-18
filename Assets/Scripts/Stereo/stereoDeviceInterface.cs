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

using UnityEngine;
using System.Collections;

public class stereoDeviceInterface : deviceInterface {
  public omniJack input, outputL, outputR;
  dial panDial;
  stereoSignalGenerator signal;
  basicSwitch activeSwitch;

  public override void Awake() {
    base.Awake();
    panDial = GetComponentInChildren<dial>();
    signal = GetComponent<stereoSignalGenerator>();
  }

  void Update() {
    
    //signal.amp = 1f + panDial.percent * 32; // 30db! 6 db per duplication, sqrt(32) * 6 = 5 * 6 = 30
    
    //signal.active = activeSwitch.switchVal;
    if (signal.incoming != input.signal) signal.incoming = input.signal;
  }

  public override InstrumentData GetData() {
    StereoData data = new StereoData();
    data.deviceType = menuItem.deviceType.Stereo;
    GetTransformData(data);

    data.dialState = panDial.percent;
    data.switchState = activeSwitch.switchVal;

    data.jackInID = input.transform.GetInstanceID();
    data.jackOutLID = outputL.transform.GetInstanceID();
    data.jackOutRID = outputR.transform.GetInstanceID();

    return data;
  }

  public override void Load(InstrumentData d) {
    StereoData data = d as StereoData;
    base.Load(data);

    input.ID = data.jackInID;
    outputL.ID = data.jackOutLID;
    outputR.ID = data.jackOutRID;

    panDial.setPercent(data.dialState);
    activeSwitch.setSwitch(data.switchState);
  }
}

public class StereoData : InstrumentData {
  public float dialState;
  public bool switchState;
  public int jackOutLID;
  public int jackOutRID;
  public int jackInID;
  
}
