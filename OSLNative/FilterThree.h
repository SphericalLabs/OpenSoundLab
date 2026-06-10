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

#include "main.h"

struct Biquad;

// FilterThree is OSL's clean utility EQ.
// It is a six-stage chain of cookbook biquads: corrective and sculpting rather than characterful.

struct FilterThreeData {
    int channels;
    float sampleRate;
    Biquad* lowCut;
    Biquad* lowShelf;
    Biquad* bellOne;
    Biquad* bellTwo;
    Biquad* highShelf;
    Biquad* highCut;
};

extern "C" {
OSL_API FilterThreeData* FilterThree_New(int channels, float sampleRate);
OSL_API void FilterThree_Free(FilterThreeData* x);
OSL_API void FilterThree_Reset(FilterThreeData* x);
OSL_API void FilterThree_Process(FilterThreeData* x, float buffer[], int length, float lowCutFrequency,
                                 float lowShelfFrequency, float lowShelfGain, float bellOneFrequency,
                                 float bellOneGain, float bellOneQ, float bellTwoFrequency, float bellTwoGain,
                                 float bellTwoQ, float highShelfFrequency, float highShelfGain,
                                 float highCutFrequency);
}
