// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 
// 
// Copyright © 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright © 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright © 2017 Apache 2.0 Google LLC SoundStage VR
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
///Allocates and returns a new freeverb::ReverbModel instance.
SOUNDSTAGE_API freeverb::ReverbModel* Freeverb_New(int sampleRate);
///Releases all allocated resources.
SOUNDSTAGE_API void Freeverb_Free(freeverb::ReverbModel* x);
///Sets a given parameter to a given value.
SOUNDSTAGE_API void Freeverb_SetParam(int param, float value, freeverb::ReverbModel *x);
///Returns the current value of the selected parameter.
SOUNDSTAGE_API float Freeverb_GetParam(int param, freeverb::ReverbModel *x);
///Clears all buffers of the freeverb::ReverbModel instance.
SOUNDSTAGE_API void Freeverb_Clear(freeverb::ReverbModel *x);
///Processes 1 block of interleaved stereo audio data.
SOUNDSTAGE_API void Freeverb_Process(float buffer[], int length, int channels, freeverb::ReverbModel *x);
#ifdef __cplusplus
}
#endif

#endif /* StereoVerb_h */
