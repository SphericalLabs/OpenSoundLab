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

public class compressorDeviceInterface : deviceInterface
{
    public omniJack input, sidechain, output;
    public dial attackDial, releaseDial, thresholdDial, ratioDial, kneeDial, lookaheadDial, makeupDial;
    public glowDisk attenuationDisplay;
    public GameObject clippingDisplay;
    compressorSignalGenerator signal;
    private dial[] dials;

    public override void Awake()
    {
        base.Awake();
        signal = GetComponent<compressorSignalGenerator>();
        dials = new dial[7] { attackDial, releaseDial, thresholdDial, ratioDial, kneeDial, makeupDial, lookaheadDial };
        clippingDisplay.SetActive(false);
        attenuationDisplay.percent = 0;
    }

    void Update()
    {
        if (signal.input != input.signal) signal.input = input.signal;
        if (signal.sidechain != sidechain.signal) signal.sidechain = sidechain.signal;

        for(int i = 0; i < dials.Length; i++)
        {
            signal.SetParam(dials[i].percent, i);
        }

        /* TODO: The calculated values are absolutely correct, but the attenuationDisplay 
         * can not be seen on the Quest 2...
         */

        float attenuation = signal.attenuation;
        //float attenuationScaled = ( attenuation / -40 ) * 0.05f;
        /*float attenuationScaled = 0.01f + (attenuation / -40) * 0.05f;
        Vector3 scale = attenuationDisplay.transform.localScale;
        Vector3 position = attenuationDisplay.transform.position;
        attenuationDisplay.transform.localScale = new Vector3(attenuation, scale.y, scale.z);
        attenuationDisplay.transform.position = new Vector3(-0.07f + attenuationScaled / 2, position.y, position.z);*/

        float attenuationPercent = Mathf.Min( (attenuation / -40.0f), 1);
        attenuationDisplay.percent = attenuationPercent * 0.85f;
        attenuationDisplay.PercentUpdate();
        Debug.Log("attenuation: " + attenuation + " / " + attenuationPercent);

        clippingDisplay.SetActive(signal.isClipping);
    }

    public override InstrumentData GetData()
    {
        compressorData data = new compressorData();
        data.deviceType = menuItem.deviceType.Compressor;
        GetTransformData(data);

        data.attackState = attackDial.percent;
        data.releaseState = releaseDial.percent;
        data.thresholdState = thresholdDial.percent;
        data.ratioState = ratioDial.percent;
        data.kneeState = kneeDial.percent;
        data.makeupState = makeupDial.percent;
        data.lookaheadState = lookaheadDial.percent;

        data.jackInID = input.transform.GetInstanceID();
        data.jackOutID = output.transform.GetInstanceID();
        data.jackSidechainID = sidechain.transform.GetInstanceID();

        return data;
    }

    public override void Load(InstrumentData d)
    {
        compressorData data = d as compressorData;
        base.Load(data);

        input.ID = data.jackInID;
        output.ID = data.jackOutID;
        sidechain.ID = data.jackSidechainID;

        attackDial.setPercent(data.attackState);
        releaseDial.setPercent(data.releaseState);
        thresholdDial.setPercent(data.thresholdState);
        ratioDial.setPercent(data.ratioState);
        kneeDial.setPercent(data.kneeState);
        makeupDial.setPercent(data.makeupState);
        lookaheadDial.setPercent(data.lookaheadState);
    }
}

public class compressorData : InstrumentData
{
    public float attackState;
    public float releaseState;
    public float thresholdState;
    public float ratioState;
    public float kneeState;
    public float makeupState;
    public float lookaheadState;

    public int jackOutID;
    public int jackInID;
    public int jackSidechainID;
}
