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
constexpr float kBaseFrequency = 261.6256f;
constexpr float kExcitationThreshold = 0.985f;
constexpr float kExcitationAmount = 0.0001f;
constexpr float kSilenceThreshold = 0.0002f;
constexpr float kPi = 3.14159265358979323846f;

inline float softClip(float value) {
    float clamped = _clamp(value, -1.5f, 1.5f);
    return clamped - 0.14814815f * clamped * clamped * clamped;
}

#if OSL_ARM_NEON
inline float32x2_t softClip2(float32x2_t value) {
    const float32x2_t minValue = vdup_n_f32(-1.5f);
    const float32x2_t maxValue = vdup_n_f32(1.5f);
    const float32x2_t cubic = vdup_n_f32(0.14814815f);
    float32x2_t clamped = vmax_f32(minValue, vmin_f32(value, maxValue));
    return vsub_f32(clamped, vmul_f32(cubic, vmul_f32(vmul_f32(clamped, clamped), clamped)));
}
#endif

inline float getDamping(float resonance) {
    float clampedResonance = _clamp(resonance, 0.f, 1.f);
    float k = lerp(1.9f, 0.08f, clampedResonance * clampedResonance);

    if (clampedResonance > kExcitationThreshold) {
        float tail = (clampedResonance - kExcitationThreshold) / (1.f - kExcitationThreshold);
        k = lerp(k, 0.f, tail * tail);
    }

    return _max(0.f, k);
}

inline float getCutoffHz(float cutoff, float modulation, float sampleRate) {
    float octaveOffset = (_clamp(modulation, -1.f, 1.f) + cutoff) * 10.f;
    float cutoffHz = kBaseFrequency * powf(2.f, octaveOffset);
    return _clamp(cutoffHz, 5.f, sampleRate * 0.45f);
}

bool isSilent(const float* buffer, int length) {
    for (int i = 0; i < length; ++i) {
        if (fabsf(buffer[i]) > kSilenceThreshold)
            return false;
    }
    return true;
}

inline float selectOutput(int mode, float low, float band, float high, float notch) {
    switch (mode) {
    case FILTERTWO_HP:
        return high;
    case FILTERTWO_BP:
        return band;
    case FILTERTWO_NOTCH:
        return notch;
    default:
        return low;
    }
}

void processStereoNeon(FilterTwoData* x, float* buffer, int frames, float cutoffFrequency, float lastCutoffFrequency,
                       const float* frequencyBuffer, float resonance, float lastResonance, int mode) {
#if OSL_ARM_NEON
    float32x2_t lowEq = vld1_f32(x->lowEq);
    float32x2_t bandEq = vld1_f32(x->bandEq);

    for (int frame = 0; frame < frames; ++frame) {
        float t = frames > 1 ? (float) frame / (float) (frames - 1) : 1.f;
        float currentCutoff = lerp(lastCutoffFrequency, cutoffFrequency, t);
        float currentResonance = lerp(lastResonance, resonance, t);
        float cutoffHz = getCutoffHz(currentCutoff, frequencyBuffer[frame * 2], x->sampleRate);
        float g = tanf(kPi * cutoffHz / x->sampleRate);
        float k = getDamping(currentResonance);
        float a1 = 1.f / (1.f + g * (g + k));
        float a2 = g * a1;
        float a3 = g * a2;
        float drive = 1.1f + currentResonance * 1.2f;
        float inputDrive = 1.f + currentResonance * 0.25f;

        float32x2_t input = vld1_f32(buffer + frame * 2);
        float32x2_t v3;
        float32x2_t band;
        float32x2_t low;
        float32x2_t high;
        float32x2_t notch;

        input = softClip2(vmul_n_f32(input, inputDrive));
        v3 = vsub_f32(input, lowEq);
        band = vmla_n_f32(vmul_n_f32(bandEq, a1), v3, a2);
        band = vmul_n_f32(softClip2(vmul_n_f32(band, drive)), 1.f / drive);

        low = vadd_f32(lowEq, vmla_n_f32(vmul_n_f32(bandEq, a2), v3, a3));
        low = vmul_n_f32(softClip2(vmul_n_f32(low, drive)), 1.f / drive);

        high = vsub_f32(vsub_f32(input, vmul_n_f32(band, k)), low);
        notch = vadd_f32(high, low);

        bandEq = vsub_f32(vmul_n_f32(band, 2.f), bandEq);
        lowEq = vsub_f32(vmul_n_f32(low, 2.f), lowEq);

        switch (mode) {
        case FILTERTWO_HP:
            vst1_f32(buffer + frame * 2, high);
            break;
        case FILTERTWO_BP:
            vst1_f32(buffer + frame * 2, band);
            break;
        case FILTERTWO_NOTCH:
            vst1_f32(buffer + frame * 2, notch);
            break;
        default:
            vst1_f32(buffer + frame * 2, low);
            break;
        }
    }

    vst1_f32(x->lowEq, lowEq);
    vst1_f32(x->bandEq, bandEq);
#else
    (void) x;
    (void) buffer;
    (void) frames;
    (void) cutoffFrequency;
    (void) lastCutoffFrequency;
    (void) frequencyBuffer;
    (void) resonance;
    (void) lastResonance;
    (void) mode;
#endif
}

void processScalar(FilterTwoData* x, float* buffer, int length, int channels, float cutoffFrequency,
                   float lastCutoffFrequency, const float* frequencyBuffer, float resonance, float lastResonance,
                   int mode) {
    int frames = length / channels;

    for (int frame = 0; frame < frames; ++frame) {
        float t = frames > 1 ? (float) frame / (float) (frames - 1) : 1.f;
        float currentCutoff = lerp(lastCutoffFrequency, cutoffFrequency, t);
        float currentResonance = lerp(lastResonance, resonance, t);
        float cutoffHz = getCutoffHz(currentCutoff, frequencyBuffer[frame * channels], x->sampleRate);
        float g = tanf(kPi * cutoffHz / x->sampleRate);
        float k = getDamping(currentResonance);
        float a1 = 1.f / (1.f + g * (g + k));
        float a2 = g * a1;
        float a3 = g * a2;
        float drive = 1.1f + currentResonance * 1.2f;
        float inputDrive = 1.f + currentResonance * 0.25f;

        for (int channel = 0; channel < channels; ++channel) {
            int index = frame * channels + channel;
            float input = softClip(buffer[index] * inputDrive);
            float v3 = input - x->lowEq[channel];
            float band = a1 * x->bandEq[channel] + a2 * v3;
            band = softClip(band * drive) / drive;

            float low = x->lowEq[channel] + a2 * x->bandEq[channel] + a3 * v3;
            low = softClip(low * drive) / drive;

            float high = input - k * band - low;
            float notch = high + low;

            x->bandEq[channel] = 2.f * band - x->bandEq[channel];
            x->lowEq[channel] = 2.f * low - x->lowEq[channel];
            buffer[index] = selectOutput(mode, low, band, high, notch);
        }
    }
}
} // namespace

extern "C" {

OSL_API FilterTwoData* FilterTwo_New(int channels, float sampleRate) {
    FilterTwoData* x = (FilterTwoData*) _malloc(sizeof(FilterTwoData));
    x->channels = channels > 0 ? channels : 2;
    x->sampleRate = sampleRate > 0.f ? sampleRate : 48000.f;
    x->lowEq = (float*) _malloc(sizeof(float) * x->channels);
    x->bandEq = (float*) _malloc(sizeof(float) * x->channels);
    FilterTwo_Reset(x);
    return x;
}

OSL_API void FilterTwo_Free(FilterTwoData* x) {
    if (x == nullptr)
        return;

    _free(x->lowEq);
    _free(x->bandEq);
    _free(x);
}

OSL_API void FilterTwo_Reset(FilterTwoData* x) {
    if (x == nullptr)
        return;

    _fZero(x->lowEq, x->channels);
    _fZero(x->bandEq, x->channels);
}

OSL_API void FilterTwo_Process(FilterTwoData* x, float buffer[], int length, float cutoffFrequency,
                               float lastCutoffFrequency, float frequencyBuffer[], float resonance,
                               float lastResonance, int mode, bool queueExcitation) {
    if (x == nullptr || buffer == nullptr || frequencyBuffer == nullptr || x->channels <= 0 || length <= 0)
        return;

    int safeMode = mode;
    if (safeMode < FILTERTWO_LP || safeMode > FILTERTWO_NOTCH)
        safeMode = FILTERTWO_LP;

    bool crossedThreshold = lastResonance < kExcitationThreshold && resonance >= kExcitationThreshold;
    bool shouldExcite = (queueExcitation || crossedThreshold) && isSilent(buffer, length);
    if (shouldExcite) {
        int exciteSamples = _min(x->channels, length);
        for (int i = 0; i < exciteSamples; ++i)
            buffer[i] += kExcitationAmount;
    }

    if (x->channels == 2) {
#if OSL_ARM_NEON
        processStereoNeon(x, buffer, length / 2, cutoffFrequency, lastCutoffFrequency, frequencyBuffer, resonance,
                          lastResonance, safeMode);
#else
        processScalar(x, buffer, length, x->channels, cutoffFrequency, lastCutoffFrequency, frequencyBuffer, resonance,
                      lastResonance, safeMode);
#endif
    } else {
        processScalar(x, buffer, length, x->channels, cutoffFrequency, lastCutoffFrequency, frequencyBuffer, resonance,
                      lastResonance, safeMode);
    }
}
}
