using UnityEngine;

public enum LinkPhaseOutputMode
{
    Phase,
    Reset
}

public class linkPhaseGeneratorSignalGenerator : signalGenerator
{
    public LinkPhaseOutputMode mode = LinkPhaseOutputMode.Phase;
    public float bpm = 120f;
    public bool running = true;
    public float quantum = 4f;
    public float compensationMs = 0f;
    public bool linkEnabled = true;
    public double measurePeriod = 2.0;
    public double _measurePhase = 0.0;
    public float lastPhase = 0f;
    public float lastLinkPhase = 0f;

    bool resetQueued = false;
    float[] cachedBuffer;
    double lastDspTime = -1.0;

    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        if (cachedBuffer != null && cachedBuffer.Length == buffer.Length && dspTime == lastDspTime && !resetQueued)
        {
            cachedBuffer.CopyTo(buffer, 0);
            return;
        }

        if (cachedBuffer == null || cachedBuffer.Length != buffer.Length)
            cachedBuffer = new float[buffer.Length];

        if (mode == LinkPhaseOutputMode.Reset)
            processReset(buffer, channels);
        else
            processPhase(buffer, channels);

        buffer.CopyTo(cachedBuffer, 0);
        lastDspTime = dspTime;
    }

    public void triggerResetPulse()
    {
        resetQueued = true;
    }

    public void rewindLocalPhase()
    {
        _measurePhase = 0.0;
        lastPhase = 0f;
    }

    void processReset(float[] buffer, int channels)
    {
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = 0f;

        if (!resetQueued)
            return;

        int pulseChannels = Mathf.Min(channels, buffer.Length);
        for (int i = 0; i < pulseChannels; i++)
            buffer[i] = 1f;

        resetQueued = false;
    }

    void processPhase(float[] buffer, int channels)
    {
        float safeQuantum = Mathf.Max(1f, quantum);
        float safeBpm = Mathf.Max(1f, bpm);
        bool useLink = linkEnabled && linkPhaseGeneratorService.LinkEnabled;
        bool shouldRun = running;
        float phase = lastPhase;

        if (useLink)
        {
            LinkPhaseGeneratorState state = linkPhaseGeneratorService.CaptureAudioState(safeQuantum);
            if (state.linkEnabled)
            {
                lastLinkPhase = Mathf.Repeat(state.phase, 1f);
                safeBpm = Mathf.Max(1f, state.bpm);
                shouldRun = running && state.isPlaying;
                if (shouldRun)
                {
                    phase = lastLinkPhase;
                    phase = applyCompensation(phase, safeBpm, safeQuantum);
                }
            }
            else
            {
                useLink = false;
            }
        }

        if (!useLink)
        {
            phase = (float)_measurePhase;
            measurePeriod = 60.0 * safeQuantum / safeBpm;
        }

        float phaseStep = shouldRun ? (float)(_sampleDuration * safeBpm / 60.0 / safeQuantum) : 0f;

        for (int i = 0; i < buffer.Length; i += channels)
        {
            for (int c = 0; c < channels && i + c < buffer.Length; c++)
                buffer[i + c] = phase;

            if (shouldRun)
            {
                phase += phaseStep;
                if (phase >= 1f)
                    phase -= Mathf.Floor(phase);
            }
        }

        lastPhase = phase;
        if (!useLink)
            _measurePhase = phase;
    }

    float applyCompensation(float phase, float safeBpm, float safeQuantum)
    {
        float phaseOffset = compensationMs * 0.001f * safeBpm / 60f / safeQuantum;
        return Mathf.Repeat(phase + phaseOffset, 1f);
    }
}
