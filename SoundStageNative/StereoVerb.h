// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of SoundStage Lab.
//
// SoundStage Lab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// SoundStage Lab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SoundStage Lab.  If not, see <http://www.gnu.org/licenses/>.

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
