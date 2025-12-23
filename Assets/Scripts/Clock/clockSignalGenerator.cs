using UnityEngine;
using System.Collections;

public class clockSignalGenerator : signalGenerator
{
    public enum ClockOutputMode { Phase, Pulse, Reset }
    public ClockOutputMode mode = ClockOutputMode.Phase;

    public float bpm = 120;
    public bool running = true;

    public double measurePeriod = 2; // 1 bar
    public double _measurePhase;
    bool resetPulseQueued = false;

    public void setBPM(float b)
    {
        float curCycle = 0f;
        if (measurePeriod > 0)
        {
            curCycle = (float)(_measurePhase / measurePeriod);
        }
        bpm = b;
        measurePeriod = 240f / bpm; // 4 beats = 1 bar
        _measurePhase = curCycle * measurePeriod;
    }

    public override void Awake()
    {
        base.Awake();
        setBPM(bpm);
    }

    private double lastProcessedDspTime = -1;
    private float[] cachedBuffer = new float[2048]; // MAX_BUFFER_LENGTH from signalGenerator

    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        if (cachedBuffer.Length != buffer.Length)
        {
            System.Array.Resize(ref cachedBuffer, buffer.Length);
        }

        if (resetPulseQueued)
        {
            System.Array.Clear(buffer, 0, buffer.Length);
            for (int c = 0; c < channels && c < buffer.Length; c++)
            {
                buffer[c] = 1f;
            }
            resetPulseQueued = false;
            lastProcessedDspTime = dspTime;
            System.Array.Copy(buffer, cachedBuffer, buffer.Length);
            return;
        }

        if (dspTime == lastProcessedDspTime)
        {
            System.Array.Copy(cachedBuffer, buffer, buffer.Length);
            return;
        }

        if (!running)
        {
            System.Array.Clear(buffer, 0, buffer.Length);
            lastProcessedDspTime = dspTime;
            System.Array.Copy(buffer, cachedBuffer, buffer.Length);
            return;
        }

        for (int n = 0; n < buffer.Length; n += channels)
        {
            float curCycle = (float)(_measurePhase / measurePeriod);

            switch (mode)
            {
                case ClockOutputMode.Phase:
                    buffer[n] = curCycle;
                    break;
                case ClockOutputMode.Pulse:
                    // 8 pulses per bar (8th notes)
                    float step = Mathf.Repeat(curCycle * 8, 1);
                    buffer[n] = step < 0.1f ? 1f : 0f;
                    break;
                case ClockOutputMode.Reset:
                    buffer[n] = curCycle < 0.01f ? 1f : 0f;
                    break;
            }

            if (channels > 1) buffer[n + 1] = buffer[n];

            _measurePhase += _sampleDuration;
            if (_measurePhase >= measurePeriod) _measurePhase -= measurePeriod;
        }

        lastProcessedDspTime = dspTime;
        System.Array.Copy(buffer, cachedBuffer, buffer.Length);
    }

    private void OnAudioFilterRead(float[] buffer, int channels)
    {
        processBuffer(buffer, AudioSettings.dspTime, channels);
    }

    public void ResetPhase()
    {
        _measurePhase = 0;
        lastProcessedDspTime = -1; // Force re-calculate on next pull
    }

    public void triggerResetPulse()
    {
        resetPulseQueued = true;
        _measurePhase = 0;
        lastProcessedDspTime = -1;
    }
}
