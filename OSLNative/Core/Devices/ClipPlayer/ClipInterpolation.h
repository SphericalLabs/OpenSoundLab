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

#pragma once

#include "Resample.h"
#include "Util.h"
#include <math.h>

#if defined(__ARM_NEON) || defined(__ARM_NEON__) || defined(_M_ARM64) || defined(_M_ARM)
#include <arm_neon.h>
#define OSL_ARM_NEON 1
#else
#define OSL_ARM_NEON 0
#endif

constexpr float kWsincCutoffNormalized = 0.85f;
constexpr float kAudioPi = 3.14159265f;
constexpr float kWsincWindowA0 = 0.35875f;
constexpr float kWsincWindowA1 = 0.48829f;
constexpr float kWsincWindowA2 = 0.14128f;
constexpr float kWsincWindowA3 = 0.01168f;

static inline int clampFrameIndex(int frame, int frameCount) {
    if (frame < 0)
        return 0;
    if (frame >= frameCount)
        return frameCount - 1;
    return frame;
}

static inline float readClipFrame(const float* clipData, int frameCount, int clipChannels, int frame, int channel) {
    if (!clipData || frameCount <= 0)
        return 0.f;

    frame = clampFrameIndex(frame, frameCount);
    int channelIndex = clipChannels > 1 ? channel : 0;
    return clipData[frame * clipChannels + channelIndex];
}

static inline float windowedSincWeight(float distance) {
    float absDistance = fabsf(distance);
    if (absDistance > ZEROCROSSINGS_PER_AXIS)
        return 0.f;

    float sinc = 0.f;
    if (absDistance < 1e-6f) {
        sinc = kWsincCutoffNormalized;
    } else {
        float x = kWsincCutoffNormalized * kAudioPi * distance;
        sinc = sinf(x) / x * kWsincCutoffNormalized;
    }

    float phase = (distance + ZEROCROSSINGS_PER_AXIS) / (2.f * ZEROCROSSINGS_PER_AXIS);
    float window = kWsincWindowA0 - kWsincWindowA1 * cosf(2.f * kAudioPi * phase) +
                   kWsincWindowA2 * cosf(4.f * kAudioPi * phase) -
                   kWsincWindowA3 * cosf(6.f * kAudioPi * phase);
    return sinc * window;
}

#if OSL_ARM_NEON
static inline float32x2_t readClipFrameStereo(const float* clipData, int frameCount, int frame) {
    if (!clipData || frameCount <= 0)
        return vdup_n_f32(0.f);

    frame = clampFrameIndex(frame, frameCount);
    return vld1_f32(clipData + frame * 2);
}

static inline float32x2_t readNearestSampleStereo(const float* clipData, int frameCount, double position) {
    return readClipFrameStereo(clipData, frameCount, (int) round(position));
}

static inline float32x2_t readLinearSampleStereo(const float* clipData, int frameCount, double position) {
    int frame = (int) floor(position);
    float frac = (float) (position - frame);
    float32x2_t a = readClipFrameStereo(clipData, frameCount, frame);
    float32x2_t b = readClipFrameStereo(clipData, frameCount, frame + 1);
    float32x2_t delta = vsub_f32(b, a);
    return vmla_n_f32(a, delta, frac);
}

static inline float32x2_t readHermiteSampleStereo(const float* clipData, int frameCount, double position) {
    int x1 = (int) floor(position);
    float frac = (float) (position - x1);

    float32x2_t y0 = readClipFrameStereo(clipData, frameCount, x1 - 1);
    float32x2_t y1 = readClipFrameStereo(clipData, frameCount, x1);
    float32x2_t y2 = readClipFrameStereo(clipData, frameCount, x1 + 1);
    float32x2_t y3 = readClipFrameStereo(clipData, frameCount, x1 + 2);

    float32x2_t c0 = y1;
    float32x2_t c1 = vmul_n_f32(vsub_f32(y2, y0), 0.5f);

    float32x2_t c2 = vsub_f32(y0, vmul_n_f32(y1, 2.5f));
    c2 = vadd_f32(c2, vmul_n_f32(y2, 2.f));
    c2 = vsub_f32(c2, vmul_n_f32(y3, 0.5f));

    float32x2_t c3 = vmul_n_f32(vsub_f32(y3, y0), 0.5f);
    c3 = vadd_f32(c3, vmul_n_f32(vsub_f32(y1, y2), 1.5f));

    float32x2_t result = vmla_n_f32(c2, c3, frac);
    result = vmla_n_f32(c1, result, frac);
    return vmla_n_f32(c0, result, frac);
}

static inline float32x2_t readLagrangeSampleStereo(const float* clipData, int frameCount, double position) {
    int x1 = (int) floor(position);
    float frac = (float) (position - x1);

    float32x2_t y0 = readClipFrameStereo(clipData, frameCount, x1 - 1);
    float32x2_t y1 = readClipFrameStereo(clipData, frameCount, x1);
    float32x2_t y2 = readClipFrameStereo(clipData, frameCount, x1 + 1);
    float32x2_t y3 = readClipFrameStereo(clipData, frameCount, x1 + 2);

    float w0 = -frac * (frac - 1.f) * (frac - 2.f) / 6.f;
    float w1 = (frac + 1.f) * (frac - 1.f) * (frac - 2.f) * 0.5f;
    float w2 = -(frac + 1.f) * frac * (frac - 2.f) * 0.5f;
    float w3 = (frac + 1.f) * frac * (frac - 1.f) / 6.f;

    float32x2_t result = vmul_n_f32(y0, w0);
    result = vmla_n_f32(result, y1, w1);
    result = vmla_n_f32(result, y2, w2);
    result = vmla_n_f32(result, y3, w3);
    return result;
}

static inline float32x2_t readWindowedSincSampleStereo(const float* clipData, int frameCount, double position) {
    int center = (int) round(position);
    float frac = (float) (position - center);
    float32x2_t sum = vdup_n_f32(0.f);

    for (int tap = -ZEROCROSSINGS_PER_AXIS; tap <= ZEROCROSSINGS_PER_AXIS; tap++) {
        int frame = center + tap;
        float32x2_t sample = readClipFrameStereo(clipData, frameCount, frame);
        float weight = windowedSincWeight((float) tap - frac);
        sum = vmla_n_f32(sum, sample, weight);
    }

    return sum;
}

static inline float32x2_t readInterpolatedSampleStereo(const float* clipData, int frameCount, double position,
                                                       int interpolationMode) {
    if (!clipData || frameCount <= 0)
        return vdup_n_f32(0.f);

    switch (interpolationMode) {
    case INTERPOLATION_NONE:
        return readNearestSampleStereo(clipData, frameCount, position);
    case INTERPOLATION_LINEAR:
        return readLinearSampleStereo(clipData, frameCount, position);
    case INTERPOLATION_WSINC:
        return readWindowedSincSampleStereo(clipData, frameCount, position);
    case INTERPOLATION_LAGRANGE:
        return readLagrangeSampleStereo(clipData, frameCount, position);
    case INTERPOLATION_HERMITE:
    default:
        return readHermiteSampleStereo(clipData, frameCount, position);
    }
}
#endif

static inline float readNearestSample(const float* clipData, int frameCount, int clipChannels, double position,
                                      int channel) {
    return readClipFrame(clipData, frameCount, clipChannels, (int) round(position), channel);
}

static inline float readLinearSample(const float* clipData, int frameCount, int clipChannels, double position,
                                     int channel) {
    int frame = (int) floor(position);
    float frac = (float) (position - frame);
    float a = readClipFrame(clipData, frameCount, clipChannels, frame, channel);
    float b = readClipFrame(clipData, frameCount, clipChannels, frame + 1, channel);
    return a + frac * (b - a);
}

static inline float readHermiteSample(const float* clipData, int frameCount, int clipChannels, double position,
                                      int channel) {
    int x1 = (int) floor(position);
    float frac = (float) (position - x1);

    float y0 = readClipFrame(clipData, frameCount, clipChannels, x1 - 1, channel);
    float y1 = readClipFrame(clipData, frameCount, clipChannels, x1, channel);
    float y2 = readClipFrame(clipData, frameCount, clipChannels, x1 + 1, channel);
    float y3 = readClipFrame(clipData, frameCount, clipChannels, x1 + 2, channel);

    float c0 = y1;
    float c1 = 0.5f * (y2 - y0);
    float c2 = y0 - 2.5f * y1 + 2.f * y2 - 0.5f * y3;
    float c3 = 0.5f * (y3 - y0) + 1.5f * (y1 - y2);
    return ((c3 * frac + c2) * frac + c1) * frac + c0;
}

static inline float readLagrangeSample(const float* clipData, int frameCount, int clipChannels, double position,
                                       int channel) {
    int x1 = (int) floor(position);
    float frac = (float) (position - x1);

    float y0 = readClipFrame(clipData, frameCount, clipChannels, x1 - 1, channel);
    float y1 = readClipFrame(clipData, frameCount, clipChannels, x1, channel);
    float y2 = readClipFrame(clipData, frameCount, clipChannels, x1 + 1, channel);
    float y3 = readClipFrame(clipData, frameCount, clipChannels, x1 + 2, channel);

    float w0 = -frac * (frac - 1.f) * (frac - 2.f) / 6.f;
    float w1 = (frac + 1.f) * (frac - 1.f) * (frac - 2.f) * 0.5f;
    float w2 = -(frac + 1.f) * frac * (frac - 2.f) * 0.5f;
    float w3 = (frac + 1.f) * frac * (frac - 1.f) / 6.f;
    return y0 * w0 + y1 * w1 + y2 * w2 + y3 * w3;
}

static inline float readWindowedSincSample(const float* clipData, int frameCount, int clipChannels, double position,
                                           int channel) {
    int center = (int) round(position);
    float frac = (float) (position - center);
    float sum = 0.f;

    for (int tap = -ZEROCROSSINGS_PER_AXIS; tap <= ZEROCROSSINGS_PER_AXIS; tap++) {
        int frame = center + tap;
        float sample = readClipFrame(clipData, frameCount, clipChannels, frame, channel);
        sum += sample * windowedSincWeight((float) tap - frac);
    }

    return sum;
}

static inline float readInterpolatedSample(const float* clipData, int frameCount, int clipChannels, double position,
                                           int channel, int interpolationMode) {
    if (!clipData || frameCount <= 0)
        return 0.f;

    switch (interpolationMode) {
    case INTERPOLATION_NONE:
        return readNearestSample(clipData, frameCount, clipChannels, position, channel);
    case INTERPOLATION_LINEAR:
        return readLinearSample(clipData, frameCount, clipChannels, position, channel);
    case INTERPOLATION_WSINC:
        return readWindowedSincSample(clipData, frameCount, clipChannels, position, channel);
    case INTERPOLATION_LAGRANGE:
        return readLagrangeSample(clipData, frameCount, clipChannels, position, channel);
    case INTERPOLATION_HERMITE:
    default:
        return readHermiteSample(clipData, frameCount, clipChannels, position, channel);
    }
}

static inline int selectLayerIndex(float playbackStep, int availableLayers, bool useSampleLayers) {
    if (!useSampleLayers || availableLayers <= 1)
        return 0;

    playbackStep = fabsf(playbackStep);
    int layer = 0;
    while (layer + 1 < availableLayers && playbackStep > 1.25f) {
        playbackStep *= 0.5f;
        layer++;
    }

    return layer;
}

static inline float readLayeredSample(void* clipLayers[], int clipFrames[], int availableLayers, int clipChannels,
                                      double position, int channel, int interpolationMode, bool useSampleLayers,
                                      float playbackStep) {
    int layer = selectLayerIndex(playbackStep, availableLayers, useSampleLayers);
    while (layer > 0 && (!clipLayers[layer] || clipFrames[layer] <= 0)) {
        layer--;
    }

    const float* clipData = reinterpret_cast<const float*>(clipLayers[layer]);
    float scale = (float) (1 << layer);
    double scaledPosition = position / scale;
    return readInterpolatedSample(clipData, clipFrames[layer], clipChannels, scaledPosition, channel,
                                  interpolationMode);
}

#if OSL_ARM_NEON
static inline float32x2_t readLayeredSampleStereo(void* clipLayers[], int clipFrames[], int availableLayers,
                                                  double position, int interpolationMode, bool useSampleLayers,
                                                  float playbackStep) {
    int layer = selectLayerIndex(playbackStep, availableLayers, useSampleLayers);
    while (layer > 0 && (!clipLayers[layer] || clipFrames[layer] <= 0)) {
        layer--;
    }

    const float* clipData = reinterpret_cast<const float*>(clipLayers[layer]);
    float scale = (float) (1 << layer);
    double scaledPosition = position / scale;
    return readInterpolatedSampleStereo(clipData, clipFrames[layer], scaledPosition, interpolationMode);
}
#endif
