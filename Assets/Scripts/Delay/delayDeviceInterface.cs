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
using System;

public class delayDeviceInterface : deviceInterface
{
    public omniJack input, cTimeInput, cFeedbackInput, output;
    public dial timeDial, feedbackDial, wetDial, dryDial;
    public dial modeDial; //this one is special and does not go into the array
    public button panicButton;
    delaySignalGenerator signal;
    private dial[] dials;

    public override void Awake()
    {
        base.Awake();
        signal = GetComponent<delaySignalGenerator>();
        dials = new dial[4] { timeDial, feedbackDial, wetDial, dryDial };
    }

    void Update()
    {
        if (signal.input != input.signal) signal.input = input.signal;
        if (signal.cFeedbackInput != cFeedbackInput.signal) signal.cFeedbackInput = cFeedbackInput.signal;
        if (signal.cTimeInput != cTimeInput.signal) signal.cTimeInput = cTimeInput.signal;

        for (int i = 0; i < dials.Length; i++)
        {
            signal.SetParam(dials[i].percent, i);
        }
        if (panicButton.isHit)
            signal.SetParam(1, (int)delaySignalGenerator.Param.P_CLEAR);

        signal.SetMode(Mathf.RoundToInt(modeDial.percent * 3));
    }

    public override InstrumentData GetData()
    {
        delayData data = new delayData();
        data.deviceType = menuItem.deviceType.Delay;
        GetTransformData(data);

        data.timeState = timeDial.percent;
        data.feedbackState = feedbackDial.percent;
        data.wetState = wetDial.percent;
        data.dryState = dryDial.percent;

        data.jackInID = input.transform.GetInstanceID();
        data.jackOutID = output.transform.GetInstanceID();

        return data;
    }

    public override void Load(InstrumentData d)
    {
        delayData data = d as delayData;
        base.Load(data);

        input.ID = data.jackInID;
        output.ID = data.jackOutID;

        timeDial.setPercent(data.timeState);
        feedbackDial.setPercent(data.feedbackState);
        wetDial.setPercent(data.wetState);
        dryDial.setPercent(data.dryState);
    }
}

public class delayData : InstrumentData
{
    public float timeState;
    public float feedbackState;
    public float wetState;
    public float dryState;

    public int jackOutID;
    public int jackInID;
    public int jackSidechainID;
}
