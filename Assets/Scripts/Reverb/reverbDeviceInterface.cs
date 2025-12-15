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

using System.Collections;
using System.Xml.Serialization;
using UnityEngine;

public class reverbDeviceInterface : deviceInterface
{
    reverbSignalGenerator signal;
    public dial size, damping, width, mix;
    public basicSwitch freeze;
    public omniJack omniJackIn, omniJackOut, omniJackModSize, omniJackModFreeze, omniJackModMix;
    public AudioSource speaker;

    private dial[] dials;

    public override void Awake()
    {
        base.Awake();
        signal = GetComponent<reverbSignalGenerator>();
        dials = new dial[3] { size, damping, width };
    }

    void Update()
    {
        if (omniJackIn.signal != signal.sigIn) signal.sigIn = omniJackIn.signal;
        if (omniJackModSize.signal != signal.sigModSize) signal.sigModSize = omniJackModSize.signal;
        if (omniJackModFreeze.signal != signal.sigModFreeze) signal.sigModFreeze = omniJackModFreeze.signal;
        if (omniJackModMix.signal != signal.sigModMix) signal.sigModMix = omniJackModMix.signal;

        signal.SetParam(size.percent, (int)reverbSignalGenerator.Param.P_ROOMSIZE);
        signal.SetParam(damping.percent, (int)reverbSignalGenerator.Param.P_DAMPING);
        signal.SetParam(width.percent, (int)reverbSignalGenerator.Param.P_WIDTH);
        signal.SetParam(freeze.switchVal ? 1 : 0, (int)reverbSignalGenerator.Param.P_FREEZE);
        signal.SetParam(Utils.equalPowerCrossfadeGain(mix.percent), (int)reverbSignalGenerator.Param.P_WET);
        signal.SetParam(Utils.equalPowerCrossfadeGain(1 - mix.percent), (int)reverbSignalGenerator.Param.P_DRY);
    }

    public override InstrumentData GetData()
    {
        ReverbData data = new ReverbData();
        data.deviceType = DeviceType.Reverb;
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

    public override void Load(InstrumentData d, bool copyMode)
    {
        ReverbData data = d as ReverbData;
        base.Load(data, true);
        omniJackIn.SetID(data.jackInID, copyMode);
        omniJackOut.SetID(data.jackOutID, copyMode);
        omniJackModSize.SetID(data.cSizeID, copyMode);
        omniJackModFreeze.SetID(data.cFreezeID, copyMode);
        omniJackModMix.SetID(data.cMixID, copyMode);

        size.setPercent(data.size);
        damping.setPercent(data.damping);
        mix.setPercent(data.mix);
        width.setPercent(data.width);
        freeze.setSwitch(data.freeze, true);
    }
}

[XmlInclude(typeof(FreeverbData))]
public class ReverbData : InstrumentData
{
    public float size, damping, mix, width;
    public bool freeze;
    public int jackOutID, jackInID;
    public int cSizeID, cFreezeID, cMixID;
}

[XmlType("FreeverbData")]
public class FreeverbData : ReverbData
{
}
