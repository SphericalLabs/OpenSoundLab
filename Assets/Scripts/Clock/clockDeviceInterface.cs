using UnityEngine;
using System.Xml.Serialization;

public class clockDeviceInterface : deviceInterface
{
    public omniJack phaseInputJack, clockOutputJack, resetInputJack;
    public sliderNotched resolutionSlider;
    public dial swingDial;
    public Transform progressBar;

    private clockSignalGenerator clockGenerator;
    private static readonly int[] baseResolutions = new int[] { 1, 2, 4, 8, 12, 16, 24, 32, 64 };
    private static readonly int[] slowCycleDivisions = new int[] { 8, 4, 3, 2 };

    public override void Awake()
    {
        base.Awake();

        var gens = GetComponents<clockSignalGenerator>();
        clockGenerator = gens.Length > 0 ? gens[0] : gameObject.AddComponent<clockSignalGenerator>();

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
        setupResolutionLabels();
    }

    void OnEnable()
    {
        phaseDeviceInterface.resetPressedEvent += handlePhaseReset;
    }

    void OnDisable()
    {
        phaseDeviceInterface.resetPressedEvent -= handlePhaseReset;
    }

    void Update()
    {
        applyResolutionSettings();
        clockGenerator.phaseInput = phaseInputJack != null ? phaseInputJack.signal : null;
        clockGenerator.resetInput = resetInputJack != null ? resetInputJack.signal : null;
        bool outputPlugged = clockOutputJack != null && clockOutputJack.near != null;
        clockGenerator.autorunning = !outputPlugged;
        updateProgressBar();
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

    private void setupResolutionLabels()
    {
        if (resolutionSlider == null)
        {
            return;
        }

        string[] labels = new string[slowCycleDivisions.Length + baseResolutions.Length];
        for (int i = 0; i < slowCycleDivisions.Length; i++)
        {
            labels[i] = formatCycleLabel(slowCycleDivisions[i]);
        }

        for (int i = 0; i < baseResolutions.Length; i++)
        {
            labels[slowCycleDivisions.Length + i] = baseResolutions[i].ToString();
        }

        resolutionSlider.createLabels(labels);
    }

    void handlePhaseReset(phaseDeviceInterface phase)
    {
        if (clockGenerator != null) clockGenerator.requestGlobalReset();
    }

    private void updateProgressBar()
    {
        if (progressBar == null || clockGenerator == null)
        {
            return;
        }

        float progress = Mathf.Clamp01(clockGenerator.progressToNextTrigger);
        Vector3 scale = progressBar.localScale;
        scale.x = progress;
        progressBar.localScale = scale;
    }

    private string formatCycleLabel(int division)
    {
        switch (division)
        {
            case 2:
                return "½";
            case 3:
                return "⅓";
            case 4:
                return "¼";
            case 8:
                return "⅛";
            default:
                return "1/" + division;
        }
    }

    public override InstrumentData GetData()
    {
        ClockData data = new ClockData
        {
            deviceType = DeviceType.Clock
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
        ClockData data = d as ClockData;
        if (data == null)
        {
            Debug.LogWarning("Clock device load received non-ClockData.");
            base.Load(d, copyMode);
            return;
        }
        base.Load(data, copyMode);
        phaseInputJack.SetID(data.phaseInputJackID, copyMode);
        clockOutputJack.SetID(data.clockOutputJackID, copyMode);
        if (resetInputJack != null) resetInputJack.SetID(data.resetInputJackID, copyMode);
        if (resolutionSlider != null) resolutionSlider.setVal(data.resolution);
        if (swingDial != null) swingDial.setPercent(data.swing);
    }
}

[XmlType("ClockData")]
public class ClockData : InstrumentData
{
    public int phaseInputJackID, clockOutputJackID, resetInputJackID;
    public int resolution;
    public float swing;
}
