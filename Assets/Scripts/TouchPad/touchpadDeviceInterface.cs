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

public class touchpadDeviceInterface : deviceInterface {

  touchpadSignalGenerator pad;
  omniJack output;
  basicSwitch isTrigger;
  button touchPad;
  public bool switchedOn = false;
  bool wasToggleInPreviousFrame = false;

  void Update()
  {
    if (!wasToggleInPreviousFrame && isTrigger.switchVal) touchPad.keyHit(false);
    wasToggleInPreviousFrame = isTrigger.switchVal;
  }

  public override void Awake() {
    base.Awake();
    pad = GetComponent<touchpadSignalGenerator>();
    output = GetComponentInChildren<omniJack>();
    isTrigger = GetComponentInChildren<basicSwitch>();
    touchPad = GetComponentInChildren<button>();
  }

  public override void hit(bool on, int ID = -1) {
    if(isTrigger.switchVal){ // momentarily trigger
      touchPad.isSwitch = false;
      pad.signalOn = on;
    } else { // permanent switch
      touchPad.isSwitch = true;
      //if (on) switchedOn = !switchedOn;
      pad.signalOn = touchPad.isHit;
    }
     
  }

  public override InstrumentData GetData() {
    TouchPadData data = new TouchPadData();
    data.deviceType = DeviceType.TouchPad;
    GetTransformData(data);

    data.jackOutID = output.transform.GetInstanceID();
    data.isTrigger = isTrigger.switchVal;
    return data;
  }

  public override void Load(InstrumentData d, bool copyMode) {
    TouchPadData data = d as TouchPadData;
    base.Load(data, copyMode);
    output.SetID(data.jackOutID, copyMode);
    isTrigger.setSwitch(data.isTrigger);
  }
}

public class TouchPadData : InstrumentData {
  public int jackOutID;
  public bool isTrigger;
}
