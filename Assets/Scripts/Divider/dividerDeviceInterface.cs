using UnityEngine;
using System.Xml.Serialization;

public class dividerDeviceInterface : deviceInterface
{
    public omniJack phaseInputJack, clockOutputJack, resetOutputJack;
    public sliderNotched resolutionSlider;
    public dial swingDial;

    private dividerSignalGenerator clockGenerator, resetGenerator;

    public override void Awake()
    {
        base.Awake();

        var gens = GetComponents<dividerSignalGenerator>();
        if (gens.Length < 2)
        {
            clockGenerator = gameObject.AddComponent<dividerSignalGenerator>();
            resetGenerator = gameObject.AddComponent<dividerSignalGenerator>();
        }
        else
        {
            clockGenerator = gens[0];
            resetGenerator = gens[1];
        }

        clockGenerator.mode = dividerSignalGenerator.OutputMode.Clock;
        resetGenerator.mode = dividerSignalGenerator.OutputMode.Reset;

        clockOutputJack.signal = clockGenerator;
        resetOutputJack.signal = resetGenerator;

        // resolutionSlider: 1, 2, 4, 8, 12, 16, 24, 32, 64
        // these match beatTracker.cs resolutions
    }

    void Update()
    {
        if (resolutionSlider != null)
        {
            clockGenerator.UpdateSettings(resolutionSlider.switchVal, swingDial != null ? swingDial.percent : 0.5f);
            resetGenerator.UpdateSettings(resolutionSlider.switchVal, swingDial != null ? swingDial.percent : 0.5f);
        }

        clockGenerator.phaseInput = phaseInputJack.signal;
        resetGenerator.phaseInput = phaseInputJack.signal;
    }

    public override InstrumentData GetData()
    {
        DividerData data = new DividerData
        {
            deviceType = DeviceType.Divider
        };
        GetTransformData(data);
        data.phaseInputJackID = phaseInputJack.transform.GetInstanceID();
        data.clockOutputJackID = clockOutputJack.transform.GetInstanceID();
        data.resetOutputJackID = resetOutputJack.transform.GetInstanceID();
        data.resolution = resolutionSlider != null ? resolutionSlider.switchVal : 3;
        data.swing = swingDial != null ? swingDial.percent : 0.5f;
        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        DividerData data = d as DividerData;
        base.Load(data, copyMode);
        phaseInputJack.SetID(data.phaseInputJackID, copyMode);
        clockOutputJack.SetID(data.clockOutputJackID, copyMode);
        resetOutputJack.SetID(data.resetOutputJackID, copyMode);
        if (resolutionSlider != null) resolutionSlider.setVal(data.resolution);
        if (swingDial != null) swingDial.setPercent(data.swing);
    }
}

[XmlType("DividerData")]
public class DividerData : InstrumentData
{
    public int phaseInputJackID, clockOutputJackID, resetOutputJackID;
    public int resolution;
    public float swing;
}
