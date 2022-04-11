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

/*
 Hannes Barfuss 17.12.2021
 
 These are just some wrapper functions for FreeVerb.
 
 All functions are not thread-safe, hence the caller must avoid simultaneous access from multiple threads.
 
 */

#ifndef StereoVerb_h
#define StereoVerb_h

#include "main.h"
#include "util.h"
#ifdef _WIN32
#include "Freeverb/freeverb/components/revmodel.hpp"
#else
#include "revmodel.hpp"
#endif

#ifdef __cplusplus
extern "C" {
#endif
///Allocates and returns a new freeverb::ReverbModel instance.
SOUNDSTAGE_API freeverb::ReverbModel* StereoVerb_New(int sampleRate);
///Releases all allocated resources.
SOUNDSTAGE_API void StereoVerb_Free(freeverb::ReverbModel* x);
///Sets a given parameter to a given value.
SOUNDSTAGE_API void StereoVerb_SetParam(int param, float value, freeverb::ReverbModel *x);
///Returns the current value of the selected parameter.
SOUNDSTAGE_API float StereoVerb_GetParam(int param, freeverb::ReverbModel *x);
///Clears all buffers of the freeverb::ReverbModel instance.
SOUNDSTAGE_API void StereoVerb_Clear(freeverb::ReverbModel *x);
///Processes 1 block of interleaved stereo audio data.
SOUNDSTAGE_API void StereoVerb_Process(float buffer[], int length, int channels, freeverb::ReverbModel *x);
#ifdef __cplusplus
}
#endif

#endif /* StereoVerb_h */
