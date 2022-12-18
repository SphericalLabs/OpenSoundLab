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

public class vcaDeviceInterface : deviceInterface {
  public omniJack input, output, controlInput;
  dial ampDial;

  vcaSignalGenerator signal;

  public override void Awake() {
    base.Awake();
    ampDial = GetComponentInChildren<dial>();
    signal = GetComponent<vcaSignalGenerator>();
  }

  void Update() {
    signal.amp = ampDial.percent;

    if (signal.incoming != input.signal) signal.incoming = input.signal;
    if (signal.controlSig != controlInput.signal) signal.controlSig = controlInput.signal;
  }

  public override InstrumentData GetData() {
    vcaData data = new vcaData();
    data.deviceType = menuItem.deviceType.VCA;
    GetTransformData(data);

    data.dialState = ampDial.percent;

    data.jackInID = input.transform.GetInstanceID();
    data.jackOutID = output.transform.GetInstanceID();
    data.jackControlID = controlInput.transform.GetInstanceID();

    return data;
  }

  public override void Load(InstrumentData d) {
    vcaData data = d as vcaData;
    base.Load(data);

    input.ID = data.jackInID;
    output.ID = data.jackOutID;
    controlInput.ID = data.jackControlID;

    ampDial.setPercent(data.dialState);

  }
}

public class vcaData : InstrumentData {
  public float dialState;

  public int jackOutID;
  public int jackInID;
  public int jackControlID;
}
