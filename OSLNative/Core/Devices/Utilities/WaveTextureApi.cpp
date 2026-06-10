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

#include "WaveTextureApi.h"

extern "C" {

void ProcessWaveTexture(float buffer[], int length, void* pixels, unsigned char Ra, unsigned char Ga, unsigned char Ba,
                        unsigned char Rb, unsigned char Gb, unsigned char Bb, int period, int waveheight, int wavewidth,
                        int& lastWaveH, int& curWaveW) {
    unsigned char* data = reinterpret_cast<unsigned char*>(pixels);

    for (int i = 0; i < length / period; ++i) {
        float temp = (buffer[i * period] + 1.0f) * .5f;
        temp = (temp > 1.0f) ? 1.0f : ((temp < 0.0f) ? 0.0f : temp);
        int curH = (int) ((waveheight - 1) * temp);
        for (int i2 = 0; i2 < waveheight; i2++) {
            unsigned char* pixel = data + 4 * (i2 * wavewidth + curWaveW);
            if (lastWaveH >= i2 && i2 >= curH) {
                pixel[0] = Ra;
                pixel[1] = Ga;
                pixel[2] = Ba;
                pixel[3] = 255;
            } else if (lastWaveH <= i2 && i2 <= curH) {
                pixel[0] = Ra;
                pixel[1] = Ga;
                pixel[2] = Ba;
                pixel[3] = 255;
            } else {
                pixel[0] = Rb;
                pixel[1] = Gb;
                pixel[2] = Bb;
                pixel[3] = 255;
            }
        }

        lastWaveH = curH;
        curWaveW = (curWaveW + 1) % wavewidth;
    }
}
}
