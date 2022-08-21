// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of OpenSoundLab.
//
// OpenSoundLab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenSoundLab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenSoundLab.  If not, see <http://www.gnu.org/licenses/>.

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
        data.deviceType = menuItem.deviceType.Compressor;
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
