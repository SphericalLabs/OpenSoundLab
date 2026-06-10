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

#include "EnvelopeApi.h"
#include "AudioMath.h"
#include "EnvelopeMath.h"

extern "C" {

void ADSRSignalGenerator(float buffer[], int length, int channels, int frames[], int& frameCount, bool active,
                         float& ADSRvolume, float volumes[], float startVal, int& curFrame, bool sustaining) {
    if (!active) {
        for (int i = 0; i < length; i += channels) {
            buffer[i] = buffer[i + 1] = 0.f;
        }
        return;
    }

    for (int i = 0; i < length; i += channels) {
        buffer[i + 1] = buffer[i] = ADSRvolume =
            lerp(getADSR(curFrame, startVal, frameCount, frames, volumes), ADSRvolume, .98f);

        if (curFrame != 2)
            frameCount++;
        else if (curFrame == 2 && !sustaining)
            frameCount++;

        if (curFrame < 4) {
            if (frameCount >= frames[curFrame]) {
                curFrame++;
                frameCount = 0;
            }
        }
    }
}
}
