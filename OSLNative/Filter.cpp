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

#include "main.h"
#include "util.h"
#include <math.h>
#include "Filter.h"

extern "C" {

namespace {
constexpr float kSilentInputThreshold = 0.00001f;
constexpr float kExcitationAmplitude = 0.0002f;

inline float getCutoffHz(float cutoffPercent, float modulation, float minCutoffHz, float maxCutoffHz,
                         float sampleRate) {
    float safeMin = _max(1.f, minCutoffHz);
    float safeMax = _max(safeMin + 1.f, maxCutoffHz);
    float baseCutoffHz = expf(lerp(logf(safeMin), logf(safeMax), _clamp(cutoffPercent, 0.f, 1.f)));
    float cutoffHz = baseCutoffHz * powf(2.f, modulation);
    return _clamp(cutoffHz, 1.f, sampleRate * 0.495f);
}

bool bufferIsNearSilent(const float* buffer, int length) {
    for (int i = 0; i < length; ++i) {
        if (fabsf(buffer[i]) > kSilentInputThreshold)
            return false;
    }
    return true;
}
} // namespace

static inline float ProcessSample(FilterData* fd, float sample) {
    float input = _clamp(sample, -1.f, 1.f) - fd->q * fd->b4; // feedback

    float t1 = fd->b1;
    fd->b1 = (input + fd->b0) * fd->p - fd->b1 * fd->f;
    float t2 = fd->b2;
    fd->b2 = (fd->b1 + t1) * fd->p - fd->b2 * fd->f;
    t1 = fd->b3;
    fd->b3 = (fd->b2 + t2) * fd->p - fd->b3 * fd->f;
    fd->b4 = (fd->b3 + t1) * fd->p - fd->b4 * fd->f;
    fd->b4 = fd->b4 - fd->b4 * fd->b4 * fd->b4 * 0.166667f; // clipping
    fd->b0 = input;

    fd->q = _clamp(fd->q, -1.f, 1.f);
    fd->b0 = _clamp(fd->b0, -1.f, 1.f);
    fd->b1 = _clamp(fd->b1, -1.f, 1.f);
    fd->b2 = _clamp(fd->b2, -1.f, 1.f);
    fd->b3 = _clamp(fd->b3, -1.f, 1.f);
    fd->b4 = _clamp(fd->b4, -1.f, 1.f);

    return fd->b4;
}

// Filter is OSL's character lowpass: a saturated 4-pole ladder approximation derived from the
// Paul Kellett / Stilson-Smith-style Moog VCF family. It is intentionally LP-only and built to
// be musical near self-oscillation rather than perfectly neutral.
void processStereoFilter(float buffer[], int length, FilterData* mfL, FilterData* mfR, float cutoffPercent,
                         float lastCutoffPercent, float minCutoffHz, float maxCutoffHz, float modulationOctaveRange,
                         float frequencyBuffer[], float resonance, float sampleRate, bool queueExcitation) {
    float safeSampleRate = sampleRate > 0.f ? sampleRate : 48000.f;
    float freqDiv = 2.f / safeSampleRate;
    bool injectExcitation = queueExcitation && bufferIsNearSilent(buffer, length);
    int frames = length / 2;

    for (int i = 0; i < length; i += 2) {
        int frame = i / 2;
        float t = frames > 1 ? (float) frame / (float) (frames - 1) : 1.f;
        float currentCutoffPercent = lerp(lastCutoffPercent, cutoffPercent, t);
        float modulation = _clamp(frequencyBuffer[i], -1.f, 1.f) * modulationOctaveRange;
        float normalizedCutoff = getCutoffHz(currentCutoffPercent, modulation, minCutoffHz, maxCutoffHz,
                                             safeSampleRate) * freqDiv;

        normalizedCutoff = _clamp(normalizedCutoff, 0.f, 1.f);

        mfL->q = 1.0f - normalizedCutoff;
        mfL->p = normalizedCutoff + 0.8f * normalizedCutoff * mfL->q;
        mfL->f = mfL->p + mfL->p - 1.0f;
        mfL->q = resonance * (1.0f + 0.5f * mfL->q * (1.0f - mfL->q + 5.6f * mfL->q * mfL->q));

        mfR->q = 1.0f - normalizedCutoff;
        mfR->p = normalizedCutoff + 0.8f * normalizedCutoff * mfR->q;
        mfR->f = mfR->p + mfR->p - 1.0f;
        mfR->q = resonance * (1.0f + 0.5f * mfR->q * (1.0f - mfR->q + 5.6f * mfR->q * mfR->q));

        float leftSample = buffer[i];
        float rightSample = buffer[i + 1];
        if (injectExcitation && i == 0) {
            leftSample += kExcitationAmplitude;
            rightSample += kExcitationAmplitude;
        }

        buffer[i] = ProcessSample(mfL, leftSample);
        buffer[i + 1] = ProcessSample(mfR, rightSample);
    }
}
}
