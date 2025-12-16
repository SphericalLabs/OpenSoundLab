// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
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

/// Created by Hannes January 2022
///
/// This is a delay audio effect that emulates a bucket-brigade delay. It operates on a fixed-size buffer.
/// The delay time is determined by the oversampling factor used to write/read to the buffer.
/// This results in the characteristic "pitch-shift" effect when changing the delay time while there is still signal in
/// the delay buffer.
///
/// Note that the delay ignores multi-channel input: The first channel is used as input and copied to all output
/// channels.
///
/// All functions are not thread-safe, hence the caller must avoid simultaneous access from multiple threads.

#ifndef Delay_h
#define Delay_h

#include "main.h"
#include "RingBuffer.h"
#include "CompressedRingBuffer.h"
#include "CRingBuffer.hpp"

// #define DELAYMODE_SIMPLE 0 //deprecated
#define DELAYMODE_PADDED 1
#define DELAYMODE_INTERPOLATED 2

struct DelayData {
    // public
    int time; // in samples, therefore int.
    float feedback;
    float wet;
    float dry;
    int interpolation;
    int delayMode;

    // internal
    int maxTime;
    int minSamples;
    int maxSamples;
    int prevTime; // for time interpolation
    float prevFeedback;
    float prevDry;
    float prevWet;
    void* tap; // either RingBuffer or FrameRingBuffer, depending on the delay's current mode
    float* temp;
    float* temp2;
    float* cTime; // control signal for delay time
};

#ifdef __cplusplus
extern "C" {
#endif

/* Processing audio */

/// Processes 1 block of interleaved audio data.
OSL_API void Delay_Process(float buffer[], float timeBuffer[], float feedbackBuffer[], float mixBuffer[], int n,
                           int channels, DelayData* x);
/// Clears the delay buffer.
OSL_API void Delay_Clear(DelayData* x);

/* Setting and getting parameters */

/// Sets the parameter to the specified value.
OSL_API void Delay_SetParam(float value, int param, struct DelayData* x);
/// Sets the possible range of the delay. This is necessary for now bc excessive oversampling causes high CPU loads
/// otherwise.
OSL_API void Delay_SetRange(int min, int max, DelayData* x);
OSL_API void Delay_SetMode(int mode, DelayData* x);

/* Allocating and freeing */

/// Allocates and returns a new delay instance with maximum delay time n (samples).
OSL_API struct DelayData* Delay_New(int n);
/// Releases allocated resources.
OSL_API void Delay_Free(struct DelayData* x);

#ifdef __cplusplus
}
#endif

#endif /* Delay_h */
