using UnityEngine;
using System.Collections;

public class filterthreeSignalGenerator : signalGenerator
{
    public signalGenerator incoming;

    public float lowCutPercent = 0f;
    public float lowShelfFrequencyPercent = 0.3f;
    public float lowShelfGainPercent = 0.5f;
    public float bellOneFrequencyPercent = 0.35f;
    public float bellOneGainPercent = 0.5f;
    public float bellOneQPercent = 0.35f;
    public float bellTwoFrequencyPercent = 0.65f;
    public float bellTwoGainPercent = 0.5f;
    public float bellTwoQPercent = 0.35f;
    public float highShelfFrequencyPercent = 0.7f;
    public float highShelfGainPercent = 0.5f;
    public float highCutPercent = 1f;

    BiquadSection[] sections;

    public override void Awake()
    {
        base.Awake();
        sections = new BiquadSection[6];
        for (int i = 0; i < sections.Length; i++)
            sections[i] = new BiquadSection();
    }

    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        if (!recursionCheckPre()) return;

        ensureChannels(channels);
        updateSections();

        if (incoming != null)
            incoming.processBuffer(buffer, dspTime, channels);
        else
            System.Array.Clear(buffer, 0, buffer.Length);

        for (int i = 0; i < buffer.Length; i += channels)
        {
            for (int channel = 0; channel < channels; channel++)
            {
                float sample = buffer[i + channel];
                for (int section = 0; section < sections.Length; section++)
                    sample = sections[section].processSample(sample, channel);

                buffer[i + channel] = sample;
            }
        }

        recursionCheckPost();
    }

    void ensureChannels(int channels)
    {
        for (int i = 0; i < sections.Length; i++)
            sections[i].ensureChannels(channels);
    }

    void updateSections()
    {
        float sampleRate = (float)_sampleRate;
        sections[0].setHighpass(sampleRate, mapFrequency(lowCutPercent, 20f, 2000f), 0.7071f);
        sections[1].setLowShelf(sampleRate, mapFrequency(lowShelfFrequencyPercent, 40f, 1600f), mapGain(lowShelfGainPercent), 1f);
        sections[2].setPeak(sampleRate, mapFrequency(bellOneFrequencyPercent, 60f, 4000f), mapQ(bellOneQPercent), mapGain(bellOneGainPercent));
        sections[3].setPeak(sampleRate, mapFrequency(bellTwoFrequencyPercent, 300f, 12000f), mapQ(bellTwoQPercent), mapGain(bellTwoGainPercent));
        sections[4].setHighShelf(sampleRate, mapFrequency(highShelfFrequencyPercent, 1200f, 16000f), mapGain(highShelfGainPercent), 1f);
        sections[5].setLowpass(sampleRate, mapFrequency(highCutPercent, 1000f, 20000f), 0.7071f);
    }

    float mapFrequency(float percent, float minFrequency, float maxFrequency)
    {
        float clampedPercent = Mathf.Clamp01(percent);
        float minLog = Mathf.Log(minFrequency);
        float maxLog = Mathf.Log(maxFrequency);
        return Mathf.Exp(Mathf.Lerp(minLog, maxLog, clampedPercent));
    }

    float mapGain(float percent)
    {
        return Utils.map(Mathf.Clamp01(percent), 0f, 1f, -18f, 18f);
    }

    float mapQ(float percent)
    {
        float clampedPercent = Mathf.Clamp01(percent);
        float minLog = Mathf.Log(0.35f);
        float maxLog = Mathf.Log(10f);
        return Mathf.Exp(Mathf.Lerp(minLog, maxLog, clampedPercent));
    }
}

class BiquadSection
{
    float b0 = 1f;
    float b1 = 0f;
    float b2 = 0f;
    float a1 = 0f;
    float a2 = 0f;

    float[] z1 = new float[2];
    float[] z2 = new float[2];

    public void ensureChannels(int channels)
    {
        if (z1.Length != channels)
            System.Array.Resize(ref z1, channels);

        if (z2.Length != channels)
            System.Array.Resize(ref z2, channels);
    }

    public float processSample(float input, int channel)
    {
        float output = b0 * input + z1[channel];
        z1[channel] = b1 * input - a1 * output + z2[channel];
        z2[channel] = b2 * input - a2 * output;
        return output;
    }

    public void setLowpass(float sampleRate, float frequency, float q)
    {
        float w0 = 2f * Mathf.PI * frequency / sampleRate;
        float cosW0 = Mathf.Cos(w0);
        float alpha = Mathf.Sin(w0) / (2f * q);
        float a0 = 1f + alpha;
        normalize((1f - cosW0) * 0.5f, 1f - cosW0, (1f - cosW0) * 0.5f, a0, -2f * cosW0, 1f - alpha);
    }

    public void setHighpass(float sampleRate, float frequency, float q)
    {
        float w0 = 2f * Mathf.PI * frequency / sampleRate;
        float cosW0 = Mathf.Cos(w0);
        float alpha = Mathf.Sin(w0) / (2f * q);
        float a0 = 1f + alpha;
        normalize((1f + cosW0) * 0.5f, -(1f + cosW0), (1f + cosW0) * 0.5f, a0, -2f * cosW0, 1f - alpha);
    }

    public void setPeak(float sampleRate, float frequency, float q, float gainDb)
    {
        float a = Mathf.Pow(10f, gainDb / 40f);
        float w0 = 2f * Mathf.PI * frequency / sampleRate;
        float cosW0 = Mathf.Cos(w0);
        float alpha = Mathf.Sin(w0) / (2f * q);
        float a0 = 1f + alpha / a;
        normalize(1f + alpha * a, -2f * cosW0, 1f - alpha * a, a0, -2f * cosW0, 1f - alpha / a);
    }

    public void setLowShelf(float sampleRate, float frequency, float gainDb, float slope)
    {
        float a = Mathf.Pow(10f, gainDb / 40f);
        float w0 = 2f * Mathf.PI * frequency / sampleRate;
        float cosW0 = Mathf.Cos(w0);
        float alpha = getShelfAlpha(w0, a, slope);
        float twoRootAAlpha = 2f * Mathf.Sqrt(a) * alpha;
        float a0 = (a + 1f) + (a - 1f) * cosW0 + twoRootAAlpha;

        normalize(
            a * ((a + 1f) - (a - 1f) * cosW0 + twoRootAAlpha),
            2f * a * ((a - 1f) - (a + 1f) * cosW0),
            a * ((a + 1f) - (a - 1f) * cosW0 - twoRootAAlpha),
            a0,
            -2f * ((a - 1f) + (a + 1f) * cosW0),
            (a + 1f) + (a - 1f) * cosW0 - twoRootAAlpha);
    }

    public void setHighShelf(float sampleRate, float frequency, float gainDb, float slope)
    {
        float a = Mathf.Pow(10f, gainDb / 40f);
        float w0 = 2f * Mathf.PI * frequency / sampleRate;
        float cosW0 = Mathf.Cos(w0);
        float alpha = getShelfAlpha(w0, a, slope);
        float twoRootAAlpha = 2f * Mathf.Sqrt(a) * alpha;
        float a0 = (a + 1f) - (a - 1f) * cosW0 + twoRootAAlpha;

        normalize(
            a * ((a + 1f) + (a - 1f) * cosW0 + twoRootAAlpha),
            -2f * a * ((a - 1f) + (a + 1f) * cosW0),
            a * ((a + 1f) + (a - 1f) * cosW0 - twoRootAAlpha),
            a0,
            2f * ((a - 1f) - (a + 1f) * cosW0),
            (a + 1f) - (a - 1f) * cosW0 - twoRootAAlpha);
    }

    float getShelfAlpha(float w0, float a, float slope)
    {
        float sinW0 = Mathf.Sin(w0);
        float shelfTerm = (a + 1f / a) * (1f / slope - 1f) + 2f;
        return sinW0 * 0.5f * Mathf.Sqrt(Mathf.Max(0f, shelfTerm));
    }

    void normalize(float inB0, float inB1, float inB2, float a0, float inA1, float inA2)
    {
        b0 = inB0 / a0;
        b1 = inB1 / a0;
        b2 = inB2 / a0;
        a1 = inA1 / a0;
        a2 = inA2 / a0;
    }
}
