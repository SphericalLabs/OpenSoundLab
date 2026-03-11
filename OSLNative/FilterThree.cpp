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

#include "FilterThree.h"
#include "Biquad.h"
#include "util.h"

extern "C" {

OSL_API FilterThreeData* FilterThree_New(int channels, float sampleRate) {
    FilterThreeData* x = (FilterThreeData*) _malloc(sizeof(FilterThreeData));
    x->channels = channels > 0 ? channels : 2;
    x->sampleRate = sampleRate > 0.f ? sampleRate : 48000.f;
    x->lowCut = Biquad_new(BIQUAD_HIGHPASS, 20.f, 0.7071f, 0.f, x->sampleRate, x->channels);
    x->lowShelf = Biquad_new(BIQUAD_LOWSHELF, 120.f, 1.f, 0.f, x->sampleRate, x->channels);
    x->bellOne = Biquad_new(BIQUAD_PEAK, 400.f, 0.7f, 0.f, x->sampleRate, x->channels);
    x->bellTwo = Biquad_new(BIQUAD_PEAK, 2000.f, 0.7f, 0.f, x->sampleRate, x->channels);
    x->highShelf = Biquad_new(BIQUAD_HIGHSHELF, 5000.f, 1.f, 0.f, x->sampleRate, x->channels);
    x->highCut = Biquad_new(BIQUAD_LOWPASS, 20000.f, 0.7071f, 0.f, x->sampleRate, x->channels);
    return x;
}

OSL_API void FilterThree_Free(FilterThreeData* x) {
    if (x == nullptr)
        return;

    Biquad_free(x->lowCut);
    Biquad_free(x->lowShelf);
    Biquad_free(x->bellOne);
    Biquad_free(x->bellTwo);
    Biquad_free(x->highShelf);
    Biquad_free(x->highCut);
    _free(x);
}

OSL_API void FilterThree_Reset(FilterThreeData* x) {
    if (x == nullptr)
        return;

    Biquad_reset(x->lowCut);
    Biquad_reset(x->lowShelf);
    Biquad_reset(x->bellOne);
    Biquad_reset(x->bellTwo);
    Biquad_reset(x->highShelf);
    Biquad_reset(x->highCut);
}

OSL_API void FilterThree_Process(FilterThreeData* x, float buffer[], int length, float lowCutFrequency,
                                 float lowShelfFrequency, float lowShelfGain, float bellOneFrequency,
                                 float bellOneGain, float bellOneQ, float bellTwoFrequency, float bellTwoGain,
                                 float bellTwoQ, float highShelfFrequency, float highShelfGain,
                                 float highCutFrequency) {
    if (x == nullptr || buffer == nullptr || length <= 0)
        return;

    Biquad_process(x->lowCut, BIQUAD_HIGHPASS, lowCutFrequency, 0.7071f, 0.f, x->sampleRate, buffer, buffer, length);
    Biquad_process(x->lowShelf, BIQUAD_LOWSHELF, lowShelfFrequency, 1.f, lowShelfGain, x->sampleRate, buffer, buffer,
                   length);
    Biquad_process(x->bellOne, BIQUAD_PEAK, bellOneFrequency, bellOneQ, bellOneGain, x->sampleRate, buffer, buffer,
                   length);
    Biquad_process(x->bellTwo, BIQUAD_PEAK, bellTwoFrequency, bellTwoQ, bellTwoGain, x->sampleRate, buffer, buffer,
                   length);
    Biquad_process(x->highShelf, BIQUAD_HIGHSHELF, highShelfFrequency, 1.f, highShelfGain, x->sampleRate, buffer,
                   buffer, length);
    Biquad_process(x->highCut, BIQUAD_LOWPASS, highCutFrequency, 0.7071f, 0.f, x->sampleRate, buffer, buffer, length);
}
}
