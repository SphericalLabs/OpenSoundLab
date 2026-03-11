using UnityEngine;
using System.Collections;
using System.Xml.Serialization;

public class filterthreeDeviceInterface : deviceInterface
{
    public omniJack input, output;

    public dial lowCutDial;
    public dial lowShelfFrequencyDial, lowShelfGainDial;
    public dial bellOneFrequencyDial, bellOneGainDial, bellOneQDial;
    public dial bellTwoFrequencyDial, bellTwoGainDial, bellTwoQDial;
    public dial highShelfFrequencyDial, highShelfGainDial;
    public dial highCutDial;

    filterthreeSignalGenerator signal;

    public override void Awake()
    {
        base.Awake();
        signal = GetComponent<filterthreeSignalGenerator>();
    }

    void Update()
    {
        signalGenerator nextInput = input != null ? input.signal : null;
        if (signal.incoming != nextInput)
            signal.incoming = nextInput;

        if (lowCutDial != null) signal.lowCutPercent = lowCutDial.percent;
        if (lowShelfFrequencyDial != null) signal.lowShelfFrequencyPercent = lowShelfFrequencyDial.percent;
        if (lowShelfGainDial != null) signal.lowShelfGainPercent = lowShelfGainDial.percent;
        if (bellOneFrequencyDial != null) signal.bellOneFrequencyPercent = bellOneFrequencyDial.percent;
        if (bellOneGainDial != null) signal.bellOneGainPercent = bellOneGainDial.percent;
        if (bellOneQDial != null) signal.bellOneQPercent = bellOneQDial.percent;
        if (bellTwoFrequencyDial != null) signal.bellTwoFrequencyPercent = bellTwoFrequencyDial.percent;
        if (bellTwoGainDial != null) signal.bellTwoGainPercent = bellTwoGainDial.percent;
        if (bellTwoQDial != null) signal.bellTwoQPercent = bellTwoQDial.percent;
        if (highShelfFrequencyDial != null) signal.highShelfFrequencyPercent = highShelfFrequencyDial.percent;
        if (highShelfGainDial != null) signal.highShelfGainPercent = highShelfGainDial.percent;
        if (highCutDial != null) signal.highCutPercent = highCutDial.percent;
    }

    public override InstrumentData GetData()
    {
        FilterThreeData data = new FilterThreeData();
        data.deviceType = DeviceType.FilterThree;
        GetTransformData(data);

        data.jackInID = input != null ? input.transform.GetInstanceID() : 0;
        data.jackOutID = output != null ? output.transform.GetInstanceID() : 0;

        data.lowCutState = lowCutDial != null ? lowCutDial.percent : signal.lowCutPercent;
        data.lowShelfFrequencyState = lowShelfFrequencyDial != null ? lowShelfFrequencyDial.percent : signal.lowShelfFrequencyPercent;
        data.lowShelfGainState = lowShelfGainDial != null ? lowShelfGainDial.percent : signal.lowShelfGainPercent;
        data.bellOneFrequencyState = bellOneFrequencyDial != null ? bellOneFrequencyDial.percent : signal.bellOneFrequencyPercent;
        data.bellOneGainState = bellOneGainDial != null ? bellOneGainDial.percent : signal.bellOneGainPercent;
        data.bellOneQState = bellOneQDial != null ? bellOneQDial.percent : signal.bellOneQPercent;
        data.bellTwoFrequencyState = bellTwoFrequencyDial != null ? bellTwoFrequencyDial.percent : signal.bellTwoFrequencyPercent;
        data.bellTwoGainState = bellTwoGainDial != null ? bellTwoGainDial.percent : signal.bellTwoGainPercent;
        data.bellTwoQState = bellTwoQDial != null ? bellTwoQDial.percent : signal.bellTwoQPercent;
        data.highShelfFrequencyState = highShelfFrequencyDial != null ? highShelfFrequencyDial.percent : signal.highShelfFrequencyPercent;
        data.highShelfGainState = highShelfGainDial != null ? highShelfGainDial.percent : signal.highShelfGainPercent;
        data.highCutState = highCutDial != null ? highCutDial.percent : signal.highCutPercent;

        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        FilterThreeData data = d as FilterThreeData;
        base.Load(data, copyMode);

        if (input != null) input.SetID(data.jackInID, copyMode);
        if (output != null) output.SetID(data.jackOutID, copyMode);

        setDial(lowCutDial, data.lowCutState, ref signal.lowCutPercent);
        setDial(lowShelfFrequencyDial, data.lowShelfFrequencyState, ref signal.lowShelfFrequencyPercent);
        setDial(lowShelfGainDial, data.lowShelfGainState, ref signal.lowShelfGainPercent);
        setDial(bellOneFrequencyDial, data.bellOneFrequencyState, ref signal.bellOneFrequencyPercent);
        setDial(bellOneGainDial, data.bellOneGainState, ref signal.bellOneGainPercent);
        setDial(bellOneQDial, data.bellOneQState, ref signal.bellOneQPercent);
        setDial(bellTwoFrequencyDial, data.bellTwoFrequencyState, ref signal.bellTwoFrequencyPercent);
        setDial(bellTwoGainDial, data.bellTwoGainState, ref signal.bellTwoGainPercent);
        setDial(bellTwoQDial, data.bellTwoQState, ref signal.bellTwoQPercent);
        setDial(highShelfFrequencyDial, data.highShelfFrequencyState, ref signal.highShelfFrequencyPercent);
        setDial(highShelfGainDial, data.highShelfGainState, ref signal.highShelfGainPercent);
        setDial(highCutDial, data.highCutState, ref signal.highCutPercent);
    }

    void setDial(dial targetDial, float value, ref float targetValue)
    {
        if (targetDial != null)
            targetDial.setPercent(value);
        else
            targetValue = value;
    }
}

[XmlType("FilterThreeData")]
public class FilterThreeData : InstrumentData
{
    public float lowCutState;
    public float lowShelfFrequencyState;
    public float lowShelfGainState;
    public float bellOneFrequencyState;
    public float bellOneGainState;
    public float bellOneQState;
    public float bellTwoFrequencyState;
    public float bellTwoGainState;
    public float bellTwoQState;
    public float highShelfFrequencyState;
    public float highShelfGainState;
    public float highCutState;

    public int jackInID;
    public int jackOutID;
}
