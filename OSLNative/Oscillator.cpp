#include "Oscillator.h"
#include "util.h"

#include <cmath>

#if defined(__ARM_NEON) || defined(__ARM_NEON__)
#include <arm_neon.h>
#endif

namespace {

constexpr float kPi = 3.14159265f;

static inline float wrapPhase01(float phase) {
    phase -= floorf(phase);
    return phase < 0.f ? phase + 1.f : phase;
}

static inline float blepPhaseStep(float phaseStep) {
    return _min(fabsf(phaseStep), 0.499f);
}

static inline float polyBlep(float phase, float phaseStep) {
    if (phaseStep <= 0.f)
        return 0.f;

    if (phase < phaseStep) {
        phase /= phaseStep;
        return phase + phase - phase * phase - 1.f;
    }

    if (phase > 1.f - phaseStep) {
        phase = (phase - 1.f) / phaseStep;
        return phase * phase + phase + phase + 1.f;
    }

    return 0.f;
}

static inline float clampPulseWidth(float duty, float phaseStep) {
    float minDuty = blepPhaseStep(phaseStep);
    if (minDuty >= 0.5f)
        return 0.5f;

    return _clamp(duty, minDuty, 1.f - minDuty);
}

static inline float positiveSaw(float phase, float phaseStep) {
    float sample = phase * 2.f - 1.f;
    sample -= polyBlep(phase, phaseStep);
    return sample;
}

static inline float positiveSquare(float phase, float phaseStep, float duty) {
    float sample = phase < duty ? 1.f : -1.f;
    sample += polyBlep(phase, phaseStep);
    sample -= polyBlep(wrapPhase01(phase - duty), phaseStep);
    return sample;
}

static inline float generateNaiveOscillatorWave(int waveMode, float phase, float duty) {
    phase = wrapPhase01(phase);

    if (waveMode == 0)
        return sinf(phase * 2.f * kPi);

    if (waveMode == 1)
        return phase >= duty ? -1.f : 1.f;

    if (waveMode == 2)
        return phase * 2.f - 1.f;

    float sample = phase <= 0.5f ? phase * 2.f : 1.f - (phase - 0.5f) * 2.f;
    return sample * 2.f - 1.f;
}

static inline float generateOscillatorWave(int waveMode, float phase, float phaseStep, float duty) {
    phase = wrapPhase01(phase);

    if (waveMode == 0)
        return sinf(phase * 2.f * kPi);

    if (waveMode == 1) {
        duty = clampPulseWidth(duty, phaseStep);
        if (phaseStep >= 0.f)
            return positiveSquare(phase, phaseStep, duty);

        float reversePhase = wrapPhase01(1.f - phase);
        float reverseDuty = 1.f - duty;
        return -positiveSquare(reversePhase, -phaseStep, reverseDuty);
    }

    if (waveMode == 2) {
        if (phaseStep >= 0.f)
            return positiveSaw(phase, phaseStep);

        return -positiveSaw(wrapPhase01(1.f - phase), -phaseStep);
    }

    float sample = phase <= 0.5f ? phase * 2.f : 1.f - (phase - 0.5f) * 2.f;
    return sample * 2.f - 1.f;
}

static inline float estimatePositiveCrossing(float prevValue, float curValue) {
    float delta = curValue - prevValue;
    if (fabsf(delta) < 1e-6f)
        return 1.f;

    return _clamp(-prevValue / delta, 0.f, 1.f);
}

static inline void writeOscillatorSample(float buffer[], int offset, int channels, float sample) {
#if defined(__ARM_NEON) || defined(__ARM_NEON__)
    if (channels == 2) {
        float32x2_t stereo = vdup_n_f32(sample);
        vst1_f32(buffer + offset, stereo);
        return;
    }
#endif

    for (int c = 0; c < channels; ++c)
        buffer[offset + c] = sample;
}

static inline float lerpValue(float a, float b, float t) {
    return (a * (1.f - t)) + (b * t);
}

} // namespace

OscillatorProcessor* oscillatorCreateProcessor(float sampleRate) {
    OscillatorProcessor* processor = new OscillatorProcessor();
    processor->phase = 0.0;
    processor->sampleDuration = sampleRate > 0.f ? 1.0 / (double) sampleRate : 1.0 / 48000.0;
    processor->prevSyncValue = -1.f;
    processor->prevFrequency = 0.f;
    processor->prevAmplitude = 0.f;
    processor->hadPreviousBuffer = false;
    return processor;
}

void oscillatorDestroyProcessor(OscillatorProcessor* processor) {
    delete processor;
}

void oscillatorSetPhase(OscillatorProcessor* processor, double phase) {
    if (!processor)
        return;

    processor->phase = wrapPhase01((float) phase);
    processor->prevSyncValue = -1.f;
}

void oscillatorProcessBuffer(OscillatorProcessor* processor, float buffer[], int length, int channels, double& phaseMirror,
                             float analogWave, bool bLfo, float frequency, float amplitude,
                             float frequencyExpBuffer[], float frequencyLinBuffer[], float amplitudeBuffer[],
                             float syncBuffer[], float pwmBuffer[], bool bFreqExpGen, bool bFreqLinGen, bool bAmpGen,
                             bool bSyncGen, bool bPwmGen, double& dspTime) {
    if (!processor)
        return;

    int waveMode = (int) roundf(analogWave * 3.f);

    if (!processor->hadPreviousBuffer) {
        processor->prevFrequency = frequency;
        processor->prevAmplitude = amplitude;
        processor->hadPreviousBuffer = true;
    }

    float phase = (float) processor->phase;
    float prevSyncValue = processor->prevSyncValue;
    float startFrequency = processor->prevFrequency;
    float startAmplitude = processor->prevAmplitude;
    double sampleDuration = processor->sampleDuration;

    for (int i = 0; i < length; i += channels) {
        float endFrequency =
            startFrequency != frequency ? lerpValue(startFrequency, frequency, (float) i / (float) length) : frequency;
        float endAmplitude =
            startAmplitude != amplitude ? lerpValue(startAmplitude, amplitude, (float) i / (float) length) : amplitude;

        if (bFreqExpGen)
            endFrequency = endFrequency * powf(2.f, _clamp(frequencyExpBuffer[i], -1.f, 1.f) * 10.f);
        if (bFreqLinGen)
            endFrequency += frequencyLinBuffer[i] * 8000.f;
        if (bAmpGen)
            endAmplitude *= amplitudeBuffer[i];

        float phaseStep = _clamp(endFrequency, -24000.f, 24000.f) * (float) sampleDuration;
        float duty = bPwmGen ? (pwmBuffer[i] + 1.f) * 0.5f : 0.5f;
        float sample;

        if (bLfo) {
            if (bSyncGen) {
                if (syncBuffer[i] > 0.f && prevSyncValue <= 0.f)
                    phase = 0.f;

                prevSyncValue = syncBuffer[i];
            }

            sample = generateNaiveOscillatorWave(waveMode, phase, duty);
        } else {
            sample = generateOscillatorWave(waveMode, phase, phaseStep, duty);

            if (bSyncGen) {
                bool syncTriggered = syncBuffer[i] > 0.f && prevSyncValue <= 0.f;
                if (syncTriggered) {
                    float syncPosition = estimatePositiveCrossing(prevSyncValue, syncBuffer[i]);
                    float phaseAfterSync = 1.f - syncPosition;
                    float preSyncPhase = wrapPhase01(phase - phaseStep * phaseAfterSync);
                    float postSyncPhase = wrapPhase01(phaseStep * phaseAfterSync);
                    float preSyncSample = generateOscillatorWave(waveMode, preSyncPhase, phaseStep, duty);
                    float postResetSample = generateOscillatorWave(waveMode, 0.f, phaseStep, duty);
                    float blepStep = blepPhaseStep(phaseStep);
                    float syncCorrection = 0.f;

                    sample = generateOscillatorWave(waveMode, postSyncPhase, phaseStep, duty);
                    if (blepStep > 0.f) {
                        float syncPhase = _min(fabsf(phaseStep) * phaseAfterSync, blepStep);
                        syncCorrection = (postResetSample - preSyncSample) * polyBlep(syncPhase, blepStep);
                    }

                    sample += syncCorrection;
                    phase = postSyncPhase;
                }

                prevSyncValue = syncBuffer[i];
            }
        }

        phase = wrapPhase01(phase + phaseStep);

        sample *= endAmplitude;
        writeOscillatorSample(buffer, i, channels, sample);

        dspTime += sampleDuration;
    }

    processor->phase = phase;
    processor->prevSyncValue = prevSyncValue;
    processor->prevFrequency = frequency;
    processor->prevAmplitude = amplitude;
    phaseMirror = processor->phase;
}
