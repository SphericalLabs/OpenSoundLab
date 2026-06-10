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

#include "OSLNativeExport.h"

// FilterTwo is OSL's modulatable multimode synth filter.
// It uses a TPT / zero-delay-feedback state-variable design in the Zavalishin / Simper family:
// cleaner and more coherent than the ladder, but intended to stay musical under audio-rate cutoff modulation.

enum FilterTwoMode {
    FILTERTWO_LP = 0,
    FILTERTWO_HP = 1,
    FILTERTWO_BP = 2,
    FILTERTWO_NOTCH = 3
};

struct FilterTwoData {
    int channels;
    float sampleRate;
    int lastMode;
    float* lowEq;
    float* bandEq;
    float* notchLowEq[3];
    float* notchBandEq[3];
};

extern "C" {
OSL_API FilterTwoData* FilterTwo_New(int channels, float sampleRate);
OSL_API void FilterTwo_Free(FilterTwoData* x);
OSL_API void FilterTwo_Reset(FilterTwoData* x);
OSL_API void FilterTwo_Process(FilterTwoData* x, float buffer[], int length, float cutoffPercent,
                               float lastCutoffPercent, float minCutoffHz, float maxCutoffHz,
                               float modulationOctaveRange, float frequencyBuffer[], float resonance,
                               float lastResonance, int mode, int oversampling);
}
