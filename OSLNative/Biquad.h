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

/* Adapted from https://webaudio.github.io/Audio-EQ-Cookbook/Audio-EQ-Cookbook.txt

 A biquad filter with a very simple API. Coefficients are recalculated automatically if necessary.
 A change of type, frequency, Q, gain or sampleRate will cause re-calculation of the filter coefficients.
 At the moment, only stereo processing is exposed to the public API, though the filter can handle any number of
 channels.

 C# / Unity usage example:

 1. Create a new instance:
    IntPtr biquad = Biquad_new(BIQUAD_LOWPASS, 500, 1.0, 1.0, 48000, 2);

 2. Repeatedly call the processing routine:
    Biquad_processStereo(biquad, BIQUAD_LOWPASS, 500, 1.0, 1.0, 48000, buffer, buffer, buffer.Length);

 3. Don't forget to free memory:
    Biquad_free(biquad);

 */

#ifndef Biquad_h
#define Biquad_h

#include "main.h"
#include "util.h"

#define BIQUAD_LOWPASS 1
#define BIQUAD_HIGHPASS 2
#define BIQUAD_LOWSHELF 3
#define BIQUAD_HIGHSHELF 4
#define BIQUAD_PEAK 5

typedef struct Biquad {
    // public
    int type;
    float frequency;
    float Q;
    float gain;
    float sampleRate;
    int channels;

    // internal
    float* yMem;
    float* xMem;
    float A;
    float w0;
    float cosW0;
    float sinW0;
    float alpha;
    float a0, a1, a2, b0, b1, b2;
    float b0_over_a0;
    float b1_over_a0;
    float b2_over_a0;
    float a1_over_a0;
    float a2_over_a0;
} Biquad;

#ifdef __cplusplus
extern "C" {
#endif

/* Allocates a new instance and returns a pointer to it.
channels: The number of channels the biquad should process. Cannot be changed afterwards.
All other parameters: See  Biquad_process documentation. */
OSL_API Biquad* Biquad_new(int type, float frequency, float Q, float gain, float sampleRate, int channels);

/* Deletes an existing instance. */
OSL_API void Biquad_free(Biquad* x);

/*
Processes a block of INTERLEAVED (!) audio data.

Params:
 type: BIQUAD_LOWPASS, BIQUAD_HIGHPASS, BIQUAD_LOWSHELF, BIQUAD_HIGHSHELF or BIQUAD_PEAK.
 frequency: the center frequency in Hertz.
 Q: the quality factor ("resonance") of the filter.
 gain: the overall gain of the filter.
 sampleRate: the sample rate of the filter.
 in: a buffer of 32bit floating point PCM audio data.
 out: a buffer of 32bit floating point PCM audio data.
 (in and out can be identical.)
 n: The number of samples to process.

 Though all values are passed every time, the biquad re-calculates coefficients only if necessary.
 */
OSL_API void Biquad_process(Biquad* x, int type, float frequency, float Q, float gain, float sampleRate, float* in,
                            float* out, int n);

#ifdef __cplusplus
}
#endif

#endif // Biquad_h
