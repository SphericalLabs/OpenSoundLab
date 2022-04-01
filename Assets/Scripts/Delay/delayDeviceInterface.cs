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
    public omniJack input, cTimeInput, cFeedbackInput, output;
    public dial timeDial, feedbackDial, mixDial;
    public dial modeDial; //this one is special and does not go into the array
    public button panicButton;
    delaySignalGenerator signal;
    private dial[] dials;

    public override void Awake()
    {
        base.Awake();
        signal = GetComponent<delaySignalGenerator>();
        dials = new dial[3] { timeDial, feedbackDial, mixDial };
    }

    void Update()
    {
        if (signal.input != input.signal) signal.input = input.signal;
        if (signal.cFeedbackInput != cFeedbackInput.signal) signal.cFeedbackInput = cFeedbackInput.signal;
        if (signal.cTimeInput != cTimeInput.signal) signal.cTimeInput = cTimeInput.signal;

        signal.SetParam(timeDial.percent, (int)delaySignalGenerator.Param.P_TIME);
        signal.SetParam(feedbackDial.percent, (int)delaySignalGenerator.Param.P_FEEDBACK);
        signal.SetParam(Utils.equalPowerCrossfadeGain( mixDial.percent ), (int)delaySignalGenerator.Param.P_WET);
        signal.SetParam(Utils.equalPowerCrossfadeGain(1 - mixDial.percent), (int)delaySignalGenerator.Param.P_DRY);
        if (panicButton.isHit)
            signal.SetParam(1, (int)delaySignalGenerator.Param.P_CLEAR);

        signal.SetMode(Mathf.RoundToInt(modeDial.percent * 3));
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

        data.jackInID = input.transform.GetInstanceID();
        data.jackOutID = output.transform.GetInstanceID();
        data.jackTimeID = cTimeInput.transform.GetInstanceID();
        data.jackFeedbackID = cFeedbackInput.transform.GetInstanceID();

        return data;
    }

    public override void Load(InstrumentData d)
    {
        DelayData data = d as DelayData;
        base.Load(data);

        input.ID = data.jackInID;
        output.ID = data.jackOutID;
        cTimeInput.ID = data.jackTimeID;
        cFeedbackInput.ID = data.jackFeedbackID;

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

    public int jackTimeID;
    public int jackFeedbackID;

}
