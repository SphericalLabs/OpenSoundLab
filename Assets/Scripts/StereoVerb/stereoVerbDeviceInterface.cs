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

public class stereoVerbDeviceInterface : deviceInterface {
    stereoVerbSignalGenerator signal;
    public dial size, damping, width;
    public basicSwitch freeze;
    public slider dryWet;
    public omniJack input, output;
    public AudioSource speaker;

    private dial[] dials;

    public override void Awake() {
        base.Awake();
        signal = GetComponent<stereoVerbSignalGenerator>();
        dials = new dial[3] { size, damping, width };
    }

    void Update() {
        if (input.signal != signal.incoming) signal.incoming = input.signal;

        signal.SetParam(size.percent, (int)stereoVerbSignalGenerator.Param.P_ROOMSIZE);
        signal.SetParam(damping.percent, (int)stereoVerbSignalGenerator.Param.P_DAMPING);
        signal.SetParam(width.percent, (int)stereoVerbSignalGenerator.Param.P_WIDTH);
        signal.SetParam(freeze.switchVal ? 1 : 0, (int)stereoVerbSignalGenerator.Param.P_FREEZE);
        signal.SetParam(dryWet.percent, (int)stereoVerbSignalGenerator.Param.P_WET);
        signal.SetParam(1 - dryWet.percent, (int)stereoVerbSignalGenerator.Param.P_DRY);
    }

    public override InstrumentData GetData() {
        StereoVerbData data = new StereoVerbData();
        data.deviceType = menuItem.deviceType.Reverb;
        GetTransformData(data);

        data.jackInID = input.transform.GetInstanceID();
        data.jackOutID = output.transform.GetInstanceID();

        data.size = size.percent;
        data.damping = damping.percent;
        data.dryWet = dryWet.percent;
        data.width = width.percent;
        data.freeze = freeze.switchVal;

        return data;
    }

    public override void Load(InstrumentData d) {
        StereoVerbData data = d as StereoVerbData;
        base.Load(data);
        input.ID = data.jackInID;
        output.ID = data.jackOutID;

        size.setPercent(data.size);
        damping.setPercent(data.size);
        dryWet.setPercent(data.dryWet);
        width.setPercent(data.width);
        freeze.switchVal = data.freeze;
    }
}

public class StereoVerbData : InstrumentData {
    public float size, damping, dryWet, width;
    public bool freeze;
    public int jackOutID, jackInID;
}