// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2026 OSLLv1 Sphericals OpenSoundLab
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
    const float excitationThreshold = 0.985f;

    public omniJack input, controlInput, output;
    public dial frequencyDial, resonanceDial;

    filterSignalGenerator filter;

    float freqPercent = -1f;
    float resPercent = -1f;

    public override void Awake()
    {
        base.Awake();
        filter = GetComponent<filterSignalGenerator>();
        filter.curType = filterSignalGenerator.filterType.LP;
    }

    void Update()
    {
        signalGenerator nextInput = input != null ? input.signal : null;
        signalGenerator nextControlInput = controlInput != null ? controlInput.signal : null;

        if (filter.incoming != nextInput)
            filter.incoming = nextInput;

        if (filter.freqIncoming != nextControlInput)
            filter.freqIncoming = nextControlInput;

        filter.curType = filterSignalGenerator.filterType.LP;

        if (frequencyDial != null && freqPercent != frequencyDial.percent)
            updateFrequency();
        if (resonanceDial != null && resPercent != resonanceDial.percent)
            updateResonance();
    }

    void updateFrequency()
    {
        freqPercent = frequencyDial.percent;
        filter.cutoffFrequency = frequencyDial.percent;
    }

    void updateResonance()
    {
        float previousResonance = filter.resonance;
        resPercent = resonanceDial.percent;
        filter.resonance = resonanceDial.percent;
        if (previousResonance < excitationThreshold && filter.resonance >= excitationThreshold)
            filter.queueExcitation();
    }

    public override InstrumentData GetData()
    {
        FilterData data = new FilterData();
        setDeviceType(data);
        GetTransformData(data);

        data.jackInID = input != null ? input.transform.GetInstanceID() : 0;
        data.jackOutID = output != null ? output.transform.GetInstanceID() : 0;
        data.jackControlInID = controlInput != null ? controlInput.transform.GetInstanceID() : 0;

        data.resonance = resonanceDial != null ? resonanceDial.percent : Mathf.Clamp01(filter.resonance);
        data.frequency = frequencyDial != null ? frequencyDial.percent : Mathf.Clamp01(filter.cutoffFrequency);
        data.filterMode = 0f;

        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        FilterData data = d as FilterData;
        base.Load(data, true);

        if (input != null) input.SetID(data.jackInID, copyMode);
        if (output != null) output.SetID(data.jackOutID, copyMode);
        if (controlInput != null) controlInput.SetID(data.jackControlInID, copyMode);

        if (resonanceDial != null)
            resonanceDial.setPercent(data.resonance);
        else
            filter.resonance = data.resonance;

        if (frequencyDial != null)
            frequencyDial.setPercent(data.frequency);
        else
            filter.cutoffFrequency = Mathf.Clamp01(data.frequency);

        filter.curType = filterSignalGenerator.filterType.LP;
        if (data.resonance >= excitationThreshold)
            filter.queueExcitation();
    }
}


public class FilterData : InstrumentData
{
    public float resonance, frequency;
    public float filterMode;
    public int jackOutID;
    public int jackInID;
    public int jackControlInID;
}
