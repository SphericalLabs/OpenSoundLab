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
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class artifactDeviceInterface : deviceInterface
{
    public omniJack input, output;
    public dial noiseDial, jitterDial, downsampleDial, bitreductionDial;

    artifactSignalGenerator signal;

    public override void Awake()
    {
        base.Awake();
        signal = GetComponent<artifactSignalGenerator>();
    }

    void Update()
    {
        if (signal.input != input.signal) signal.input = input.signal;

        signal.noiseAmount = noiseDial.percent;
        signal.jitterAmount = jitterDial.percent;
        signal.downsampleFactor = downsampleDial.percent;
        signal.bitReduction = bitreductionDial.percent;
    }

    public override InstrumentData GetData()
    {
        ArtifactData data = new ArtifactData();
        data.deviceType = DeviceType.Artifact;
        GetTransformData(data);

        data.noiseAmount = noiseDial.percent;
        data.jitterAmount = jitterDial.percent;
        data.downsampleFactor = downsampleDial.percent;
        data.bitReduction = bitreductionDial.percent;

        data.jackInID = input.transform.GetInstanceID();
        data.jackOutID = output.transform.GetInstanceID();

        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        ArtifactData data = d as ArtifactData;
        base.Load(data, copyMode);

        input.SetID(data.jackInID, copyMode);
        output.SetID(data.jackOutID, copyMode);

        noiseDial.setPercent(data.noiseAmount);
        jitterDial.setPercent(data.jitterAmount);
        downsampleDial.setPercent(data.downsampleFactor);
        bitreductionDial.setPercent(data.bitReduction);
    }
}

public class ArtifactData : InstrumentData
{
    public float noiseAmount;
    public float jitterAmount;
    public float downsampleFactor;
    public float bitReduction;

    public int jackOutID;
    public int jackInID;
}

[XmlType("ArtefactData")]
public class ArtifactDataLegacy : ArtifactData // legacy alias, remove when old Artefact saves are dropped
{
}
