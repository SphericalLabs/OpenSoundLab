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

#include "ClipPlayerApi.h"
#include "AudioMath.h"
#include "ClipInterpolation.h"
#include "Util.h"
#include <math.h>

constexpr int kMaxClipLayers = 5;

extern "C" {

double ClipSignalGenerator(float buffer[], float freqExpBuffer[], float freqLinBuffer[], float ampBuffer[],
                           float seqBuffer[], int length, float lastSeqGen[2], int channels, bool freqExpGen,
                           bool freqLinGen, bool ampGen, bool seqGen, double floatingBufferCount, int sampleBounds[2],
                           float playbackSpeed, float lastPlayBackSpeed, void* clip0, void* clip1, void* clip2,
                           void* clip3, void* clip4, int clipFrames0, int clipFrames1, int clipFrames2,
                           int clipFrames3, int clipFrames4, int availableLayers, int interpolationMode,
                           bool useSampleLayers, int clipChannels, float amplitude, float lastAmplitude,
                           bool playdirection, bool looping, double _sampleDuration, int bufferCount, bool& active,
                           int windowLength) {
    // clip not yet or not available anymore, but wouldn't check for segmentation fault due to outdated pointer
    if (!clip0) {
        return floatingBufferCount;
    }
    if (!sampleBounds) {
        return floatingBufferCount;
    }

    void* clipLayers[kMaxClipLayers];
    clipLayers[0] = clip0;
    clipLayers[1] = clip1;
    clipLayers[2] = clip2;
    clipLayers[3] = clip3;
    clipLayers[4] = clip4;

    int clipFrames[kMaxClipLayers];
    clipFrames[0] = clipFrames0;
    clipFrames[1] = clipFrames1;
    clipFrames[2] = clipFrames2;
    clipFrames[3] = clipFrames3;
    clipFrames[4] = clipFrames4;
    if (availableLayers < 1)
        availableLayers = 1;
    if (availableLayers > kMaxClipLayers)
        availableLayers = kMaxClipLayers;
    if (sampleBounds[1] <= sampleBounds[0])
        return floatingBufferCount;

    float sampleBoundsCenter = (sampleBounds[0] + sampleBounds[1]) * 0.5f;

    for (int i = 0; i < length; i += channels) {

        float endAmplitude = amplitude;
        if (lastAmplitude != amplitude)
            endAmplitude = lerp(lastAmplitude, amplitude, (float) i / length); // slope limiting
        if (ampGen)
            endAmplitude = endAmplitude * ampBuffer[i]; // -1,1, allows for ring modulation

        float endPlaybackSpeed = playbackSpeed;
        if (lastPlayBackSpeed != playbackSpeed)
            endPlaybackSpeed = lerp(lastPlayBackSpeed, playbackSpeed, (float) i / length); // slope limiting

        float playbackStep = 0.f;
        if (active) {
            if (freqExpGen)
                playbackStep = endPlaybackSpeed * pow(2, _clamp(freqExpBuffer[i], -1.f, 1.f) *
                                                             10.f); // exp fm, upscale 0.1V/Oct to 1V/Oct
            else
                playbackStep = endPlaybackSpeed;
            if (freqLinGen)
                playbackStep += freqLinBuffer[i] * 20.f; // lin fm

            floatingBufferCount += playbackStep;
        }

        bool endOfSample = false;
        if (floatingBufferCount > sampleBounds[1]) {
            endOfSample = true;
            floatingBufferCount = fmod(floatingBufferCount - sampleBounds[0], sampleBounds[1] - sampleBounds[0]) +
                                  sampleBounds[0] + 1; // wrap over playhead offset
        } else if (floatingBufferCount <
                   sampleBounds[0] + 1) // still needed? this introduces problems when resetting to 0 (instead of 1)
                                        // while having playback speeds below 1, this will always be caught here and
                                        // then playback stops immediately
        {
            endOfSample = true;
            floatingBufferCount =
                sampleBounds[1] - fmod(floatingBufferCount - sampleBounds[0],
                                       sampleBounds[1] - sampleBounds[0]); // wrap over playhead offset
        }

        if (endOfSample) {
            if (!looping)
                active = false;
        }

        if (seqGen) {
            if (seqBuffer[i] > 0.f && lastSeqGen[0] <= 0.f) {
                if (playbackSpeed >= 0)
                    floatingBufferCount = bufferCount =
                        sampleBounds[0] +
                        1; // WARNING: Due to the code structure in the  ClipSignalGenerator function resetting to 0
                           // (instead of 0 + 1) would mean that playback does not work anymore for speeds lower than
                           // 1f. It would always floor() to 0 and would not move through the file anymore.
                else
                    floatingBufferCount = bufferCount = sampleBounds[1];
                active = true;
            }
            lastSeqGen[0] = seqBuffer[i];
        }

        float windowing = 1.f;
        if (windowLength != 0) {
            if (floatingBufferCount < sampleBounds[0] + windowLength && floatingBufferCount <= sampleBoundsCenter) {
                windowing = _map(floatingBufferCount, sampleBounds[0], sampleBounds[0] + windowLength, 0.f, 1.f, 0.5f);
            } else if (floatingBufferCount > sampleBounds[1] - windowLength) {
                windowing = _map(floatingBufferCount, sampleBounds[1] - windowLength, sampleBounds[1], 1.f, 0.f, 0.5f);
            } else {
                windowing = 1.f;
            }
        }

        if (active) {
#if OSL_ARM_NEON
            if (clipChannels == 2 && channels == 2) {
                float32x2_t frame = readLayeredSampleStereo(clipLayers, clipFrames, availableLayers,
                                                            floatingBufferCount, interpolationMode,
                                                            useSampleLayers, playbackStep);
                frame = vmul_n_f32(frame, endAmplitude * windowing);
                vst1_f32(buffer + i, frame);
            } else
#endif
            {
            buffer[i] = readLayeredSample(clipLayers, clipFrames, availableLayers, clipChannels, floatingBufferCount, 0,
                                          interpolationMode, useSampleLayers, playbackStep) *
                        endAmplitude * windowing;

            if (clipChannels == 2) {
                buffer[i + 1] =
                    readLayeredSample(clipLayers, clipFrames, availableLayers, clipChannels, floatingBufferCount, 1,
                                      interpolationMode, useSampleLayers, playbackStep) *
                    endAmplitude * windowing;
            } else {
                buffer[i + 1] = buffer[i];
            }
            }
        }
    }
    return floatingBufferCount;
}
}
