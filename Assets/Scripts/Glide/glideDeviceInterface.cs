// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright � 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
//
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
//
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
//
//
// Copyright � 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright � 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright � 2017 Apache 2.0 Google LLC SoundStage VR
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

public class glideDeviceInterface : deviceInterface
{
    public omniJack input, output;
    dial valueDial;
    glideSignalGenerator signal;

    public dial ValueDial { get => valueDial; set => valueDial = value; }

    public override void Awake()
    {
        base.Awake();
        valueDial = GetComponentInChildren<dial>();
        //    activeSwitch = GetComponentInChildren<basicSwitch>();
        signal = GetComponent<glideSignalGenerator>();
    }

    void Update()
    {

        signal.setTimeFactor(valueDial.percent);

        if (signal.incoming != input.signal) signal.incoming = input.signal;
    }

    public override InstrumentData GetData()
    {
        GlideData data = new GlideData();
        data.deviceType = DeviceType.Glide;
        GetTransformData(data);

        data.dialState = valueDial.percent;
        //    data.switchState = activeSwitch.switchVal;

        data.jackInID = input.transform.GetInstanceID();
        data.jackOutID = output.transform.GetInstanceID();

        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        GlideData data = d as GlideData;
        base.Load(data, copyMode);

        input.SetID(data.jackInID, copyMode);
        output.SetID(data.jackOutID, copyMode);

        valueDial.setPercent(data.dialState);
        //    activeSwitch.setSwitch(data.switchState);
    }
}

public class GlideData : InstrumentData
{
    public float dialState;
    //public bool switchState;
    public int jackOutID;
    public int jackInID;
}
