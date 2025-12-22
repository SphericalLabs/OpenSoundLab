using UnityEngine;

public class dividerSignalGenerator : signalGenerator
{
    public signalGenerator phaseInput;
    public int resolutionIndex = 3; // default 8th notes?
    public float swingVal = 0.5f;

    private beatTracker _beatManager;
    private float[] phaseBuffer;

    private bool clockTriggered = false;
    private bool resetTriggered = false;

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

        if (phaseBuffer == null || phaseBuffer.Length != buffer.Length)
            phaseBuffer = new float[buffer.Length];

        phaseInput.processBuffer(phaseBuffer, dspTime, channels);

        for (int n = 0; n < buffer.Length; n += channels)
        {
            clockTriggered = false;
            resetTriggered = false;

            // drive the beat tracker with the phase
            _beatManager.beatUpdateEvent(phaseBuffer[n]);

            if (mode == OutputMode.Clock)
                buffer[n] = clockTriggered ? 1f : 0f;
            else
                buffer[n] = resetTriggered ? 1f : 0f;

            if (channels > 1) buffer[n + 1] = buffer[n];
        }
    }

    private void OnDestroy()
    {
        if (_beatManager != null) Destroy(_beatManager);
    }
}
