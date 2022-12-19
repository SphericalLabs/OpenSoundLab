// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
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
