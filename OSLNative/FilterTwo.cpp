// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2026 OSLLv1 Sphericals OpenSoundLab
//
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
//
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
//
//
// Copyright © 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright © 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright © 2017 Apache 2.0 Google LLC SoundStage VR
//
// Licensed under the Apache License, Version 2.0 (the "License");
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#include "FilterTwo.h"
#include "util.h"
#include <math.h>

#if defined(__ARM_NEON) || defined(__ARM_NEON__) || defined(_M_ARM64) || defined(_M_ARM)
#include <arm_neon.h>
#define OSL_ARM_NEON 1
#else
#define OSL_ARM_NEON 0
#endif

namespace {
constexpr float kPi = 3.14159265358979323846f;
constexpr float kStateLimit = 32.f;
constexpr float kQDialScale = 0.7f;             // in all modes
constexpr float kBandpassResonanceScale = 0.4f; // additionally in BP mode;
constexpr float kNotchQ = 0.85f;                // in notch mode

inline int clampOversampling(int oversampling) {
    if (oversampling < 1)
        return 1;
    if (oversampling > 2)
        return 2;
    return oversampling;
}

inline float getCutoffHz(float cutoffPercent, float modulation, float minCutoffHz, float maxCutoffHz,
                         float sampleRate) {
    float safeMin = _max(1.f, minCutoffHz);
    float safeMax = _max(safeMin + 1.f, maxCutoffHz);
    float baseCutoffHz = expf(lerp(logf(safeMin), logf(safeMax), _clamp(cutoffPercent, 0.f, 1.f)));
    float cutoffHz = baseCutoffHz * powf(2.f, modulation);
    return _clamp(cutoffHz, 5.f, sampleRate * 0.45f);
}

inline float getQ(float resonance) {
    float clampedResonance = _clamp(resonance, 0.f, 1.f);
    float normalizedResonance = _clamp(clampedResonance * kQDialScale, 0.f, 1.f);
    float resonanceShape = powf(normalizedResonance, 0.65f);
    float minQ = 0.7071f;
    float maxQ = 24.f;
    return expf(lerp(logf(minQ), logf(maxQ), resonanceShape));
}

inline float getModeResonance(float resonance, int mode) {
    if (mode == FILTERTWO_BP)
        return resonance * kBandpassResonanceScale;
    return resonance;
}

inline float getDamping(float q) {
    return _clamp(1.f / q, 0.f, 2.f);
}

inline float getBandpassGain(float q) {
    return _clamp(0.9f + 0.35f * q, 1.f, 7.5f);
}

inline float renderMode(int mode, float low, float band, float high, float bandpassGain) {
    switch (mode) {
    case FILTERTWO_HP:
        return high;
    case FILTERTWO_BP:
        return band * bandpassGain;
    default:
        return low;
    }
}

inline float renderNotchStage(float input, float& lowEq, float& bandEq, float a1, float a2, float a3, float damping) {
    float v3 = input - lowEq;
    float band = a1 * bandEq + a2 * v3;
    float low = lowEq + a2 * bandEq + a3 * v3;
    float high = input - damping * band - low;
    lowEq = 2.f * low - lowEq;
    bandEq = 2.f * band - bandEq;
    return high + low;
}

inline float blendNotchOutputs(const float stageOutputs[4], float resonance) {
    float stagePosition = _clamp(resonance, 0.f, 1.f) * 3.f;
    int stageIndex = (int) floorf(stagePosition);
    if (stageIndex >= 3)
        return stageOutputs[3];

    float stageBlend = stagePosition - (float) stageIndex;
    return lerp(stageOutputs[stageIndex], stageOutputs[stageIndex + 1], stageBlend);
}

inline bool invalidValue(float value) {
    return !isfinite(value) || fabsf(value) > kStateLimit;
}

inline void sanitizeChannel(float& lowEq, float& bandEq, float& previousInput, float& output) {
    if (!invalidValue(lowEq) && !invalidValue(bandEq) && !invalidValue(previousInput) && !invalidValue(output))
        return;

    lowEq = 0.f;
    bandEq = 0.f;
    previousInput = 0.f;
    output = 0.f;
}

void resetState(FilterTwoData* x) {
    _fZero(x->lowEq, x->channels);
    _fZero(x->bandEq, x->channels);
    _fZero(x->previousInput, x->channels);
    for (int stage = 0; stage < 3; ++stage) {
        _fZero(x->notchLowEq[stage], x->channels);
        _fZero(x->notchBandEq[stage], x->channels);
    }
    x->previousModulation = 0.f;
}

void processFrameScalar(FilterTwoData* x, float* frameBuffer, float currentCutoffPercent, float currentResonance,
                        float rawModulation, float minCutoffHz, float maxCutoffHz, int mode, int oversampling) {
    float previousModulation = x->previousModulation;
    float stepSampleRate = x->sampleRate * oversampling;
    float inverseOversampling = 1.f / (float) oversampling;
    float q = mode == FILTERTWO_NOTCH ? kNotchQ : getQ(getModeResonance(currentResonance, mode));
    float damping = getDamping(q);
    float bandpassGain = getBandpassGain(q);

    for (int channel = 0; channel < x->channels; ++channel) {
        float currentInput = frameBuffer[channel];
        float previousInput = x->previousInput[channel];
        float lowEq = x->lowEq[channel];
        float bandEq = x->bandEq[channel];
        float notchLowEq[3] = {x->notchLowEq[0][channel], x->notchLowEq[1][channel], x->notchLowEq[2][channel]};
        float notchBandEq[3] = {x->notchBandEq[0][channel], x->notchBandEq[1][channel], x->notchBandEq[2][channel]};
        float outputSum = 0.f;

        for (int substep = 0; substep < oversampling; ++substep) {
            float frac = (float) (substep + 1) * inverseOversampling;
            float modulation = lerp(previousModulation, rawModulation, frac);
            float cutoffHz = getCutoffHz(currentCutoffPercent, modulation, minCutoffHz, maxCutoffHz, x->sampleRate);
            float g = tanf(kPi * cutoffHz / stepSampleRate);
            float a1 = 1.f / (1.f + g * (g + damping));
            float a2 = g * a1;
            float a3 = g * a2;

            float interpolatedInput = lerp(previousInput, currentInput, frac);
            float v3 = interpolatedInput - lowEq;
            float band = a1 * bandEq + a2 * v3;
            float low = lowEq + a2 * bandEq + a3 * v3;
            float high = interpolatedInput - damping * band - low;
            float output = low;

            lowEq = 2.f * low - lowEq;
            bandEq = 2.f * band - bandEq;

            if (mode == FILTERTWO_NOTCH) {
                float notchStages[4];
                notchStages[0] = high + low;
                for (int stage = 0; stage < 3; ++stage)
                    notchStages[stage + 1] = renderNotchStage(notchStages[stage], notchLowEq[stage], notchBandEq[stage],
                                                              a1, a2, a3, damping);
                output = blendNotchOutputs(notchStages, currentResonance);
            } else {
                output = renderMode(mode, low, band, high, bandpassGain);
            }

            outputSum += output;
        }

        float output = outputSum * inverseOversampling;
        bool invalid =
            invalidValue(lowEq) || invalidValue(bandEq) || invalidValue(currentInput) || invalidValue(output);
        if (mode == FILTERTWO_NOTCH) {
            for (int stage = 0; stage < 3; ++stage)
                invalid = invalid || invalidValue(notchLowEq[stage]) || invalidValue(notchBandEq[stage]);
        }
        if (invalid) {
            lowEq = 0.f;
            bandEq = 0.f;
            for (int stage = 0; stage < 3; ++stage) {
                notchLowEq[stage] = 0.f;
                notchBandEq[stage] = 0.f;
            }
            currentInput = 0.f;
            output = 0.f;
        }
        frameBuffer[channel] = output;
        x->lowEq[channel] = lowEq;
        x->bandEq[channel] = bandEq;
        x->previousInput[channel] = currentInput;
        for (int stage = 0; stage < 3; ++stage) {
            x->notchLowEq[stage][channel] = notchLowEq[stage];
            x->notchBandEq[stage][channel] = notchBandEq[stage];
        }
    }

    x->previousModulation = rawModulation;
}

#if OSL_ARM_NEON
void processFrameStereoNeon(FilterTwoData* x, float* frameBuffer, float currentCutoffPercent, float currentResonance,
                            float rawModulation, float minCutoffHz, float maxCutoffHz, int mode, int oversampling) {
    float32x2_t currentInput = vld1_f32(frameBuffer);
    float32x2_t previousInput = vld1_f32(x->previousInput);
    float32x2_t lowEq = vld1_f32(x->lowEq);
    float32x2_t bandEq = vld1_f32(x->bandEq);
    float32x2_t outputSum = vdup_n_f32(0.f);
    float previousModulation = x->previousModulation;
    float stepSampleRate = x->sampleRate * oversampling;
    float inverseOversampling = 1.f / (float) oversampling;
    float q = getQ(getModeResonance(currentResonance, mode));
    float damping = getDamping(q);
    float bandpassGain = getBandpassGain(q);

    for (int substep = 0; substep < oversampling; ++substep) {
        float frac = (float) (substep + 1) * inverseOversampling;
        float modulation = lerp(previousModulation, rawModulation, frac);
        float cutoffHz = getCutoffHz(currentCutoffPercent, modulation, minCutoffHz, maxCutoffHz, x->sampleRate);
        float g = tanf(kPi * cutoffHz / stepSampleRate);
        float a1 = 1.f / (1.f + g * (g + damping));
        float a2 = g * a1;
        float a3 = g * a2;

        float32x2_t interpolatedInput = vmla_n_f32(previousInput, vsub_f32(currentInput, previousInput), frac);
        float32x2_t v3 = vsub_f32(interpolatedInput, lowEq);
        float32x2_t band = vadd_f32(vmul_n_f32(bandEq, a1), vmul_n_f32(v3, a2));
        float32x2_t low = vadd_f32(lowEq, vadd_f32(vmul_n_f32(bandEq, a2), vmul_n_f32(v3, a3)));
        float32x2_t high = vsub_f32(vsub_f32(interpolatedInput, vmul_n_f32(band, damping)), low);

        lowEq = vsub_f32(vadd_f32(low, low), lowEq);
        bandEq = vsub_f32(vadd_f32(band, band), bandEq);

        float32x2_t output;
        switch (mode) {
        case FILTERTWO_HP:
            output = high;
            break;
        case FILTERTWO_BP:
            output = vmul_n_f32(band, bandpassGain);
            break;
        case FILTERTWO_NOTCH:
            output = vadd_f32(high, low);
            break;
        default:
            output = low;
            break;
        }

        outputSum = vadd_f32(outputSum, output);
    }

    float lowValues[2];
    float bandValues[2];
    float previousInputValues[2];
    float outputValues[2];
    vst1_f32(lowValues, lowEq);
    vst1_f32(bandValues, bandEq);
    vst1_f32(previousInputValues, currentInput);
    vst1_f32(outputValues, vmul_n_f32(outputSum, inverseOversampling));

    sanitizeChannel(lowValues[0], bandValues[0], previousInputValues[0], outputValues[0]);
    sanitizeChannel(lowValues[1], bandValues[1], previousInputValues[1], outputValues[1]);

    x->lowEq[0] = lowValues[0];
    x->lowEq[1] = lowValues[1];
    x->bandEq[0] = bandValues[0];
    x->bandEq[1] = bandValues[1];
    x->previousInput[0] = previousInputValues[0];
    x->previousInput[1] = previousInputValues[1];
    frameBuffer[0] = outputValues[0];
    frameBuffer[1] = outputValues[1];
    x->previousModulation = rawModulation;
}
#endif
} // namespace

extern "C" {

OSL_API FilterTwoData* FilterTwo_New(int channels, float sampleRate) {
    FilterTwoData* x = (FilterTwoData*) _malloc(sizeof(FilterTwoData));
    x->channels = channels > 0 ? channels : 2;
    x->sampleRate = sampleRate > 0.f ? sampleRate : 48000.f;
    x->lastMode = -1;
    x->previousModulation = 0.f;
    x->lowEq = (float*) _malloc(x->channels * sizeof(float));
    x->bandEq = (float*) _malloc(x->channels * sizeof(float));
    x->previousInput = (float*) _malloc(x->channels * sizeof(float));
    for (int stage = 0; stage < 3; ++stage) {
        x->notchLowEq[stage] = (float*) _malloc(x->channels * sizeof(float));
        x->notchBandEq[stage] = (float*) _malloc(x->channels * sizeof(float));
    }
    resetState(x);
    return x;
}

OSL_API void FilterTwo_Free(FilterTwoData* x) {
    if (x == nullptr)
        return;

    _free(x->lowEq);
    _free(x->bandEq);
    _free(x->previousInput);
    for (int stage = 0; stage < 3; ++stage) {
        _free(x->notchLowEq[stage]);
        _free(x->notchBandEq[stage]);
    }
    _free(x);
}

OSL_API void FilterTwo_Reset(FilterTwoData* x) {
    if (x == nullptr)
        return;

    resetState(x);
    x->lastMode = -1;
}

OSL_API void FilterTwo_Process(FilterTwoData* x, float buffer[], int length, float cutoffPercent,
                               float lastCutoffPercent, float minCutoffHz, float maxCutoffHz,
                               float modulationOctaveRange, float frequencyBuffer[], float resonance,
                               float lastResonance, int mode, int oversampling) {
    if (x == nullptr || buffer == nullptr || frequencyBuffer == nullptr || x->channels <= 0 || length <= 0)
        return;

    int safeOversampling = clampOversampling(oversampling);
    if (mode != x->lastMode) {
        resetState(x);
        x->lastMode = mode;
    }

    int frames = length / x->channels;
    for (int frame = 0; frame < frames; ++frame) {
        float t = frames > 1 ? (float) frame / (float) (frames - 1) : 1.f;
        float currentCutoff = lerp(lastCutoffPercent, cutoffPercent, t);
        float currentResonance = lerp(lastResonance, resonance, t);
        float rawModulation = _clamp(frequencyBuffer[frame * x->channels], -1.f, 1.f) * modulationOctaveRange;
        if (!isfinite(rawModulation))
            rawModulation = 0.f;

#if OSL_ARM_NEON
        if (x->channels == 2 && mode != FILTERTWO_NOTCH)
            processFrameStereoNeon(x, buffer + frame * x->channels, currentCutoff, currentResonance, rawModulation,
                                   minCutoffHz, maxCutoffHz, mode, safeOversampling);
        else
#endif
            processFrameScalar(x, buffer + frame * x->channels, currentCutoff, currentResonance, rawModulation,
                               minCutoffHz, maxCutoffHz, mode, safeOversampling);
    }
}
}
