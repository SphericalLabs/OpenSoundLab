using UnityEngine;
using System.Collections;

public class clockSignalGenerator : signalGenerator
{
    public enum ClockOutputMode { Phase, Pulse, Reset }
    public ClockOutputMode mode = ClockOutputMode.Phase;

    public float bpm = 120;
    public bool running = true;

    public double measurePeriod = 4; // 2 bars
    public double _measurePhase;

    public void setBPM(float b)
    {
        bpm = b;
        measurePeriod = 480f / bpm; // 8 beats = 2 bars
    }

    public void ResetPhase()
    {
        _measurePhase = 0;
    }

    public override void Awake()
    {
        base.Awake();
        setBPM(bpm);
    }

    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        if (!running) return;

        for (int n = 0; n < buffer.Length; n += channels)
        {
            float curCycle = (float)(_measurePhase / measurePeriod);

            switch (mode)
            {
                case ClockOutputMode.Phase:
                    buffer[n] = curCycle;
                    break;
                case ClockOutputMode.Pulse:
                    // 32 pulses per 2-bar cycle (16th notes)
                    float step = Mathf.Repeat(curCycle * 32, 1);
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
    }
}
