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

void processStereoFilter(float buffer[], int length, FilterData* mfL, FilterData* mfR, float cutoffFrequency,
                         float lastCutoffFrequency, float frequencyBuffer[], float resonance, float sampleRate) {
    float safeSampleRate = sampleRate > 0.f ? sampleRate : 48000.f;
    float freqDiv = 2.f / safeSampleRate;
    float cut = cutoffFrequency;

    for (int i = 0; i < length; i += 2) {
        cut = lerp(lastCutoffFrequency, cutoffFrequency, (float) i / length); // slope limiting for dial

        float normalizedCutoff =
            _clamp(261.6256f * powf(2, (_clamp(frequencyBuffer[i], -1.f, 1.f) + cut) * 10.f) * freqDiv, 0.f, 1.f);

        mfL->q = 1.0f - normalizedCutoff;
        mfL->p = normalizedCutoff + 0.8f * normalizedCutoff * mfL->q;
        mfL->f = mfL->p + mfL->p - 1.0f;
        mfL->q = resonance * (1.0f + 0.5f * mfL->q * (1.0f - mfL->q + 5.6f * mfL->q * mfL->q));

        mfR->q = 1.0f - normalizedCutoff;
        mfR->p = normalizedCutoff + 0.8f * normalizedCutoff * mfR->q;
        mfR->f = mfR->p + mfR->p - 1.0f;
        mfR->q = resonance * (1.0f + 0.5f * mfR->q * (1.0f - mfR->q + 5.6f * mfR->q * mfR->q));

        buffer[i] = ProcessSample(mfL, buffer[i]);
        buffer[i + 1] = ProcessSample(mfR, buffer[i + 1]);
    }
}
}
