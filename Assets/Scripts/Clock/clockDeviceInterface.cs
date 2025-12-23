using UnityEngine;
using System.Collections;
using System.Xml.Serialization;

public class clockDeviceInterface : deviceInterface
{
    public omniJack resetJack, clockJack, phaseJack;
    public dial bpmDial;
    public TextMesh bpmDisplay;

    public clockSignalGenerator resetSignal, clockSignal, phaseSignal;

    public float minBpm = 60f;
    public float maxBpm = 180f;

    private bool isRunning = true;
    private float pitchBendMult = 1f;

    public button playButton, rewindButton, nudgeForwardButton, nudgeBackwardButton, recordButton;

    public override void Awake()
    {
        base.Awake();
        var gens = GetComponents<clockSignalGenerator>();
        // Ensure we have 3 generators
        if (gens.Length < 3)
        {
            phaseSignal = gameObject.AddComponent<clockSignalGenerator>();
            clockSignal = gameObject.AddComponent<clockSignalGenerator>();
            resetSignal = gameObject.AddComponent<clockSignalGenerator>();
        }
        else
        {
            phaseSignal = gens[0];
            clockSignal = gens[1];
            resetSignal = gens[2];
        }

        phaseSignal.mode = clockSignalGenerator.ClockOutputMode.Phase;
        clockSignal.mode = clockSignalGenerator.ClockOutputMode.Pulse;
        resetSignal.mode = clockSignalGenerator.ClockOutputMode.Reset;

        phaseJack.signal = phaseSignal;
        clockJack.signal = clockSignal;
        resetJack.signal = resetSignal;

        // Auto-discover buttons for visual sync
        button[] buttons = GetComponentsInChildren<button>();
        foreach (button b in buttons)
        {
            if (b.buttonID == 0) playButton = b;
            if (b.buttonID == 1) rewindButton = b;
            if (b.buttonID == 3) nudgeBackwardButton = b;
            if (b.buttonID == 4) nudgeForwardButton = b;
            if (b.buttonID == 5) recordButton = b;
        }

        // bpmDial.onPercentChangedEventLocal.AddListener(readBpmDialAndBroadcast);
    }

    // void readBpmDialAndBroadcast()
    // {
    //     float targetBpm = Mathf.Round(Utils.map(bpmDial.percent, 0f, 1f, minBpm, maxBpm) * 10f) / 10f;
    //     targetBpm *= pitchBendMult;

    //     if (targetBpm != phaseSignal.bpm)
    //     {
    //         phaseSignal.setBPM(targetBpm);
    //         clockSignal.setBPM(targetBpm);
    //         resetSignal.setBPM(targetBpm);
    //         if (bpmDisplay != null) bpmDisplay.text = (targetBpm / pitchBendMult).ToString("N1");
    //     }
    // }

    void Update()
    {
        if (bpmDial != null)
        {
            float targetBpm = Mathf.Round(Utils.map(bpmDial.percent, 0f, 1f, minBpm, maxBpm) * 10f) / 10f;
            targetBpm *= pitchBendMult;

            if (targetBpm != phaseSignal.bpm)
            {
                phaseSignal.setBPM(targetBpm);
                clockSignal.setBPM(targetBpm);
                resetSignal.setBPM(targetBpm);
                if (bpmDisplay != null) bpmDisplay.text = (targetBpm / pitchBendMult).ToString("N1");
            }
        }

        // Apply running state
        phaseSignal.running = isRunning;
        clockSignal.running = isRunning;
        resetSignal.running = isRunning;

        // Sync generators
        clockSignal._measurePhase = phaseSignal._measurePhase;
        resetSignal._measurePhase = phaseSignal._measurePhase;

        // Record button state sync
        if (recordButton != null && masterControl.instance != null && masterControl.instance.recorder != null)
        {
            bool isRecording = masterControl.instance.recorder.state != masterBusRecorder.State.Idle;
            if (recordButton.isHit != isRecording) recordButton.phantomHit(isRecording);
        }
    }

    public override void hit(bool on, int ID = -1)
    {
        if (ID == 0) // Play/Stop
        {
            isRunning = on;
        }
        else if (ID == 1 && on) // Rewind
        {
            phaseSignal.ResetPhase();
            clockSignal.ResetPhase();
            resetSignal.ResetPhase();
        }
        else if (ID == 3) // Nudge Backward
        {
            pitchBendMult = on ? (1f / 1.03f) : 1f;
        }
        else if (ID == 4) // Nudge Forward
        {
            pitchBendMult = on ? 1.03f : 1f;
        }
        else if (ID == 5) // Record
        {
            if (masterControl.instance != null && masterControl.instance.recorder != null)
            {
                masterControl.instance.recorder.ToggleRec(on);
            }
        }
    }

    public override InstrumentData GetData()
    {
        ClockData data = new ClockData
        {
            deviceType = DeviceType.Clock
        };
        GetTransformData(data);
        data.bpmPercent = bpmDial != null ? bpmDial.percent : 0.5f;
        data.resetJackID = resetJack.transform.GetInstanceID();
        data.clockJackID = clockJack.transform.GetInstanceID();
        data.phaseJackID = phaseJack.transform.GetInstanceID();
        data.isRunning = isRunning;
        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        ClockData data = d as ClockData;
        base.Load(data, copyMode);
        if (bpmDial != null) bpmDial.setPercent(data.bpmPercent);
        resetJack.SetID(data.resetJackID, copyMode);
        clockJack.SetID(data.clockJackID, copyMode);
        phaseJack.SetID(data.phaseJackID, copyMode);

        isRunning = data.isRunning;
        if (playButton != null) playButton.phantomHit(isRunning);
    }
}

[XmlType("ClockData")]
public class ClockData : InstrumentData
{
    public float bpmPercent;
    public int resetJackID, clockJackID, phaseJackID;
    public bool isRunning;
}
