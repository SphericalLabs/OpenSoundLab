using UnityEngine;
using System;
using System.Collections;
using System.Xml.Serialization;

public class phaseGeneratorDeviceInterface : deviceInterface
{
    public static Action<phaseGeneratorDeviceInterface> resetPressedEvent;

    public omniJack resetJack, phaseJack;
    public dial bpmDial;
    public TextMesh bpmDisplay;
    public Transform rod;
    public Renderer phaseVisualizationRenderer;
    public Material phaseVisualizationMaterial;
    private Material phaseVizMat;

    public phaseGeneratorSignalGenerator resetSignal, phaseSignal;

    public float minBpm = 60f;
    public float maxBpm = 180f;

    public bool isRunning = true;
    private float pitchBendMult = 1f;

    Color vizColor;


    public button playButton, rewindButton, nudgeForwardButton, nudgeBackwardButton;

    public override void Awake()
    {
        base.Awake();
        vizColor = new Color(0.8f, 0.1607843f, 0.1607843f, 1f); // todo: make this standard color which it also used e.g. by the dials somehow centralize this
        // Initialize signals only if they aren't already assigned
        if (phaseSignal == null || resetSignal == null)
        {
            var gens = GetComponents<phaseGeneratorSignalGenerator>();
            if (gens.Length < 2)
            {
                if (phaseSignal == null) phaseSignal = gameObject.AddComponent<phaseGeneratorSignalGenerator>();
                if (resetSignal == null) resetSignal = gameObject.AddComponent<phaseGeneratorSignalGenerator>();
            }
            else
            {
                for (int i = 0; i < gens.Length; i++)
                {
                    if (phaseSignal == null && gens[i].mode == phaseGeneratorSignalGenerator.PhaseOutputMode.Phase)
                    {
                        phaseSignal = gens[i];
                    }

                    if (resetSignal == null && gens[i].mode == phaseGeneratorSignalGenerator.PhaseOutputMode.Reset)
                    {
                        resetSignal = gens[i];
                    }
                }

                if (phaseSignal == null) phaseSignal = gens[0];
                if (resetSignal == null) resetSignal = gens[1];
            }
        }

        // Always ensure modes are correct as they might have been lost or default to Phase
        phaseSignal.mode = phaseGeneratorSignalGenerator.PhaseOutputMode.Phase;
        resetSignal.mode = phaseGeneratorSignalGenerator.PhaseOutputMode.Reset;

        if (phaseJack != null) phaseJack.homesignal = phaseSignal;
        if (resetJack != null) resetJack.homesignal = resetSignal;

        // Auto-discover buttons for visual sync
        button[] buttons = GetComponentsInChildren<button>();
        foreach (button b in buttons)
        {
            if (b.buttonID == 0) playButton = b;
            if (b.buttonID == 1) rewindButton = b;
            if (b.buttonID == 3) nudgeBackwardButton = b;
            if (b.buttonID == 4) nudgeForwardButton = b;
        }

        if (phaseVisualizationRenderer != null && phaseVisualizationMaterial != null)
        {
            phaseVizMat = new Material(phaseVisualizationMaterial); // creates a local instance
            phaseVisualizationRenderer.material = phaseVizMat;
        }

        // bpmDial.onPercentChangedEventLocal.AddListener(readBpmDialAndBroadcast);
    }

    void OnEnable()
    {
        resetPressedEvent += handleCrossPhaseReset;
    }

    void OnDisable()
    {
        resetPressedEvent -= handleCrossPhaseReset;
    }

    // void readBpmDialAndBroadcast()
    // {
    //     float targetBpm = Mathf.Round(Utils.map(bpmDial.percent, 0f, 1f, minBpm, maxBpm) * 10f) / 10f;
    //     targetBpm *= pitchBendMult;

    //     if (targetBpm != phaseSignal.bpm)
    //     {
    //         phaseSignal.setBPM(targetBpm);
    //         resetSignal.setBPM(targetBpm);
    //         if (bpmDisplay != null) bpmDisplay.text = (targetBpm / pitchBendMult).ToString("N1");
    //     }
    // }

    void Update()
    {
        if (nudgeForwardButton != null && nudgeForwardButton.isHit)
        {
            pitchBendMult = 1.03f;
        }
        else if (nudgeBackwardButton != null && nudgeBackwardButton.isHit)
        {
            pitchBendMult = 1f / 1.03f;
        }
        else
        {
            pitchBendMult = 1f;
        }

        if (bpmDial != null)
        {
            float targetBpm = Mathf.Round(Utils.map(bpmDial.percent, 0f, 1f, minBpm, maxBpm) * 10f) / 10f;
            targetBpm *= pitchBendMult;

            if (targetBpm != phaseSignal.bpm)
            {
                phaseSignal.setBPM(targetBpm);
                resetSignal.setBPM(targetBpm);
                if (bpmDisplay != null) bpmDisplay.text = targetBpm.ToString("N1");
            }
        }

        // Apply running state
        phaseSignal.running = isRunning;
        resetSignal.running = isRunning;


        // Rod animation
        if (rod != null && phaseSignal != null)
        {
            float curCycle = (float)(phaseSignal._measurePhase / phaseSignal.measurePeriod);
            // Full rotation per bar, start at phase 0
            rod.localRotation = Quaternion.Euler(0, 0, curCycle * 360f);

            if (phaseVizMat != null)
            {
                vizColor.a = Mathf.Pow(curCycle, 2f);
                phaseVizMat.SetColor("_BaseColor", vizColor);
            }
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
            handleResetPress();
        }
        else if (ID == 3) // Nudge Backward
        {
            pitchBendMult = on ? (1f / 1.03f) : 1f;
        }
        else if (ID == 4) // Nudge Forward
        {
            pitchBendMult = on ? 1.03f : 1f;
        }
    }

    void handleResetPress()
    {
        applyReset(true);
        OSLGlobalResetBus.Broadcast(this);
        if (resetPressedEvent != null) resetPressedEvent(this);
    }

    void handleCrossPhaseReset(phaseGeneratorDeviceInterface origin)
    {
        if (origin == null || origin == this) return;
        ApplySyncedReset();
    }

    void applyReset(bool triggerPulse)
    {
        if (phaseSignal != null) phaseSignal.ResetPhase();
        if (resetSignal == null) return;
        resetSignal.ResetPhase();
        if (triggerPulse) resetSignal.triggerResetPulse();
    }

    public void ApplySyncedReset()
    {
        applyReset(false);
    }

    public void BroadcastSyncedReset()
    {
        applyReset(false);
        OSLGlobalResetBus.Broadcast(this);
        if (resetPressedEvent != null) resetPressedEvent(this);
    }

    public void ApplyNetworkReset()
    {
        ApplySyncedReset();
    }

    public override InstrumentData GetData()
    {
        PhaseGeneratorData data = new PhaseGeneratorData();
        setDeviceType(data);
        GetTransformData(data);
        data.bpmPercent = bpmDial != null ? bpmDial.percent : 0.5f;
        // Reset jack persistence disabled for now (re-enable if jackReset returns).
        data.phaseJackID = phaseJack.transform.GetInstanceID();
        data.isRunning = isRunning;
        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        PhaseGeneratorData data = d as PhaseGeneratorData;
        base.Load(data, copyMode);
        if (bpmDial != null) bpmDial.setPercent(data.bpmPercent);
        // Reset jack persistence disabled for now (re-enable if jackReset returns).
        phaseJack.SetID(data.phaseJackID, copyMode);

        isRunning = data.isRunning;
        if (playButton != null) playButton.keyHit(isRunning, false);
    }
}

public class PhaseGeneratorData : InstrumentData
{
    public float bpmPercent;
    // public int resetJackID;
    public int phaseJackID;
    public bool isRunning;
}

[XmlType("PhaseData")]
public class PhaseDataLegacy : PhaseGeneratorData // legacy alias, remove when old saves are dropped
{
}
