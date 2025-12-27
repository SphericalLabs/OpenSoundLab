using UnityEngine;
using System.Xml.Serialization;

public class dividerDeviceInterface : deviceInterface
{
    public omniJack phaseInputJack, clockOutputJack, resetInputJack;
    public sliderNotched resolutionSlider;
    public dial swingDial;

    private dividerSignalGenerator clockGenerator;

    public override void Awake()
    {
        base.Awake();

        var gens = GetComponents<dividerSignalGenerator>();
        clockGenerator = gens.Length > 0 ? gens[0] : gameObject.AddComponent<dividerSignalGenerator>();

        if (phaseInputJack != null)
        {
            phaseInputJack.outgoing = false;
        }

        if (resetInputJack != null)
        {
            resetInputJack.outgoing = false;
        }

        if (clockOutputJack != null)
        {
            clockOutputJack.outgoing = true;
            clockOutputJack.homesignal = clockGenerator;
        }

        // resolutionSlider: 1, 2, 4, 8, 12, 16, 24, 32, 64
        // these match beatTracker.cs resolutions
    }

    void Update()
    {
        if (resolutionSlider != null)
        {
            clockGenerator.UpdateSettings(resolutionSlider.switchVal, swingDial != null ? swingDial.percent : 0.5f);
        }

        clockGenerator.phaseInput = phaseInputJack.signal;
        clockGenerator.resetInput = resetInputJack != null ? resetInputJack.signal : null;
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
        if (resetInputJack != null) data.resetInputJackID = resetInputJack.transform.GetInstanceID();
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
        if (resetInputJack != null) resetInputJack.SetID(data.resetInputJackID, copyMode);
        if (resolutionSlider != null) resolutionSlider.setVal(data.resolution);
        if (swingDial != null) swingDial.setPercent(data.swing);
    }
}

[XmlType("DividerData")]
public class DividerData : InstrumentData
{
    public int phaseInputJackID, clockOutputJackID, resetInputJackID;
    public int resolution;
    public float swing;
}
