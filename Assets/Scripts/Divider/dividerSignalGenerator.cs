using UnityEngine;

public class dividerSignalGenerator : signalGenerator
{
    public signalGenerator phaseInput;
    public signalGenerator resetInput;
    public float resetThreshold = 0.5f;
    public int resolutionIndex = 3; // default 8th notes?
    public float swingVal = 0.5f;

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

    public void Awake()
    {
        _beatManager = ScriptableObject.CreateInstance<beatTracker>();
        _beatManager.setTrigger(() => clockTriggered = true);
        _beatManager.toggleMC(false); // We drive it manually
    }

    public void UpdateSettings(int res, float swing)
    {
        resolutionIndex = res;
        swingVal = swing;
        _beatManager.updateBeatNoTriplets(resolutionIndex);
        _beatManager.updateSwing(swingVal);
    }

    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        if (phaseInput == null) return;

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

        phaseInput.processBuffer(phaseBuffer, dspTime, channels);
        if (resetInput != null) resetInput.processBuffer(resetBuffer, dspTime, channels);

        for (int n = 0; n < buffer.Length; n += channels)
        {
            clockTriggered = false;

            float resetSample = resetInput != null ? resetBuffer[n] : 0f;
            bool resetEdge = resetInput != null && resetSample >= resetThreshold && lastResetSample < resetThreshold;
            if (resetEdge)
            {
                trackedPhase = 0f;
                hasPhaseSample = false;
                _beatManager.beatResetEvent();
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
                if (phaseDelta < 0f) phaseDelta = 0f;
                trackedPhase += phaseDelta;
                if (trackedPhase >= 1f)
                {
                    trackedPhase -= Mathf.Floor(trackedPhase);
                }
            }

            lastPhaseSample = phaseSample;
            _beatManager.beatUpdateEvent(trackedPhase);

            buffer[n] = clockTriggered ? 1f : 0f;

            if (channels > 1) buffer[n + 1] = buffer[n];
        }

        lastProcessedDspTime = dspTime;
        System.Array.Copy(buffer, cachedBuffer, buffer.Length);
    }

    private void OnDestroy()
    {
        if (_beatManager != null) Destroy(_beatManager);
    }
}
