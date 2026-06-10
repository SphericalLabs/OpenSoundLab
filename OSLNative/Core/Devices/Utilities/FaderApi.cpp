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

#include "FaderApi.h"
#include "AudioMath.h"
#include <math.h>

extern "C" {

void processFader(float buffer[], int length, int channels, float bufferB[], int lengthB, bool aSig, bool bSig,
                  bool samePercent, float lastpercent, float sliderPercent) {
    float p = sliderPercent;
    if (aSig && bSig) {
        for (int i = 0; i < length; i += channels) {
            if (!samePercent)
                p = lerp(lastpercent, sliderPercent, (float) i / length); // slope limiting

            buffer[i] = buffer[i] * powf(1 - p, 2) + bufferB[i] * powf(p, 2);
            buffer[i + 1] = buffer[i + 1] * powf(1 - p, 2) + bufferB[i + 1] * powf(p, 2);
        }
    } else {
        float modA = 0;
        float modB = 1;

        if (aSig) {

            p = 1 - p;
            modA = 1;
            modB = -1;
        }

        for (int i = 0; i < length; i += channels) {
            if (!samePercent)
                p = lerp(lastpercent, sliderPercent, (float) i / length) * modB + modA; // slope limiting
            buffer[i] *= powf(p, 2);
            buffer[i + 1] *= powf(p, 2);
        }
    }
}
}
