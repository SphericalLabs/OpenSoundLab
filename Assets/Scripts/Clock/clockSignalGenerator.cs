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

    public override void Awake()
    {
        base.Awake();
        setBPM(bpm);
    }

    private double lastProcessedDspTime = -1;
    private float[] cachedBuffer = new float[2048]; // MAX_BUFFER_LENGTH from signalGenerator

    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        if (dspTime == lastProcessedDspTime)
        {
            int len = Mathf.Min(buffer.Length, cachedBuffer.Length);
            System.Array.Copy(cachedBuffer, buffer, len);
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
}
