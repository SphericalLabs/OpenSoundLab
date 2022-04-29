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
