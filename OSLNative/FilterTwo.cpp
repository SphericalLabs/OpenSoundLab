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
#include "Biquad.h"
#include "util.h"
#include <math.h>

namespace {
constexpr float kModulationThreshold = 0.000001f;

inline float softClip(float value) {
    float clamped = _clamp(value, -1.5f, 1.5f);
    return clamped - 0.14814815f * clamped * clamped * clamped;
}

inline float getCutoffHz(float cutoffPercent, float modulation, float minCutoffHz, float maxCutoffHz,
                         float sampleRate) {
    float safeMin = _max(1.f, minCutoffHz);
    float safeMax = _max(safeMin + 1.f, maxCutoffHz);
    float baseCutoffHz = expf(lerp(logf(safeMin), logf(safeMax), _clamp(cutoffPercent, 0.f, 1.f)));
    float cutoffHz = baseCutoffHz * powf(2.f, modulation);
    return _clamp(cutoffHz, 1.f, sampleRate * 0.495f);
}

inline float getModulationCoeff(float modulationLowpassHz, float sampleRate) {
    if (modulationLowpassHz <= 0.f)
        return 1.f;

    float safeHz = _clamp(modulationLowpassHz, 1.f, sampleRate * 0.25f);
    return 1.f - expf(-2.f * 3.14159265358979323846f * safeHz / sampleRate);
}

inline float getQ(float resonance) {
    float clampedResonance = _clamp(resonance, 0.f, 1.f);
    float minQ = 0.7071f;
    float maxQ = 10.f;
    return expf(lerp(logf(minQ), logf(maxQ), clampedResonance * clampedResonance * 0.8f));
}

inline int getBiquadType(int mode) {
    switch (mode) {
    case FILTERTWO_HP:
        return BIQUAD_HIGHPASS;
    case FILTERTWO_BP:
        return BIQUAD_BANDPASS;
    case FILTERTWO_NOTCH:
        return BIQUAD_NOTCH;
    default:
        return BIQUAD_LOWPASS;
    }
}

bool hasPerFrameModulation(const float* frequencyBuffer, int length, int channels) {
    for (int i = 0; i < length; i += channels) {
        if (fabsf(frequencyBuffer[i]) > kModulationThreshold)
            return true;
    }
    return false;
}

inline float getRawModulation(const float* frequencyBuffer, int index, float modulationOctaveRange) {
    float raw = _clamp(frequencyBuffer[index], -1.f, 1.f) * modulationOctaveRange;
    return isfinite(raw) ? raw : 0.f;
}

void shapeOutput(float* buffer, int length, float resonance, int biquadType) {
    float clampedResonance = _clamp(resonance, 0.f, 1.f);
    float drive = lerp(1.f, 1.18f, clampedResonance);
    float trim = lerp(1.f, 0.42f, clampedResonance * clampedResonance);

    if (biquadType == BIQUAD_BANDPASS)
        trim *= 0.82f;

    for (int i = 0; i < length; ++i)
        buffer[i] = softClip(buffer[i] * drive) * trim;
}
} // namespace

extern "C" {

OSL_API FilterTwoData* FilterTwo_New(int channels, float sampleRate) {
    FilterTwoData* x = (FilterTwoData*) _malloc(sizeof(FilterTwoData));
    x->channels = channels > 0 ? channels : 2;
    x->sampleRate = sampleRate > 0.f ? sampleRate : 48000.f;
    x->biquad = Biquad_new(BIQUAD_LOWPASS, 1000.f, 0.7071f, 0.f, x->sampleRate, x->channels);
    x->lastMode = -1;
    x->filteredModulation = 0.f;
    return x;
}

OSL_API void FilterTwo_Free(FilterTwoData* x) {
    if (x == nullptr)
        return;

    Biquad_free(x->biquad);
    _free(x);
}

OSL_API void FilterTwo_Reset(FilterTwoData* x) {
    if (x == nullptr)
        return;

    Biquad_reset(x->biquad);
    x->lastMode = -1;
    x->filteredModulation = 0.f;
}

OSL_API void FilterTwo_Process(FilterTwoData* x, float buffer[], int length, float cutoffPercent,
                               float lastCutoffPercent, float minCutoffHz, float maxCutoffHz,
                               float modulationOctaveRange, float modulationLowpassHz, float frequencyBuffer[],
                               float resonance, float lastResonance, int mode) {
    if (x == nullptr || buffer == nullptr || frequencyBuffer == nullptr || x->channels <= 0 || length <= 0)
        return;

    int biquadType = getBiquadType(mode);
    if (biquadType != x->lastMode) {
        Biquad_reset(x->biquad);
        x->lastMode = biquadType;
    }

    float modulationCoeff = getModulationCoeff(modulationLowpassHz, x->sampleRate);
    bool modulationActive = hasPerFrameModulation(frequencyBuffer, length, x->channels) ||
                            fabsf(x->filteredModulation) > kModulationThreshold;
    bool perFrame = modulationActive || cutoffPercent != lastCutoffPercent || resonance != lastResonance;

    if (!perFrame) {
        float cutoffHz = getCutoffHz(cutoffPercent, x->filteredModulation, minCutoffHz, maxCutoffHz, x->sampleRate);
        Biquad_process(x->biquad, biquadType, cutoffHz, getQ(resonance), 0.f, x->sampleRate, buffer, buffer, length);
        shapeOutput(buffer, length, resonance, biquadType);
        return;
    }

    int frames = length / x->channels;
    for (int frame = 0; frame < frames; ++frame) {
        float t = frames > 1 ? (float) frame / (float) (frames - 1) : 1.f;
        float currentCutoff = lerp(lastCutoffPercent, cutoffPercent, t);
        float currentResonance = lerp(lastResonance, resonance, t);
        float rawModulation = getRawModulation(frequencyBuffer, frame * x->channels, modulationOctaveRange);
        x->filteredModulation += modulationCoeff * (rawModulation - x->filteredModulation);
        if (!isfinite(x->filteredModulation))
            x->filteredModulation = 0.f;
        float cutoffHz =
            getCutoffHz(currentCutoff, x->filteredModulation, minCutoffHz, maxCutoffHz, x->sampleRate);
        Biquad_process(x->biquad, biquadType, cutoffHz, getQ(currentResonance), 0.f, x->sampleRate,
                       buffer + frame * x->channels, buffer + frame * x->channels, x->channels);
        shapeOutput(buffer + frame * x->channels, x->channels, currentResonance, biquadType);
    }
}
}
