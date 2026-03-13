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

#include "Biquad.h"
#include <math.h>

#if defined(__ARM_NEON) || defined(__ARM_NEON__) || defined(_M_ARM64) || defined(_M_ARM)
#include <arm_neon.h>
#define OSL_ARM_NEON 1
#else
#define OSL_ARM_NEON 0
#endif

#define BIQUAD_UNDEFINED 0

namespace {
constexpr float kPi = 3.14159265358979323846f;
constexpr float kStateLimit = 1000000.f;

inline bool invalidValue(float value) {
    return !isfinite(value) || fabsf(value) > kStateLimit;
}

inline void sanitizeState(float& x1, float& x2, float& y1, float& y2, float& out) {
    if (!invalidValue(out) && !invalidValue(x1) && !invalidValue(x2) && !invalidValue(y1) && !invalidValue(y2))
        return;

    x1 = 0.f;
    x2 = 0.f;
    y1 = 0.f;
    y2 = 0.f;
    out = 0.f;
}

void Biquad_calculateCoeffs(Biquad* x) {
    x->A = powf(10.f, x->gain / 40.f);
    x->w0 = 2.f * kPi * x->frequency / x->sampleRate;
    x->cosW0 = cosf(x->w0);
    x->sinW0 = sinf(x->w0);
    x->alpha = x->sinW0 / (2.f * x->Q);

    switch (x->type) {
    case BIQUAD_LOWPASS:
        x->a0 = 1.f + x->alpha;
        x->a1 = -2.f * x->cosW0;
        x->a2 = 1.f - x->alpha;
        x->b0 = (1.f - x->cosW0) * 0.5f;
        x->b1 = 1.f - x->cosW0;
        x->b2 = (1.f - x->cosW0) * 0.5f;
        break;

    case BIQUAD_HIGHPASS:
        x->a0 = 1.f + x->alpha;
        x->a1 = -2.f * x->cosW0;
        x->a2 = 1.f - x->alpha;
        x->b0 = (1.f + x->cosW0) * 0.5f;
        x->b1 = -(1.f + x->cosW0);
        x->b2 = (1.f + x->cosW0) * 0.5f;
        break;

    case BIQUAD_LOWSHELF:
        x->b0 = x->A * ((x->A + 1.f) - (x->A - 1.f) * x->cosW0 + 2.f * sqrtf(x->A) * x->alpha);
        x->b1 = 2.f * x->A * ((x->A - 1.f) - (x->A + 1.f) * x->cosW0);
        x->b2 = x->A * ((x->A + 1.f) - (x->A - 1.f) * x->cosW0 - 2.f * sqrtf(x->A) * x->alpha);
        x->a0 = (x->A + 1.f) + (x->A - 1.f) * x->cosW0 + 2.f * sqrtf(x->A) * x->alpha;
        x->a1 = -2.f * ((x->A - 1.f) + (x->A + 1.f) * x->cosW0);
        x->a2 = (x->A + 1.f) + (x->A - 1.f) * x->cosW0 - 2.f * sqrtf(x->A) * x->alpha;
        break;

    case BIQUAD_HIGHSHELF:
        x->b0 = x->A * ((x->A + 1.f) + (x->A - 1.f) * x->cosW0 + 2.f * sqrtf(x->A) * x->alpha);
        x->b1 = -2.f * x->A * ((x->A - 1.f) + (x->A + 1.f) * x->cosW0);
        x->b2 = x->A * ((x->A + 1.f) + (x->A - 1.f) * x->cosW0 - 2.f * sqrtf(x->A) * x->alpha);
        x->a0 = (x->A + 1.f) - (x->A - 1.f) * x->cosW0 + 2.f * sqrtf(x->A) * x->alpha;
        x->a1 = 2.f * ((x->A - 1.f) - (x->A + 1.f) * x->cosW0);
        x->a2 = (x->A + 1.f) - (x->A - 1.f) * x->cosW0 - 2.f * sqrtf(x->A) * x->alpha;
        break;

    case BIQUAD_PEAK:
        x->a0 = 1.f + x->alpha / x->A;
        x->a1 = -2.f * x->cosW0;
        x->a2 = 1.f - x->alpha / x->A;
        x->b0 = 1.f + x->alpha * x->A;
        x->b1 = -2.f * x->cosW0;
        x->b2 = 1.f - x->alpha * x->A;
        break;

    case BIQUAD_BANDPASS:
        x->a0 = 1.f + x->alpha;
        x->a1 = -2.f * x->cosW0;
        x->a2 = 1.f - x->alpha;
        x->b0 = x->alpha;
        x->b1 = 0.f;
        x->b2 = -x->alpha;
        break;

    case BIQUAD_NOTCH:
        x->a0 = 1.f + x->alpha;
        x->a1 = -2.f * x->cosW0;
        x->a2 = 1.f - x->alpha;
        x->b0 = 1.f;
        x->b1 = -2.f * x->cosW0;
        x->b2 = 1.f;
        break;

    default:
        x->a0 = 1.f;
        x->a1 = 0.f;
        x->a2 = 0.f;
        x->b0 = 1.f;
        x->b1 = 0.f;
        x->b2 = 0.f;
        break;
    }

    x->b0_over_a0 = x->b0 / x->a0;
    x->b1_over_a0 = x->b1 / x->a0;
    x->b2_over_a0 = x->b2 / x->a0;
    x->a1_over_a0 = x->a1 / x->a0;
    x->a2_over_a0 = x->a2 / x->a0;
}

void Biquad_setParameters(Biquad* x, int type, float frequency, float Q, float gain, float sampleRate) {
    float safeSampleRate = sampleRate > 0.f ? sampleRate : 48000.f;
    float safeFrequency = _clamp(frequency, 5.f, safeSampleRate * 0.45f);
    float safeQ = _max(Q, 0.025f);

    int recalculateCoeffs = 0;
    if (gain != x->gain) {
        x->gain = gain;
        recalculateCoeffs = 1;
    }
    if (type != x->type) {
        x->type = type;
        recalculateCoeffs = 1;
    }
    if (safeFrequency != x->frequency) {
        x->frequency = safeFrequency;
        recalculateCoeffs = 1;
    }
    if (safeQ != x->Q) {
        x->Q = safeQ;
        recalculateCoeffs = 1;
    }
    if (safeSampleRate != x->sampleRate) {
        x->sampleRate = safeSampleRate;
        recalculateCoeffs = 1;
    }

    if (recalculateCoeffs)
        Biquad_calculateCoeffs(x);
}

void processStereoScalar(Biquad* x, float* in, float* out, int frames) {
    float x1L = x->xMem[0];
    float x2L = x->xMem[1];
    float x1R = x->xMem[2];
    float x2R = x->xMem[3];
    float y1L = x->yMem[0];
    float y2L = x->yMem[1];
    float y1R = x->yMem[2];
    float y2R = x->yMem[3];

    for (int frame = 0; frame < frames; ++frame) {
        int index = frame * 2;
        float inL = in[index];
        float inR = in[index + 1];
        float outL = x->b0_over_a0 * inL + x->b1_over_a0 * x1L + x->b2_over_a0 * x2L - x->a1_over_a0 * y1L -
                     x->a2_over_a0 * y2L;
        float outR = x->b0_over_a0 * inR + x->b1_over_a0 * x1R + x->b2_over_a0 * x2R - x->a1_over_a0 * y1R -
                     x->a2_over_a0 * y2R;
        sanitizeState(x1L, x2L, y1L, y2L, outL);
        sanitizeState(x1R, x2R, y1R, y2R, outR);

        out[index] = outL;
        out[index + 1] = outR;

        x2L = x1L;
        x1L = inL;
        y2L = y1L;
        y1L = outL;

        x2R = x1R;
        x1R = inR;
        y2R = y1R;
        y1R = outR;
    }

    x->xMem[0] = x1L;
    x->xMem[1] = x2L;
    x->xMem[2] = x1R;
    x->xMem[3] = x2R;
    x->yMem[0] = y1L;
    x->yMem[1] = y2L;
    x->yMem[2] = y1R;
    x->yMem[3] = y2R;
}

void processInterleavedScalar(Biquad* x, float* in, float* out, int frames) {
    for (int frame = 0; frame < frames; ++frame) {
        int base = frame * x->channels;
        for (int channel = 0; channel < x->channels; ++channel) {
            int offset = 2 * channel;
            float x0 = in[base + channel];
            float x1 = x->xMem[offset];
            float x2 = x->xMem[offset + 1];
            float y1 = x->yMem[offset];
            float y2 = x->yMem[offset + 1];
            float y0 = x->b0_over_a0 * x0 + x->b1_over_a0 * x1 + x->b2_over_a0 * x2 - x->a1_over_a0 * y1 -
                       x->a2_over_a0 * y2;
            sanitizeState(x1, x2, y1, y2, y0);

            out[base + channel] = y0;

            x->xMem[offset + 1] = x1;
            x->xMem[offset] = x0;
            x->yMem[offset + 1] = y1;
            x->yMem[offset] = y0;
        }
    }
}

#if OSL_ARM_NEON
void processStereoNeon(Biquad* x, float* in, float* out, int frames) {
    float x1Values[2] = {x->xMem[0], x->xMem[2]};
    float x2Values[2] = {x->xMem[1], x->xMem[3]};
    float y1Values[2] = {x->yMem[0], x->yMem[2]};
    float y2Values[2] = {x->yMem[1], x->yMem[3]};
    float32x2_t x1 = vld1_f32(x1Values);
    float32x2_t x2 = vld1_f32(x2Values);
    float32x2_t y1 = vld1_f32(y1Values);
    float32x2_t y2 = vld1_f32(y2Values);

    for (int frame = 0; frame < frames; ++frame) {
        float32x2_t x0 = vld1_f32(in + frame * 2);
        float32x2_t y0 = vmul_n_f32(x0, x->b0_over_a0);
        y0 = vmla_n_f32(y0, x1, x->b1_over_a0);
        y0 = vmla_n_f32(y0, x2, x->b2_over_a0);
        y0 = vmls_n_f32(y0, y1, x->a1_over_a0);
        y0 = vmls_n_f32(y0, y2, x->a2_over_a0);

        vst1_f32(out + frame * 2, y0);
        x2 = x1;
        x1 = x0;
        y2 = y1;
        y1 = y0;
    }

    vst1_f32(x1Values, x1);
    vst1_f32(x2Values, x2);
    vst1_f32(y1Values, y1);
    vst1_f32(y2Values, y2);
    x->xMem[0] = x1Values[0];
    x->xMem[1] = x2Values[0];
    x->xMem[2] = x1Values[1];
    x->xMem[3] = x2Values[1];
    x->yMem[0] = y1Values[0];
    x->yMem[1] = y2Values[0];
    x->yMem[2] = y1Values[1];
    x->yMem[3] = y2Values[1];
}
#endif
} // namespace

extern "C" {

OSL_API Biquad* Biquad_new(int type, float frequency, float Q, float gain, float sampleRate, int channels) {
    Biquad* x = (Biquad*) _malloc(sizeof(Biquad));
    x->type = BIQUAD_UNDEFINED;
    x->frequency = 0.f;
    x->Q = 0.f;
    x->gain = 0.f;
    x->sampleRate = 0.f;
    x->channels = channels > 0 ? channels : 2;
    x->xMem = (float*) _malloc(x->channels * 2 * sizeof(float));
    x->yMem = (float*) _malloc(x->channels * 2 * sizeof(float));
    Biquad_reset(x);
    Biquad_setParameters(x, type, frequency, Q, gain, sampleRate);
    return x;
}

OSL_API void Biquad_free(Biquad* x) {
    if (x == nullptr)
        return;

    _free(x->xMem);
    _free(x->yMem);
    _free(x);
}

OSL_API void Biquad_reset(Biquad* x) {
    if (x == nullptr)
        return;

    _fZero(x->xMem, x->channels * 2);
    _fZero(x->yMem, x->channels * 2);
}

OSL_API void Biquad_process(Biquad* x, int type, float frequency, float Q, float gain, float sampleRate, float* in,
                            float* out, int n) {
    if (x == nullptr || in == nullptr || out == nullptr || x->channels <= 0 || n <= 0)
        return;

    Biquad_setParameters(x, type, frequency, Q, gain, sampleRate);
    int frames = n / x->channels;

    if (x->channels == 2 && frames > 4) {
#if OSL_ARM_NEON
        processStereoNeon(x, in, out, frames);
#else
        processStereoScalar(x, in, out, frames);
#endif
        return;
    }

    if (x->channels == 2)
        processStereoScalar(x, in, out, frames);
    else
        processInterleavedScalar(x, in, out, frames);
}
}
