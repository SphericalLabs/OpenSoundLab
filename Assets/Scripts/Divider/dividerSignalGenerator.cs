using UnityEngine;

public class dividerSignalGenerator : signalGenerator
{
    public signalGenerator phaseInput;
    public int resolutionIndex = 3; // default 8th notes?
    public float swingVal = 0.5f;

    private beatTracker _beatManager;
    private double lastProcessedDspTime = -1;
    private float[] cachedBuffer = new float[2048];
    private float[] phaseBuffer = new float[2048];

    private bool clockTriggered = false;
    private bool resetTriggered = false;
    private float lastPhaseSample = 0f;
    private bool hasPhaseSample = false;

    public enum OutputMode { Clock, Reset }
    public OutputMode mode = OutputMode.Clock;

    public void Awake()
    {
        _beatManager = ScriptableObject.CreateInstance<beatTracker>();
        _beatManager.setTriggers(() => clockTriggered = true, () => resetTriggered = true);
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

        if (dspTime == lastProcessedDspTime)
        {
            System.Array.Copy(cachedBuffer, buffer, buffer.Length);
            return;
        }

        phaseInput.processBuffer(phaseBuffer, dspTime, channels);

        for (int n = 0; n < buffer.Length; n += channels)
        {
            clockTriggered = false;
            resetTriggered = false;

            // drive the beat tracker with the phase
            float phaseSample = phaseBuffer[n];
            if (hasPhaseSample && phaseSample < lastPhaseSample)
            {
                _beatManager.beatResetEvent();
            }
            else if (!hasPhaseSample)
            {
                hasPhaseSample = true;
            }

            lastPhaseSample = phaseSample;
            _beatManager.beatUpdateEvent(phaseSample);

            if (mode == OutputMode.Clock)
                buffer[n] = clockTriggered ? 1f : 0f;
            else
                buffer[n] = resetTriggered ? 1f : 0f;

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
