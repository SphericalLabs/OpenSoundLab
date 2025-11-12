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

public class filterDeviceInterface : deviceInterface
{

    int ID = 0;
    public omniJack input, controlInput, output;
    public dial frequencyDial, resonanceDial, modeDial, bandwidthDial;

    filterSignalGenerator filter;

    float freqPercent, resPercent, modePercent = -1f;
    //public float bandwidthPercent = 0f;


    public override void Awake()
    {
        base.Awake();
        filter = GetComponent<filterSignalGenerator>();
    }

    void Update()
    {


        if (filter.incoming != input.signal)
        {
            filter.incoming = input.signal;
        }

        if (filter.freqIncoming != controlInput.signal)
        {
            filter.freqIncoming = controlInput.signal;
        }

        if (freqPercent != frequencyDial.percent) updateFrequency();
        if (resPercent != resonanceDial.percent) updateResonance();
        if (modePercent != modeDial.percent) updateMode();


        //filter.bandWidthHalfed = bandwidthPercent = Utils.map(bandwidthDial.percent, 0f, 1f, 0f, 0.4f) / 2f; // up to 4 octaves
        //filter.bandWidthHalfed = Mathf.Pow(bandwidthPercent, 3f) / 2f;
    }

    void updateFrequency()
    {
        freqPercent = frequencyDial.percent;
        filter.cutoffFrequency = Utils.map(frequencyDial.percent, 0f, 1f, -0.5f, 0.5f); // 13 octaves around C4
    }

    void updateResonance()
    {
        resPercent = resonanceDial.percent;
        filter.resonance = resonanceDial.percent;
    }
    void updateMode()
    {
        modePercent = modeDial.percent;

        switch (Mathf.RoundToInt(modePercent * 3))
        {
            case 0:
                filter.curType = filterSignalGenerator.filterType.LP;
                break;
            case 1:
                filter.curType = filterSignalGenerator.filterType.HP;
                break;
            case 2:
                filter.curType = filterSignalGenerator.filterType.BP;
                break;
            case 3:
                filter.curType = filterSignalGenerator.filterType.Notch;
                break;
        }

    }

    public override InstrumentData GetData()
    {
        FilterData data = new FilterData();
        data.deviceType = DeviceType.Filter;
        GetTransformData(data);

        data.jackInID = input.transform.GetInstanceID();
        data.jackOutID = output.transform.GetInstanceID();
        data.jackControlInID = controlInput.transform.GetInstanceID();

        data.resonance = resonanceDial.percent;
        data.frequency = frequencyDial.percent;
        data.filterMode = modeDial.percent;

        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        FilterData data = d as FilterData;
        base.Load(data, true);

        ID = data.ID;
        input.SetID(data.jackInID, copyMode);
        output.SetID(data.jackOutID, copyMode);
        controlInput.SetID(data.jackControlInID, copyMode);

        resonanceDial.setPercent(data.resonance);
        frequencyDial.setPercent(data.frequency);
        modeDial.setPercent(data.filterMode);
    }


}


public class FilterData : InstrumentData
{
    public float resonance, frequency; // width is for BP
                                       //public int filterMode; // 0 = LP, 1 == BP, 2 = HP, 4 = NO(TCH)
                                       //public filterSignalGenerator.filterType filterMode; // possible?
    public float filterMode;
    public int jackOutID;
    public int jackInID;
    public int jackControlInID;
}
