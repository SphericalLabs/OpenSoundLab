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
