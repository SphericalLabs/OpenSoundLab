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
using System;

public class compressorDeviceInterface : deviceInterface
{
    public omniJack input, sidechain, output;
    public dial attackDial, releaseDial, thresholdDial, ratioDial, makeupDial;
    public button bypassSwitch;
    public Transform attenuationDisplay;
    //public GameObject clippingDisplay;
    compressorSignalGenerator signal;
    private dial[] dials;

    public override void Awake()
    {
        base.Awake();
        signal = GetComponent<compressorSignalGenerator>();
        dials = new dial[5] { attackDial, releaseDial, thresholdDial, ratioDial, makeupDial };
        //clippingDisplay.SetActive(false);
        attenuationDisplay.localScale = Vector3.up * 0f;
    }

    void Update()
    {
        if (signal.input != input.signal) signal.input = input.signal;
        if (signal.sidechain != sidechain.signal) signal.sidechain = sidechain.signal;

        signal.SetParam(attackDial.percent, (int)compressorSignalGenerator.Param.P_ATTACK);
        signal.SetParam(releaseDial.percent, (int)compressorSignalGenerator.Param.P_RELEASE);
        signal.SetParam(thresholdDial.percent, (int)compressorSignalGenerator.Param.P_THRESHOLD);
        signal.SetParam(ratioDial.percent, (int)compressorSignalGenerator.Param.P_RATIO);
        signal.SetParam(makeupDial.percent, (int)compressorSignalGenerator.Param.P_MAKEUP);
        signal.SetParam(bypassSwitch.isHit ? 1 : 0, (int)compressorSignalGenerator.Param.P_BYPASS);

        //float attenuation = signal.attenuation;
        //float attenuationPercent = Mathf.Min( (attenuation / -40.0f), 1);
        
        attenuationDisplay.localScale = new Vector3(1f, 1f, Mathf.Min((signal.attenuation / -40.0f), 1));
    
        //clippingDisplay.SetActive(signal.isClipping);
    }

    public override InstrumentData GetData()
    {
        CompressorData data = new CompressorData();
        data.deviceType = DeviceType.Compressor;
        GetTransformData(data);

        data.attackState = attackDial.percent;
        data.releaseState = releaseDial.percent;
        data.thresholdState = thresholdDial.percent;
        data.ratioState = ratioDial.percent;
        data.makeupState = makeupDial.percent;
        data.bypassState = bypassSwitch.isHit;

        data.jackInID = input.transform.GetInstanceID();
        data.jackOutID = output.transform.GetInstanceID();
        data.jackSidechainID = sidechain.transform.GetInstanceID();

        return data;
    }

    public override void Load(InstrumentData d)
    {
        CompressorData data = d as CompressorData;
        base.Load(data);

        input.ID = data.jackInID;
        output.ID = data.jackOutID;
        sidechain.ID = data.jackSidechainID;

        attackDial.setPercent(data.attackState);
        releaseDial.setPercent(data.releaseState);
        thresholdDial.setPercent(data.thresholdState);
        ratioDial.setPercent(data.ratioState);
        makeupDial.setPercent(data.makeupState);
        bypassSwitch.keyHit(data.bypassState);
    }
}

public class CompressorData : InstrumentData
{

    public float attackState;
    public float releaseState;
    public float thresholdState;
    public float ratioState;
    public float makeupState;
    public bool bypassState;

    public int jackOutID;
    public int jackInID;
    public int jackSidechainID;
}
