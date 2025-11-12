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

public class vcaDeviceInterface : deviceInterface
{
    public omniJack input, output, controlInput;
    dial ampDial;

    vcaSignalGenerator signal;

    public override void Awake()
    {
        base.Awake();
        ampDial = GetComponentInChildren<dial>();
        signal = GetComponent<vcaSignalGenerator>();
    }

    void Update()
    {
        signal.amp = ampDial.percent;

        if (signal.incoming != input.signal) signal.incoming = input.signal;
        if (signal.controlSig != controlInput.signal) signal.controlSig = controlInput.signal;
    }

    public override InstrumentData GetData()
    {
        vcaData data = new vcaData();
        data.deviceType = DeviceType.VCA;
        GetTransformData(data);

        data.dialState = ampDial.percent;

        data.jackInID = input.transform.GetInstanceID();
        data.jackOutID = output.transform.GetInstanceID();
        data.jackControlID = controlInput.transform.GetInstanceID();

        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        vcaData data = d as vcaData;
        base.Load(data, copyMode);

        input.SetID(data.jackInID, copyMode);
        output.SetID(data.jackOutID, copyMode);
        controlInput.SetID(data.jackControlID, copyMode);

        ampDial.setPercent(data.dialState);

    }
}

public class vcaData : InstrumentData
{
    public float dialState;

    public int jackOutID;
    public int jackInID;
    public int jackControlID;
}
