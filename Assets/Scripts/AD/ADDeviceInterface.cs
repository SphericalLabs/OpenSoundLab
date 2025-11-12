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

public class ADDeviceInterface : deviceInterface
{
    public omniJack input, output, attackInput, releaseInput;
    public dial attackDial, releaseDial, linearityDial;
    ADSignalGenerator signal;

    public override void Awake()
    {
        base.Awake();
        signal = GetComponent<ADSignalGenerator>();
    }

    void Update()
    {
        if (signal.incoming != input.signal) signal.incoming = input.signal;
        if (signal.attackInput != attackInput.signal) signal.attackInput = attackInput.signal;
        if (signal.releaseInput != releaseInput.signal) signal.releaseInput = releaseInput.signal;

        signal.setAttack(Mathf.Pow(attackDial.percent, 3));
        signal.setRelease(Mathf.Pow(releaseDial.percent, 3));
        signal.setLinearity(linearityDial.percent);
    }

    public override InstrumentData GetData()
    {
        ADData data = new ADData();
        data.deviceType = DeviceType.AD;
        GetTransformData(data);

        data.attackState = attackDial.percent;
        data.releaseState = releaseDial.percent;
        data.linearityState = linearityDial.percent;

        data.jackInID = input.transform.GetInstanceID();
        data.jackOutID = output.transform.GetInstanceID();

        data.jackAttackInID = attackInput.transform.GetInstanceID();
        data.jackReleaseInID = releaseInput.transform.GetInstanceID();

        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        ADData data = d as ADData;
        base.Load(data, copyMode);

        input.SetID(data.jackInID, copyMode);
        output.SetID(data.jackOutID, copyMode);
        attackInput.SetID(data.jackAttackInID, copyMode);
        releaseInput.SetID(data.jackReleaseInID, copyMode);

        attackDial.setPercent(data.attackState);
        releaseDial.setPercent(data.releaseState);
        linearityDial.setPercent(data.linearityState);
    }
}

public class ADData : InstrumentData
{
    public float attackState;
    public float releaseState;
    public float linearityState;

    public int jackOutID;
    public int jackInID;
    public int jackAttackInID;
    public int jackReleaseInID;

}
