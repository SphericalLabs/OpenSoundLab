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

/*
 Hannes Barfuss 17.12.2021

 These are just some wrapper functions for FreeVerb.

 All functions are not thread-safe, hence the caller must avoid simultaneous access from multiple threads.

 */

#ifndef Freeverb_h
#define Freeverb_h

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
/// Allocates and returns a new freeverb::ReverbModel instance.
OSL_API freeverb::ReverbModel* Freeverb_New(int sampleRate);
/// Releases all allocated resources.
OSL_API void Freeverb_Free(freeverb::ReverbModel* x);
/// Sets a given parameter to a given value.
OSL_API void Freeverb_SetParam(int param, float value, freeverb::ReverbModel* x);
/// Returns the current value of the selected parameter.
OSL_API float Freeverb_GetParam(int param, freeverb::ReverbModel* x);
/// Clears all buffers of the freeverb::ReverbModel instance.
OSL_API void Freeverb_Clear(freeverb::ReverbModel* x);
/// Processes 1 block of interleaved stereo audio data.
OSL_API void Freeverb_Process(float buffer[], int length, int channels, freeverb::ReverbModel* x);
#ifdef __cplusplus
}
#endif

#endif /* StereoVerb_h */
