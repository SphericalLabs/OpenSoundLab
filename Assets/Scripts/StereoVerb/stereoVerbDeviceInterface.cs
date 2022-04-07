// Copyright 2017-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
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
    public dial size, damping, width, mix;
    public basicSwitch freeze;
    public omniJack omniJackIn, omniJackOut, omniJackModSize, omniJackModFreeze, omniJackModMix;
    public AudioSource speaker;

    private dial[] dials;

    public override void Awake() {
        base.Awake();
        signal = GetComponent<stereoVerbSignalGenerator>();
        dials = new dial[3] { size, damping, width };
    }

    void Update() {
        if (omniJackIn.signal != signal.sigIn) signal.sigIn = omniJackIn.signal;
        if (omniJackModSize.signal != signal.sigModSize) signal.sigModSize = omniJackModSize.signal;
        if (omniJackModFreeze.signal != signal.sigModFreeze) signal.sigModFreeze = omniJackModFreeze.signal;
        if (omniJackModMix.signal != signal.sigModMix) signal.sigModMix = omniJackModMix.signal;

        signal.SetParam(size.percent, (int)stereoVerbSignalGenerator.Param.P_ROOMSIZE);
        signal.SetParam(damping.percent, (int)stereoVerbSignalGenerator.Param.P_DAMPING);
        signal.SetParam(width.percent, (int)stereoVerbSignalGenerator.Param.P_WIDTH);
        signal.SetParam(freeze.switchVal ? 1 : 0, (int)stereoVerbSignalGenerator.Param.P_FREEZE);
        signal.SetParam(Utils.equalPowerCrossfadeGain(mix.percent), (int)stereoVerbSignalGenerator.Param.P_WET);
        signal.SetParam(Utils.equalPowerCrossfadeGain(1 - mix.percent), (int)stereoVerbSignalGenerator.Param.P_DRY);
    }

    public override InstrumentData GetData() {
        StereoVerbData data = new StereoVerbData();
        data.deviceType = menuItem.deviceType.StereoVerb;
        GetTransformData(data);

        data.jackInID = omniJackIn.transform.GetInstanceID();
        data.jackOutID = omniJackOut.transform.GetInstanceID();
        data.cSizeID = omniJackModSize.transform.GetInstanceID();
        data.cFreezeID = omniJackModFreeze.transform.GetInstanceID();
        data.cMixID = omniJackModMix.transform.GetInstanceID();

        data.size = size.percent;
        data.damping = damping.percent;
        data.mix = mix.percent;
        data.width = width.percent;
        data.freeze = freeze.switchVal;

        return data;
    }

    public override void Load(InstrumentData d) {
        StereoVerbData data = d as StereoVerbData;
        base.Load(data);
        omniJackIn.ID = data.jackInID;
        omniJackOut.ID = data.jackOutID;
        omniJackModSize.ID = data.cSizeID;
        omniJackModFreeze.ID = data.cFreezeID;
        omniJackModMix.ID = data.cMixID;

        size.setPercent(data.size);
        damping.setPercent(data.size);
        mix.setPercent(data.mix);
        width.setPercent(data.width);
        freeze.setSwitch(data.freeze, true);
    }
}

public class StereoVerbData : InstrumentData {
    public float size, damping, mix, width;
    public bool freeze;
    public int jackOutID, jackInID;
    public int cSizeID, cFreezeID, cMixID;
}