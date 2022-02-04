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
        data.deviceType = menuItem.deviceType.AD;
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

    public override void Load(InstrumentData d)
    {
        ADData data = d as ADData;
        base.Load(data);

        input.ID = data.jackInID;
        output.ID = data.jackOutID;
        attackInput.ID = data.jackAttackInID;
        releaseInput.ID = data.jackReleaseInID;

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
