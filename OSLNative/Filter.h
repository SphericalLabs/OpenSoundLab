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
#pragma once

// #include <string.h> // for memset

struct FilterData {
    float f, p, q;            // filter coefficients
    float b0, b1, b2, b3, b4; // filter buffers (beware denormals!)
    bool LP;
};

extern "C" {
typedef void (*LoggerFuncPtr)(int level, const char*); // Unity Delegate

OSL_API void processStereoFilter(float buffer[], int length, FilterData* mfL, FilterData* mfR, float cutoffFrequency,
                                 float lastCutoffFrequency, bool freqGen, float filterBuffer[],
                                 float resonance /*, LoggerFuncPtr logger*/);
}
