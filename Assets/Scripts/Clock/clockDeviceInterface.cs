using UnityEngine;
using System.Xml.Serialization;

public class clockDeviceInterface : deviceInterface
{
    public omniJack phaseInputJack, clockOutputJack, resetInputJack;
    public sliderNotched resolutionSlider;
    public dial swingDial;
    public Transform progressBar;
    public Renderer visualizationRenderer;
    public Material visualizationMaterial;
    public Transform progressBarLine;

    private clockSignalGenerator clockGenerator;
    private Material visualizationMat;
    private static readonly int[] baseResolutions = new int[] { 1, 2, 4, 8, 12, 16, 24, 32, 64 };
    private static readonly int[] slowCycleDivisions = new int[] { 8, 4, 3, 2 };
    Color vizColor;

    public override void Awake()
    {
        base.Awake();
        vizColor = new Color(0.8f, 0.1607843f, 0.1607843f, 1f);

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

        if (visualizationRenderer != null && visualizationMaterial != null)
        {
            visualizationMat = new Material(visualizationMaterial);
            visualizationRenderer.material = visualizationMat;
        }

        // resolutionSlider: 1 step per 8/4/3/2 cycles, then 1, 2, 4, 8, 12, 16, 24, 32, 64
        setupResolutionLabels();
    }

    void OnEnable()
    {
        phaseGeneratorDeviceInterface.resetPressedEvent += handlePhaseReset;
    }

    void OnDisable()
    {
        phaseGeneratorDeviceInterface.resetPressedEvent -= handlePhaseReset;
    }

    void Update()
    {
        applyResolutionSettings();
        clockGenerator.phaseInput = phaseInputJack != null ? phaseInputJack.signal : null;
        clockGenerator.resetInput = resetInputJack != null ? resetInputJack.signal : null;
        bool outputPlugged = clockOutputJack != null && clockOutputJack.near != null;
        clockGenerator.autorunning = !outputPlugged;
        updateProgressBar();
        updateProgressBarLine();
        updateVisualization();
    }

    private void applyResolutionSettings()
    {
        float swingPercent = swingDial != null ? swingDial.percent : 0.5f;
        if (resolutionSlider == null)
        {
            clockGenerator.UpdateSettings(clockGenerator.resolutionIndex, swingPercent);
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

    void handlePhaseReset(phaseGeneratorDeviceInterface phase)
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

    private void updateProgressBarLine()
    {
        if (progressBarLine == null || visualizationRenderer == null)
        {
            return;
        }

        Transform lineParent = progressBarLine.parent;
        if (lineParent == null)
        {
            return;
        }

        // Use the renderer's local bounds to find the max X in its own space, then convert to world.
        Bounds localBounds = visualizationRenderer.localBounds;
        Vector3 localMax = localBounds.center + new Vector3(localBounds.extents.x, 0f, 0f);
        Vector3 worldMax = visualizationRenderer.transform.TransformPoint(localMax);
        // Convert that world position into the line parent's space so only local X is updated.
        Vector3 localPos = lineParent.InverseTransformPoint(worldMax);
        Vector3 lineLocalPos = progressBarLine.localPosition;
        lineLocalPos.x = localPos.x;
        progressBarLine.localPosition = lineLocalPos;
    }

    private void updateVisualization()
    {
        if (visualizationMat == null || clockGenerator == null)
        {
            return;
        }

        float progress = Mathf.Clamp01(clockGenerator.progressToNextTrigger);
        vizColor.a = Mathf.Pow(progress, 2f);
        visualizationMat.SetColor("_BaseColor", vizColor);
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
