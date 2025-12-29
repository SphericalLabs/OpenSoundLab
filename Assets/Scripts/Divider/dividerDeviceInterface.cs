using UnityEngine;
using System.Xml.Serialization;

public class dividerDeviceInterface : deviceInterface
{
    public omniJack phaseInputJack, clockOutputJack, resetInputJack;
    public sliderNotched resolutionSlider;
    public dial swingDial;

    private dividerSignalGenerator clockGenerator;
    private static readonly int[] baseResolutions = new int[] { 1, 2, 4, 8, 12, 16, 24, 32, 64 };
    private static readonly int[] slowCycleDivisions = new int[] { 8, 4, 3, 2 };

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

        // resolutionSlider: 1 step per 8/4/3/2 cycles, then 1, 2, 4, 8, 12, 16, 24, 32, 64
    }

    void Update()
    {
        applyResolutionSettings();
        clockGenerator.phaseInput = phaseInputJack.signal;
        clockGenerator.resetInput = resetInputJack != null ? resetInputJack.signal : null;
    }

    private void applyResolutionSettings()
    {
        float swingPercent = swingDial != null ? swingDial.percent : 0.5f;
        if (resolutionSlider == null)
        {
            clockGenerator.UpdateSettings(3, swingPercent);
            clockGenerator.cycleDivision = 1;
            return;
        }

        int sliderVal = resolutionSlider.switchVal;
        int resolutionIndex = sliderVal - slowCycleDivisions.Length;
        int cycleDivision = 1;
        if (sliderVal < slowCycleDivisions.Length)
        {
            resolutionIndex = 0;
            cycleDivision = slowCycleDivisions[sliderVal];
        }
        else if (resolutionIndex < 0)
        {
            resolutionIndex = 0;
        }
        else if (resolutionIndex >= baseResolutions.Length)
        {
            resolutionIndex = baseResolutions.Length - 1;
        }

        clockGenerator.UpdateSettings(resolutionIndex, swingPercent);
        clockGenerator.cycleDivision = cycleDivision;
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
