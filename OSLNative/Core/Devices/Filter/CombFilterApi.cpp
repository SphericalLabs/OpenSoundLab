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

#include "CombFilterApi.h"

extern "C" {

void addCombFilterSignal(float inputbuffer[], float addbuffer[], int length, float delayBufferL[], float delayBufferR[],
                         int delaylength, float gain, int& inPoint, int& outPoint) {
    float inputL = 0;
    float inputR = 0;

    for (int i = 0; i < length; i += 2) {
        inputL = inputbuffer[i];
        inputR = inputbuffer[i + 1];

        delayBufferL[inPoint] = inputbuffer[i] + delayBufferL[outPoint] * gain;
        delayBufferR[inPoint] = inputbuffer[i + 1] + delayBufferR[outPoint] * gain;

        inPoint++;
        if (inPoint == delaylength)
            inPoint = 0;

        addbuffer[i] += delayBufferL[outPoint];
        addbuffer[i + 1] += delayBufferR[outPoint];

        outPoint++;
        if (outPoint == delaylength)
            outPoint = 0;
    }
}

void processCombFilterSignal(float buffer[], int length, float delayBufferL[], float delayBufferR[], int delaylength,
                             float gain, int& inPoint, int& outPoint) {
    float inputL = 0;
    float inputR = 0;

    for (int i = 0; i < length; i += 2) {
        delayBufferL[inPoint] = inputL = buffer[i] + delayBufferL[outPoint] * gain;
        delayBufferR[inPoint] = inputR = buffer[i + 1] + delayBufferR[outPoint] * gain;

        inPoint++;
        if (inPoint == delaylength)
            inPoint = 0;

        buffer[i] = delayBufferL[outPoint] - gain * inputL;
        buffer[i + 1] = delayBufferR[outPoint] - gain * inputR;

        outPoint++;
        if (outPoint == delaylength)
            outPoint = 0;
    }
}

void lowpassSignal(float buffer[], int length, float& lowpassL, float& lowpassR) {
    for (int i = 0; i < length; i += 2) {
        buffer[i] = lowpassL = 0.7f * lowpassL + 0.3f * buffer[i];
        buffer[i + 1] = lowpassR = 0.7f * lowpassR + 0.3f * buffer[i + 1];
    }
}
}
