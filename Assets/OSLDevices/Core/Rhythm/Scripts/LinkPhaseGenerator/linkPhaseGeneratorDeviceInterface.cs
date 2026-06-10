using UnityEngine;
using System.Collections;
using System.Xml.Serialization;

public class linkPhaseGeneratorDeviceInterface : deviceInterface
{
    public omniJack resetJack, phaseJack;
    public dial bpmDial, quantumDial, compensationDial;
    public TextMesh bpmDisplay, quantumDisplay, linkStatusDisplay;
    public Transform rod;
    public Renderer phaseVisualizationRenderer;
    public Material phaseVisualizationMaterial;
    public linkPhaseGeneratorSignalGenerator resetSignal, phaseSignal;
    private Material phaseVizMat;

    public float minBpm = 60f;
    public float maxBpm = 180f;
    public float questBaseCompensationMs = 80f;
    public float defaultBaseCompensationMs = 0f;
    public bool isRunning = true;
    public bool linkEnabled = true;

    public button playButton, rewindButton, nudgeForwardButton, nudgeBackwardButton, linkButton;

    static readonly float[] resetQuantumBarOptions = new float[] { 1f, 2f, 4f, 8f, 16f };
    const float nudgeMultiplier = 1.05f;
    const float fixedPhaseQuantum = 4f;
    const float compensationTrimRangeMs = 100f;
    const float launchBoundaryEpsilon = 0.02f;

    float bpm = 120f;
    float resetQuantumBars = 1f;
    float compensationMs = 0f;
    float nudgeBaseBpm = 120f;
    float pitchBendMult = 1f;
    float lastBpmDialPercent = -1f;
    bool wasNudging = false;
    bool launchPending = false;
    float lastLaunchPhase = 0f;
    bool suppressPlayButtonCallback = false;

    public override void Awake()
    {
        base.Awake();
        configureSignals();
        configureButtons();
        configureVisualization();
        linkEnabled = true;
        applyLinkEnabled();
        linkPhaseGeneratorService.SetPlaying(isRunning);
    }

    void Update()
    {
        if (phaseSignal == null || resetSignal == null)
            configureSignals();

        updateQuantum();
        updateCompensation();
        updateNudgeState();
        updateBpm();
        updateTransport();
        updateSignals();
        updateVisuals();
    }

    public override void hit(bool on, int ID)
    {
        if (ID == 0)
        {
            if (suppressPlayButtonCallback)
                return;

            if (on)
                requestQuantizedStart();
            else
                stopPlayback();
        }
        else if (ID == 1 && on)
        {
            linkPhaseGeneratorService.RequestBeatZero(launchQuantumBeats());
            triggerLocalReset();
            if (rewindButton != null) rewindButton.keyHit(false, false);
        }
        else if (ID == 3)
        {
            pitchBendMult = on ? 1f / nudgeMultiplier : 1f;
        }
        else if (ID == 4)
        {
            pitchBendMult = on ? nudgeMultiplier : 1f;
        }
    }

    public override InstrumentData GetData()
    {
        LinkPhaseGeneratorData data = new LinkPhaseGeneratorData();
        setDeviceType(data);
        GetTransformData(data);

        data.bpmPercent = bpmDial != null ? bpmDial.percent : Utils.map(bpm, minBpm, maxBpm, 0f, 1f);
        data.quantumPercent = quantumDial != null ? quantumDial.percent : resetQuantumBarsToPercent(resetQuantumBars);
        data.compensationPercent = compensationDial != null ? compensationDial.percent : compensationTrimToPercent(compensationMs - platformBaseCompensationMs());
        data.phaseJackID = phaseJack != null ? phaseJack.transform.GetInstanceID() : 0;
        data.isRunning = isRunning;
        data.linkEnabled = true;

        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        LinkPhaseGeneratorData data = d as LinkPhaseGeneratorData;
        base.Load(data, copyMode);

        if (phaseJack != null) phaseJack.SetID(data.phaseJackID, copyMode);
        if (bpmDial != null) bpmDial.setPercent(data.bpmPercent);
        if (quantumDial != null) quantumDial.setPercent(data.quantumPercent);
        if (compensationDial != null) compensationDial.setPercent(data.compensationPercent);

        isRunning = data.isRunning;
        linkEnabled = true;
        setPlayButtonState(isRunning);
        if (linkButton != null) linkButton.keyHit(true, false);
        bpm = percentToBpm(data.bpmPercent);
        resetQuantumBars = percentToResetQuantumBars(data.quantumPercent);
        compensationMs = platformBaseCompensationMs() + percentToCompensationTrim(data.compensationPercent);
        launchPending = false;
        applyLinkEnabled();
        linkPhaseGeneratorService.SetPlaying(isRunning);
    }

    void configureSignals()
    {
        linkPhaseGeneratorSignalGenerator[] signals = GetComponents<linkPhaseGeneratorSignalGenerator>();

        for (int i = 0; i < signals.Length; i++)
        {
            if (signals[i].mode == LinkPhaseOutputMode.Phase)
                phaseSignal = signals[i];
            else if (signals[i].mode == LinkPhaseOutputMode.Reset)
                resetSignal = signals[i];
        }

        if (phaseSignal == null)
        {
            phaseSignal = gameObject.AddComponent<linkPhaseGeneratorSignalGenerator>();
            phaseSignal.mode = LinkPhaseOutputMode.Phase;
        }

        if (resetSignal == null)
        {
            resetSignal = gameObject.AddComponent<linkPhaseGeneratorSignalGenerator>();
            resetSignal.mode = LinkPhaseOutputMode.Reset;
        }

        if (phaseJack != null) phaseJack.homesignal = phaseSignal;
        if (resetJack != null) resetJack.homesignal = resetSignal;

        phaseSignal.linkEnabled = linkEnabled;
        resetSignal.linkEnabled = linkEnabled;
    }

    void configureButtons()
    {
        button[] buttons = GetComponentsInChildren<button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            if (playButton == null && buttons[i].buttonID == 0) playButton = buttons[i];
            if (rewindButton == null && buttons[i].buttonID == 1) rewindButton = buttons[i];
            if (nudgeBackwardButton == null && buttons[i].buttonID == 3) nudgeBackwardButton = buttons[i];
            if (nudgeForwardButton == null && buttons[i].buttonID == 4) nudgeForwardButton = buttons[i];
        }
    }

    void configureVisualization()
    {
        if (phaseVisualizationRenderer != null && phaseVisualizationMaterial != null)
        {
            phaseVizMat = new Material(phaseVisualizationMaterial);
            phaseVisualizationRenderer.material = phaseVizMat;
        }
    }

    void applyLinkEnabled()
    {
        linkEnabled = true;
        linkPhaseGeneratorService.SetEnabled(linkEnabled, bpm);
    }

    void updateQuantum()
    {
        if (quantumDial != null)
            resetQuantumBars = resetQuantumBarOptions[quantumIndexFromPercent(quantumDial.percent)];

        if (quantumDisplay != null)
            quantumDisplay.text = resetQuantumBars.ToString("0") + "b";
    }

    void updateCompensation()
    {
        float trim = compensationDial != null ? percentToCompensationTrim(compensationDial.percent) : 0f;
        compensationMs = platformBaseCompensationMs() + trim;
    }

    void updateNudgeState()
    {
        if (nudgeForwardButton != null && nudgeForwardButton.isHit)
            pitchBendMult = nudgeMultiplier;
        else if (nudgeBackwardButton != null && nudgeBackwardButton.isHit)
            pitchBendMult = 1f / nudgeMultiplier;
        else if (nudgeForwardButton != null || nudgeBackwardButton != null)
            pitchBendMult = 1f;
    }

    void updateBpm()
    {
        float dialPercent = bpmDial != null ? bpmDial.percent : Utils.map(bpm, minBpm, maxBpm, 0f, 1f);
        float dialBpm = percentToBpm(dialPercent);
        bool dialChanged = lastBpmDialPercent < 0f || Mathf.Abs(dialPercent - lastBpmDialPercent) > 0.0005f;
        bool dialGrabbed = bpmDial != null && bpmDial.curState == manipObject.manipState.grabbed;
        bool nudging = !Mathf.Approximately(pitchBendMult, 1f);

        if (linkEnabled)
        {
            if (nudging)
            {
                if (!wasNudging)
                {
                    nudgeBaseBpm = dialGrabbed || (dialChanged && lastBpmDialPercent >= 0f) ? dialBpm : linkPhaseGeneratorService.GetTempo();
                    setBpmDial(nudgeBaseBpm);
                }

                if (dialGrabbed || (dialChanged && lastBpmDialPercent >= 0f))
                    nudgeBaseBpm = dialBpm;

                bpm = nudgeBaseBpm * pitchBendMult;
                linkPhaseGeneratorService.SetTempo(bpm);
            }
            else if (wasNudging)
            {
                bpm = nudgeBaseBpm;
                linkPhaseGeneratorService.SetTempo(bpm);
                setBpmDial(bpm);
            }
            else if (dialGrabbed || (dialChanged && lastBpmDialPercent >= 0f))
            {
                bpm = dialBpm;
                linkPhaseGeneratorService.SetTempo(bpm);
            }
            else
            {
                bpm = linkPhaseGeneratorService.GetTempo();
                setBpmDial(bpm);
            }
        }
        else
        {
            bpm = dialBpm * pitchBendMult;
        }

        if (bpmDisplay != null)
            bpmDisplay.text = bpm.ToString("0.00");

        wasNudging = nudging;
        lastBpmDialPercent = bpmDial != null ? bpmDial.percent : dialPercent;
    }

    void updateTransport()
    {
        if (linkEnabled)
        {
            bool linkPlaying = linkPhaseGeneratorService.IsPlaying();
            if (launchPending)
            {
                if (!linkPlaying)
                {
                    launchPending = false;
                    isRunning = false;
                    setPlayButtonState(false);
                }
                else if (launchBoundaryReached())
                {
                    launchPending = false;
                    startLocalPlayback(true);
                }
                else if (playButton != null && !playButton.isHit)
                {
                    setPlayButtonState(true);
                }
            }
            else if (linkPlaying != isRunning)
            {
                if (linkPlaying)
                {
                    if (shouldWaitForLaunchBoundary())
                    {
                        launchPending = true;
                        isRunning = false;
                        lastLaunchPhase = currentLaunchPhase();
                        setPlayButtonState(true);
                    }
                    else
                    {
                        startLocalPlayback(true);
                    }
                }
                else
                {
                    isRunning = false;
                    setPlayButtonState(false);
                }
            }
        }
    }

    void updateSignals()
    {
        if (phaseSignal == null || resetSignal == null)
            return;

        phaseSignal.bpm = bpm;
        resetSignal.bpm = bpm;
        phaseSignal.quantum = fixedPhaseQuantum;
        resetSignal.quantum = fixedPhaseQuantum;
        phaseSignal.compensationMs = compensationMs;
        resetSignal.compensationMs = compensationMs;
        phaseSignal.running = isRunning;
        resetSignal.running = isRunning;
        phaseSignal.linkEnabled = linkEnabled;
        resetSignal.linkEnabled = linkEnabled;
    }

    void updateVisuals()
    {
        float displayPhase = displayPhaseValue();

        if (rod != null && phaseSignal != null)
            rod.localRotation = Quaternion.Euler(0f, 0f, displayPhase * 360f);

        if (phaseVizMat != null)
            phaseVizMat.SetFloat("_Cutoff", 1f - displayPhase);

        if (linkStatusDisplay != null)
        {
            int peers = linkPhaseGeneratorService.LinkEnabled ? linkPhaseGeneratorService.PeerCount() : 0;
            linkStatusDisplay.text = peers == 0 ? "Local" : peers == 1 ? "1 Link" : peers.ToString("0") + " Links";
        }
    }

    float percentToBpm(float percent)
    {
        return Utils.map(percent, 0f, 1f, minBpm, maxBpm);
    }

    void requestQuantizedStart()
    {
        float launchQuantum = launchQuantumBeats();
        linkPhaseGeneratorService.SetPlayingQuantized(true, launchQuantum);

        if (shouldWaitForLaunchBoundary())
        {
            launchPending = true;
            isRunning = false;
            lastLaunchPhase = currentLaunchPhase();
            setPlayButtonState(true);
        }
        else
        {
            launchPending = false;
            startLocalPlayback(true);
        }
    }

    void stopPlayback()
    {
        launchPending = false;
        isRunning = false;
        linkPhaseGeneratorService.SetPlaying(false);
        setPlayButtonState(false);
    }

    void startLocalPlayback(bool reset)
    {
        isRunning = true;
        setPlayButtonState(true);
        if (reset) triggerLocalReset();
    }

    void setPlayButtonState(bool on)
    {
        if (playButton == null || playButton.isHit == on)
            return;

        suppressPlayButtonCallback = true;
        try
        {
            playButton.keyHit(on, false);
        }
        finally
        {
            suppressPlayButtonCallback = false;
        }
    }

    void triggerLocalReset()
    {
        OSLGlobalResetBus.Broadcast(this);
        if (phaseSignal != null) phaseSignal.rewindLocalPhase();
        if (resetSignal != null) resetSignal.triggerResetPulse();
    }

    float launchQuantumBeats()
    {
        return resetQuantumBars * fixedPhaseQuantum;
    }

    bool shouldWaitForLaunchBoundary()
    {
        if (!linkPhaseGeneratorService.LinkEnabled || linkPhaseGeneratorService.PeerCount() == 0)
            return false;

        return currentLaunchPhase() > launchBoundaryEpsilon;
    }

    bool launchBoundaryReached()
    {
        float phase = currentLaunchPhase();
        bool wrapped = phase < lastLaunchPhase - 0.5f;
        lastLaunchPhase = phase;
        return wrapped || phase <= launchBoundaryEpsilon;
    }

    float currentLaunchPhase()
    {
        return Mathf.Repeat(linkPhaseGeneratorService.PhaseAtTime(launchQuantumBeats()), 1f);
    }

    float displayPhaseValue()
    {
        if (linkPhaseGeneratorService.LinkEnabled)
        {
            float phase = phaseSignal != null ? phaseSignal.lastLinkPhase : 0f;
            float phaseOffset = compensationMs * 0.001f * Mathf.Max(1f, bpm) / 60f / fixedPhaseQuantum;
            return Mathf.Repeat(phase + phaseOffset, 1f);
        }

        return phaseSignal != null ? phaseSignal.lastPhase : 0f;
    }

    void setBpmDial(float targetBpm)
    {
        if (bpmDial == null || bpmDial.curState == manipObject.manipState.grabbed)
            return;

        bpmDial.setPercent(Utils.map(targetBpm, minBpm, maxBpm, 0f, 1f));
    }

    float platformBaseCompensationMs()
    {
        if (Application.platform != RuntimePlatform.Android)
            return defaultBaseCompensationMs;

        string model = SystemInfo.deviceModel.ToLowerInvariant();
        if (model.Contains("quest"))
            return questBaseCompensationMs;

        return defaultBaseCompensationMs;
    }

    float percentToCompensationTrim(float percent)
    {
        return Mathf.Lerp(-compensationTrimRangeMs, compensationTrimRangeMs, Mathf.Clamp01(percent));
    }

    float compensationTrimToPercent(float compensation)
    {
        return Mathf.InverseLerp(-compensationTrimRangeMs, compensationTrimRangeMs, compensation);
    }

    float resetQuantumBarsToPercent(float targetQuantum)
    {
        int bestIndex = 0;
        float bestDistance = Mathf.Abs(targetQuantum - resetQuantumBarOptions[bestIndex]);
        for (int i = 0; i < resetQuantumBarOptions.Length; i++)
        {
            float distance = Mathf.Abs(targetQuantum - resetQuantumBarOptions[i]);
            if (distance < bestDistance)
            {
                bestIndex = i;
                bestDistance = distance;
            }
        }

        return bestIndex / (float)(resetQuantumBarOptions.Length - 1);
    }

    float percentToResetQuantumBars(float percent)
    {
        return resetQuantumBarOptions[quantumIndexFromPercent(percent)];
    }

    int quantumIndexFromPercent(float percent)
    {
        return Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(percent) * (resetQuantumBarOptions.Length - 1)), 0, resetQuantumBarOptions.Length - 1);
    }
}

[XmlType("LinkPhaseGeneratorData")]
public class LinkPhaseGeneratorData : InstrumentData
{
    public float bpmPercent;
    public float quantumPercent = 0f;
    public float compensationPercent = 0.5f;
    public int phaseJackID;
    public bool isRunning = true;
    public bool linkEnabled = true;
}
