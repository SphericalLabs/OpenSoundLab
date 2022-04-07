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
