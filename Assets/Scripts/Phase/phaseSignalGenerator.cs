using UnityEngine;
using System.Collections;

public class phaseSignalGenerator : signalGenerator
{
    public enum PhaseOutputMode { Phase = 0, Reset = 2 }
    public PhaseOutputMode mode = PhaseOutputMode.Phase;

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
            if (mode == PhaseOutputMode.Reset)
            {
                buffer[0] = buffer[1] = 1f;
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
            if (mode == PhaseOutputMode.Phase)
            {
                float curCycle = 0f;
                if (measurePeriod > 0)
                {
                    curCycle = (float)(_measurePhase / measurePeriod);
                }

                for (int n = 0; n < buffer.Length; n += channels)
                {
                    buffer[n] = curCycle;
                    if (channels > 1) buffer[n + 1] = buffer[n];
                }
            }
            else
            {
                System.Array.Clear(buffer, 0, buffer.Length);
            }
            lastProcessedDspTime = dspTime;
            System.Array.Copy(buffer, cachedBuffer, buffer.Length);
            return;
        }

        for (int n = 0; n < buffer.Length; n += channels)
        {
            float curCycle = (float)(_measurePhase / measurePeriod);

            switch (mode)
            {
                case PhaseOutputMode.Phase:
                    buffer[n] = curCycle;
                    break;
                case PhaseOutputMode.Reset:
                    buffer[n] = 0f;
                    break;
                default:
                    buffer[n] = 0f;
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
        // Two phase generators live on the same GameObject; Unity calls each in order,
        // so later ones overwrite earlier ones in this buffer. The dspTime guard above
        // makes each generator fill its buffer only once per audio slice, and we zero
        // here so none of those writes reach the speakers.
        System.Array.Clear(buffer, 0, buffer.Length);
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
