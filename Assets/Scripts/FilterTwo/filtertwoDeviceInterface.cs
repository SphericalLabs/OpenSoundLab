using UnityEngine;
using System.Collections;
using System.Xml.Serialization;

public class filtertwoDeviceInterface : deviceInterface
{
    public omniJack input, controlInput, output;
    public dial frequencyDial, resonanceDial, modeDial;
    public TextMesh resonanceLabel;

    filtertwoSignalGenerator signal;
    string lastResonanceLabel = "";

    public override void Awake()
    {
        base.Awake();
        signal = GetComponent<filtertwoSignalGenerator>();
    }

    void Update()
    {
        signalGenerator nextInput = input != null ? input.signal : null;
        signalGenerator nextControlInput = controlInput != null ? controlInput.signal : null;

        if (signal.incoming != nextInput)
            signal.incoming = nextInput;

        if (signal.freqIncoming != nextControlInput)
            signal.freqIncoming = nextControlInput;

        if (frequencyDial != null)
            signal.cutoffFrequency = frequencyDial.percent;

        if (resonanceDial != null)
            signal.resonance = resonanceDial.percent;

        if (modeDial != null)
            signal.setModeFromPercent(modeDial.percent);

        updateResonanceLabel();
    }

    public override InstrumentData GetData()
    {
        FilterTwoData data = new FilterTwoData();
        data.deviceType = DeviceType.FilterTwo;
        GetTransformData(data);

        data.jackInID = input != null ? input.transform.GetInstanceID() : 0;
        data.jackOutID = output != null ? output.transform.GetInstanceID() : 0;
        data.jackControlInID = controlInput != null ? controlInput.transform.GetInstanceID() : 0;

        data.frequency = frequencyDial != null ? frequencyDial.percent : Mathf.Clamp01(signal.cutoffFrequency);
        data.resonance = resonanceDial != null ? resonanceDial.percent : Mathf.Clamp01(signal.resonance);
        data.filterMode = modeDial != null ? modeDial.percent : signal.getModePercent();

        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        FilterTwoData data = d as FilterTwoData;
        base.Load(data, copyMode);

        if (input != null) input.SetID(data.jackInID, copyMode);
        if (output != null) output.SetID(data.jackOutID, copyMode);
        if (controlInput != null) controlInput.SetID(data.jackControlInID, copyMode);

        if (frequencyDial != null)
            frequencyDial.setPercent(data.frequency);
        else
            signal.cutoffFrequency = data.frequency;

        if (resonanceDial != null)
            resonanceDial.setPercent(data.resonance);
        else
            signal.resonance = data.resonance;

        if (modeDial != null)
            modeDial.setPercent(data.filterMode);
        else
            signal.setModeFromPercent(data.filterMode);

        updateResonanceLabel();
    }

    void updateResonanceLabel()
    {
        if (resonanceLabel == null)
            return;

        string nextLabel = signal.curMode == filtertwoSignalGenerator.filterMode.Notch ? "DEPTH" : "RESONANCE";
        if (nextLabel == lastResonanceLabel)
            return;

        resonanceLabel.text = nextLabel;
        lastResonanceLabel = nextLabel;
    }
}

[XmlType("FilterTwoData")]
public class FilterTwoData : InstrumentData
{
    public float resonance, frequency;
    public float filterMode;
    public int jackOutID;
    public int jackInID;
    public int jackControlInID;
}
