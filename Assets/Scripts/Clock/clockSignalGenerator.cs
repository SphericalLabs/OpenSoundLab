using UnityEngine;

public class clockSignalGenerator : signalGenerator
{
    public signalGenerator phaseInput;
    public signalGenerator resetInput;
    public float resetThreshold = 0.5f;
    public int resolutionIndex = 3; // default 8th notes?
    public float swingVal = 0.5f;
    public int cycleDivision = 1;
    public float progressToNextTrigger = 0f;
    public bool autorunning = false;

    private beatTracker _beatManager;
    private double lastProcessedDspTime = -1;
    private float[] cachedBuffer = new float[2048];
    private float[] phaseBuffer = new float[2048];
    private float[] resetBuffer = new float[2048];

    private bool clockTriggered = false;
    private float lastPhaseSample = 0f;
    private float lastResetSample = 0f;
    private float trackedPhase = 0f;
    private bool hasPhaseSample = false;
    private int cycleCounter = 0;
    private bool settingsInitialized = false;
    private int lastResolutionIndex = -1;
    private float lastSwingVal = -1f;
    private bool globalResetQueued = false;

    public void Awake()
    {
        _beatManager = ScriptableObject.CreateInstance<beatTracker>();
        _beatManager.setTrigger(() => clockTriggered = true);
        _beatManager.toggleMC(false); // We drive it manually
        UpdateSettings(resolutionIndex, swingVal);
    }

    public void UpdateSettings(int res, float swing)
    {
        resolutionIndex = res;
        swingVal = swing;
        if (settingsInitialized &&
            resolutionIndex == lastResolutionIndex &&
            Mathf.Abs(swingVal - lastSwingVal) < 0.0001f)
        {
            return;
        }
        settingsInitialized = true;
        lastResolutionIndex = resolutionIndex;
        lastSwingVal = swingVal;
        _beatManager.updateBeatNoTriplets(resolutionIndex);
        _beatManager.updateSwing(swingVal);
    }

    public void requestGlobalReset()
    {
        globalResetQueued = true;
    }

    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        if (globalResetQueued)
        {
            applyResetState();
            globalResetQueued = false;
        }

        if (phaseInput == null)
        {
            progressToNextTrigger = 0f;
            return;
        }

        if (cachedBuffer.Length != buffer.Length)
        {
            System.Array.Resize(ref cachedBuffer, buffer.Length);
        }

        if (phaseBuffer.Length != buffer.Length)
        {
            System.Array.Resize(ref phaseBuffer, buffer.Length);
        }

        if (resetBuffer.Length != buffer.Length)
        {
            System.Array.Resize(ref resetBuffer, buffer.Length);
        }

        if (dspTime == lastProcessedDspTime)
        {
            System.Array.Copy(cachedBuffer, buffer, buffer.Length);
            return;
        }

        if (phaseInput != null) phaseInput.processBuffer(phaseBuffer, dspTime, channels);
        if (resetInput != null) resetInput.processBuffer(resetBuffer, dspTime, channels);

        int effectiveCycleDivision = cycleDivision < 1 ? 1 : cycleDivision;
        if (effectiveCycleDivision == 1)
        {
            cycleCounter = 0;
        }
        else if (cycleCounter >= effectiveCycleDivision)
        {
            cycleCounter %= effectiveCycleDivision;
        }

        float lastBeatPhase = 0f;
        for (int n = 0; n < buffer.Length; n += channels)
        {
            clockTriggered = false;

            float resetSample = resetInput != null ? resetBuffer[n] : 0f;
            bool resetEdge = resetInput != null && resetSample >= resetThreshold && lastResetSample < resetThreshold;
            if (resetEdge)
            {
                applyResetState();
            }
            lastResetSample = resetSample;

            float phaseSample = phaseBuffer[n];
            if (!hasPhaseSample)
            {
                hasPhaseSample = true;
                trackedPhase = 0f;
            }
            else
            {
                float phaseDelta = phaseSample - lastPhaseSample;
                if (phaseDelta < 0f) phaseDelta += 1f;
                trackedPhase += phaseDelta;
                if (trackedPhase >= 1f)
                {
                    int wrapCount = Mathf.FloorToInt(trackedPhase);
                    trackedPhase -= wrapCount;
                    cycleCounter = (cycleCounter + wrapCount) % effectiveCycleDivision;
                }
            }

            lastPhaseSample = phaseSample;
            float beatPhase = trackedPhase;
            if (effectiveCycleDivision > 1)
            {
                beatPhase = (cycleCounter + trackedPhase) / effectiveCycleDivision;
            }
            lastBeatPhase = beatPhase;
            _beatManager.beatUpdateEvent(beatPhase);

            buffer[n] = clockTriggered ? 1f : 0f;

            if (channels > 1) buffer[n + 1] = buffer[n];
        }

        progressToNextTrigger = _beatManager.getStepProgress(lastBeatPhase);

        lastProcessedDspTime = dspTime;
        System.Array.Copy(buffer, cachedBuffer, buffer.Length);
    }

    void applyResetState()
    {
        trackedPhase = 0f;
        hasPhaseSample = false;
        cycleCounter = 0;
        _beatManager.beatResetEvent();
    }

    private void OnDestroy()
    {
        if (_beatManager != null) Destroy(_beatManager);
    }

    private void OnAudioFilterRead(float[] buffer, int channels)
    {
        if (autorunning)
        {
            processBuffer(buffer, AudioSettings.dspTime, channels);
        }

        // Audio is being muted in the AudioSource
    }
}
