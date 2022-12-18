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

public class touchpadDeviceInterface : deviceInterface {

  touchpadSignalGenerator pad;
  omniJack output;
  basicSwitch modeSelector;
  button touchPad;
  public bool switchedOn = false;
  bool wasToggleInPreviousFrame = false;

  void Update()
  {
    if (!wasToggleInPreviousFrame && modeSelector.switchVal) touchPad.keyHit(false);
    wasToggleInPreviousFrame = modeSelector.switchVal;
  }

  public override void Awake() {
    base.Awake();
    pad = GetComponent<touchpadSignalGenerator>();
    output = GetComponentInChildren<omniJack>();
    modeSelector = GetComponentInChildren<basicSwitch>();
    touchPad = GetComponentInChildren<button>();
  }

  public override void hit(bool on, int ID = -1) {
    if(modeSelector.switchVal){ // momentarily trigger
      touchPad.isToggle = false;
      pad.signalOn = on;
    } else { // permanent switch
      touchPad.isToggle = true;
      //if (on) switchedOn = !switchedOn;
      pad.signalOn = touchPad.isHit;
    }
     
  }

  public override InstrumentData GetData() {
    TouchPadData data = new TouchPadData();
    data.deviceType = menuItem.deviceType.TouchPad;
    GetTransformData(data);

    data.jackOutID = output.transform.GetInstanceID();
    return data;
  }

  public override void Load(InstrumentData d) {
    TouchPadData data = d as TouchPadData;
    base.Load(data);
    output.ID = data.jackOutID;
  }
}

public class TouchPadData : InstrumentData {
  public int jackOutID;
}
