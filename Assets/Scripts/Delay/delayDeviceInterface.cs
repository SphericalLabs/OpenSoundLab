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
using System;

public class delayDeviceInterface : deviceInterface
{
    public omniJack omniJackIn, omniJackModTime, omniJackModFeedback, omniJackModTrigger, omniJackModMix, omniJackOut;
    public dial timeDial, feedbackDial, mixDial, modeDial;
    public button panicButton;
    delaySignalGenerator signal;

    public override void Awake()
    {
        base.Awake();
        signal = GetComponent<delaySignalGenerator>();
    }

    void Update()
    {
        if (signal.sigIn != omniJackIn.signal) signal.sigIn = omniJackIn.signal;
        if (signal.sigModFeedback != omniJackModFeedback.signal) signal.sigModFeedback = omniJackModFeedback.signal;
        if (signal.sigModTime != omniJackModTime.signal) signal.sigModTime = omniJackModTime.signal;
        if (signal.sigModMix != omniJackModMix.signal) signal.sigModMix = omniJackModMix.signal;
        if (signal.sigModTrigger != omniJackModTrigger.signal) signal.sigModTrigger = omniJackModTrigger.signal;

        signal.SetParam(timeDial.percent, (int)delaySignalGenerator.Param.P_TIME);
        signal.SetParam(feedbackDial.percent, (int)delaySignalGenerator.Param.P_FEEDBACK);
        signal.SetParam(Utils.equalPowerCrossfadeGain( mixDial.percent ), (int)delaySignalGenerator.Param.P_WET);
        signal.SetParam(Utils.equalPowerCrossfadeGain(1 - mixDial.percent), (int)delaySignalGenerator.Param.P_DRY);
        if (panicButton.isHit)
            signal.SetParam(1, (int)delaySignalGenerator.Param.P_CLEAR);

        signal.SetTimeRange(Mathf.RoundToInt(modeDial.percent * 3));
    }

    public override InstrumentData GetData()
    {
        DelayData data = new DelayData();
        data.deviceType = menuItem.deviceType.Delay;
        GetTransformData(data);

        data.timeState = timeDial.percent;
        data.feedbackState = feedbackDial.percent;
        data.mixState = mixDial.percent;
        data.modeState = modeDial.percent;

        data.jackInID = omniJackIn.transform.GetInstanceID();
        data.jackOutID = omniJackOut.transform.GetInstanceID();
        data.cTimeID = omniJackModTime.transform.GetInstanceID();
        data.cFeedbackID = omniJackModFeedback.transform.GetInstanceID();
        data.cMixID = omniJackModMix.transform.GetInstanceID();
        data.cTriggerID = omniJackModTrigger.transform.GetInstanceID();

        return data;
    }

    public override void Load(InstrumentData d)
    {
        DelayData data = d as DelayData;
        base.Load(data);

        omniJackIn.ID = data.jackInID;
        omniJackOut.ID = data.jackOutID;
        omniJackModTime.ID = data.cTimeID;
        omniJackModFeedback.ID = data.cFeedbackID;
        omniJackModTrigger.ID = data.cTriggerID;
        omniJackModMix.ID = data.cMixID;

        timeDial.setPercent(data.timeState);
        feedbackDial.setPercent(data.feedbackState);
        mixDial.setPercent(data.mixState);
        modeDial.setPercent(data.modeState);
    }
}

public class DelayData : InstrumentData
{
    public float timeState;
    public float feedbackState;
    public float mixState;
    public float modeState;

    public int jackOutID;
    public int jackInID;

    public int cTimeID, cFeedbackID, cMixID, cTriggerID;
}
