// Copyright 2017-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


///  Created by Hannes Barfuss on 15.12.2021.
///
///  This is a feed-forward compressor based on this paper:
///  https://www.researchgate.net/publication/277772168_Digital_Dynamic_Range_Compressor_Design-A_Tutorial_and_Analysisx
///  The gain is computed by first applying threshold, ratio and knee to the input signal, then smoothing the resulting control signal by a 0-pole IIR filter (envelope follower). The computed gain is then applied ("fed forward") to the input signal.
///  The compressor has an optional Lookahead feature, which is mainly useful in conjunction with brickwall limiting. A limiter without lookahead only guarantees to not overshoot if it has an attack time of 0, which means no attack gain smoothing is applied at all. This will lead to rectangular wave shapes and thus unpleasant audible distortion. With a lookahead, the limiter can have an attack time > 0, which results in smoother gain reduction, and at the same time guarantee to not overshoot. The downside is that a lookahead of n samples adds n samples of latency to the output signal (because it is impossible to see into the future, so the "lookahead" is actually implemented as a delay on the input signal...)
///
///  All functions are not thread-safe, hence the caller must avoid simultaneous access from multiple threads.

#ifndef Compressor_h
#define Compressor_h

#include "main.h"

struct CompressorData
{
    //public
    float* params;
    
    //internal
    float sampleRate;
    float aA;
    float aR;
    float aD; //delay smoothing coefficient
    float y1_prev[2];
    float yL_prev[2];
    int d_prev; //previous delay, for smoothing delay
    float *buf;
    int bufPtr;
    int bufLength;
    float attenuation;
    bool clipping; //Indicates if at least one sample in the last buffer exceeded 0dB
};

#ifdef __cplusplus
extern "C" {
#endif

/* Processing Audio */

    ///Processes one block of interleaved stereo audio data.
    SOUNDSTAGE_API void Compressor_Process(float buffer[], float sc[], int length, int channels, CompressorData* x);

/* Setting and getting parameters */

    ///Sets the parameter to the specified value.
    SOUNDSTAGE_API void Compressor_SetParam(float value, int param, struct CompressorData *x);
    ///Returns the average attenuation applied to the most recently processed block of audio data.
    SOUNDSTAGE_API float Compressor_GetAttenuation(struct CompressorData *x);
    ///Returns true if the compressor is still approaching a lookahead time specified before (smoothing is applied to the lookahead parameter to avoid artifacts).
    SOUNDSTAGE_API bool Compressor_IsRamping(struct CompressorData *x);
    ///Returns true if at least 1 output sample of the most recently processed block of audio data exceeded 0dB.
    SOUNDSTAGE_API bool Compressor_IsClipping(struct CompressorData *x);

/* Allocating and Freeing */

    ///Returns a new compressor instance.
    SOUNDSTAGE_API struct CompressorData *Compressor_New(float sampleRate);
    ///Frees all resources of a compressor instance.
    SOUNDSTAGE_API void Compressor_Free(struct CompressorData *x);

#ifdef __cplusplus
}
#endif

#endif /* Compressor_h */
