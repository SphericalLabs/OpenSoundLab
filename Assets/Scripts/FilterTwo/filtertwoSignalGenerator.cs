using UnityEngine;
using System.Collections;

public class filtertwoSignalGenerator : signalGenerator
{
    const float baseFrequency = 261.6256f;
    const float excitationThreshold = 0.985f;
    const float excitationAmount = 0.0001f;
    const float silenceThreshold = 0.0002f;

    public enum filterMode
    {
        LP,
        HP,
        BP,
        Notch
    }

    public signalGenerator incoming, freqIncoming;

    public float cutoffFrequency = 0f;
    public float resonance = 0.5f;
    public filterMode curMode = filterMode.LP;

    float lastCutoffFrequency = 0f;
    float lastResonance = 0.5f;

    float[] frequencyBuffer;
    float[] lowEq;
    float[] bandEq;

    bool excitationQueued = false;

    public override void Awake()
    {
        base.Awake();
        frequencyBuffer = new float[MAX_BUFFER_LENGTH];
        lowEq = new float[2];
        bandEq = new float[2];
        lastResonance = resonance;
    }

    public void queueExcitation()
    {
        excitationQueued = true;
    }

    public float getModePercent()
    {
        switch (curMode)
        {
            case filterMode.HP:
                return 1f / 3f;
            case filterMode.BP:
                return 2f / 3f;
            case filterMode.Notch:
                return 1f;
            default:
                return 0f;
        }
    }

    public void setModeFromPercent(float percent)
    {
        switch (Mathf.RoundToInt(Mathf.Clamp01(percent) * 3f))
        {
            case 1:
                curMode = filterMode.HP;
                break;
            case 2:
                curMode = filterMode.BP;
                break;
            case 3:
                curMode = filterMode.Notch;
                break;
            default:
                curMode = filterMode.LP;
                break;
        }
    }

    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        if (!recursionCheckPre()) return;

        ensureBuffers(buffer.Length, channels);
        fillModulationBuffer(dspTime, channels, buffer.Length);

        if (incoming != null)
            incoming.processBuffer(buffer, dspTime, channels);
        else
            System.Array.Clear(buffer, 0, buffer.Length);

        bool crossedThreshold = lastResonance < excitationThreshold && resonance >= excitationThreshold;
        bool shouldExcite = excitationQueued || crossedThreshold;
        if (shouldExcite && isSilent(buffer))
            addExcitation(buffer, channels);

        int frameCount = buffer.Length / channels;
        for (int frame = 0; frame < frameCount; frame++)
        {
            float t = frameCount > 1 ? frame / (float)(frameCount - 1) : 1f;
            float cutoff = Mathf.Lerp(lastCutoffFrequency, cutoffFrequency, t);
            float currentResonance = Mathf.Lerp(lastResonance, resonance, t);
            float cutoffHz = getCutoffHz(cutoff, frequencyBuffer[frame * channels]);

            processFrame(buffer, frame * channels, channels, cutoffHz, currentResonance);
        }

        lastCutoffFrequency = cutoffFrequency;
        lastResonance = resonance;
        excitationQueued = false;
        recursionCheckPost();
    }

    void ensureBuffers(int bufferLength, int channels)
    {
        if (frequencyBuffer.Length != bufferLength)
            System.Array.Resize(ref frequencyBuffer, bufferLength);

        if (lowEq.Length != channels)
            System.Array.Resize(ref lowEq, channels);

        if (bandEq.Length != channels)
            System.Array.Resize(ref bandEq, channels);
    }

    void fillModulationBuffer(double dspTime, int channels, int bufferLength)
    {
        System.Array.Clear(frequencyBuffer, 0, bufferLength);
        if (freqIncoming != null)
            freqIncoming.processBuffer(frequencyBuffer, dspTime, channels);
    }

    bool isSilent(float[] buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            if (Mathf.Abs(buffer[i]) > silenceThreshold)
                return false;
        }
        return true;
    }

    void addExcitation(float[] buffer, int channels)
    {
        for (int i = 0; i < channels && i < buffer.Length; i++)
            buffer[i] += excitationAmount;
    }

    float getCutoffHz(float cutoff, float modulation)
    {
        float octaveOffset = (Mathf.Clamp(modulation, -1f, 1f) + cutoff) * 10f;
        float cutoffHz = baseFrequency * Mathf.Pow(2f, octaveOffset);
        return Mathf.Clamp(cutoffHz, 5f, (float)_sampleRate * 0.45f);
    }

    void processFrame(float[] buffer, int index, int channels, float cutoffHz, float currentResonance)
    {
        float g = Mathf.Tan(Mathf.PI * cutoffHz / (float)_sampleRate);
        float k = getDamping(currentResonance);
        float a1 = 1f / (1f + g * (g + k));
        float a2 = g * a1;
        float a3 = g * a2;
        float drive = 1.1f + currentResonance * 1.2f;

        for (int channel = 0; channel < channels; channel++)
        {
            float inputSample = softClip(buffer[index + channel] * (1f + currentResonance * 0.25f));
            float v3 = inputSample - lowEq[channel];
            float band = a1 * bandEq[channel] + a2 * v3;
            band = softClip(band * drive) / drive;

            float low = lowEq[channel] + a2 * bandEq[channel] + a3 * v3;
            low = softClip(low * drive) / drive;

            float high = inputSample - k * band - low;
            float notch = high + low;

            bandEq[channel] = 2f * band - bandEq[channel];
            lowEq[channel] = 2f * low - lowEq[channel];

            switch (curMode)
            {
                case filterMode.HP:
                    buffer[index + channel] = high;
                    break;
                case filterMode.BP:
                    buffer[index + channel] = band;
                    break;
                case filterMode.Notch:
                    buffer[index + channel] = notch;
                    break;
                default:
                    buffer[index + channel] = low;
                    break;
            }
        }
    }

    float getDamping(float currentResonance)
    {
        float clampedResonance = Mathf.Clamp01(currentResonance);
        float k = Mathf.Lerp(1.9f, 0.08f, clampedResonance * clampedResonance);

        if (clampedResonance > excitationThreshold)
        {
            float tail = (clampedResonance - excitationThreshold) / (1f - excitationThreshold);
            k = Mathf.Lerp(k, 0f, tail * tail);
        }

        return Mathf.Max(0f, k);
    }

    float softClip(float value)
    {
        float clamped = Mathf.Clamp(value, -1.5f, 1.5f);
        return clamped - 0.14814815f * clamped * clamped * clamped;
    }
}
