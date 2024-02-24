using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class artefactDeviceInterface : deviceInterface
{
    public omniJack input, output;
    public dial noiseDial, jitterDial, downsampleDial, bitreductionDial;

    artefactSignalGenerator signal;

    public override void Awake()
    {
        base.Awake();
        signal = GetComponent<artefactSignalGenerator>();
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
        ArtefactData data = new ArtefactData();
        data.deviceType = DeviceType.Artefact;
        GetTransformData(data);

        data.noiseAmount = noiseDial.percent;
        data.jitterAmount = jitterDial.percent;
        data.downsampleFactor = downsampleDial.percent;
        data.bitReduction = bitreductionDial.percent;

        data.jackInID = input.transform.GetInstanceID();
        data.jackOutID = output.transform.GetInstanceID();

        return data;
    }

    public override void Load(InstrumentData d)
    {
        ArtefactData data = d as ArtefactData;
        base.Load(data);

        input.ID = data.jackInID;
        output.ID = data.jackOutID;

        noiseDial.setPercent(data.noiseAmount);
        jitterDial.setPercent(data.jitterAmount);
        downsampleDial.setPercent(data.downsampleFactor);
        bitreductionDial.setPercent(data.bitReduction);
    }
}

public class ArtefactData : InstrumentData
{
    public float noiseAmount;
    public float jitterAmount;
    public float downsampleFactor;
    public float bitReduction;

    public int jackOutID;
    public int jackInID;
}