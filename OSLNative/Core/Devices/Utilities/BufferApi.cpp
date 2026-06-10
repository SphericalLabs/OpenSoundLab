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

#include "BufferApi.h"

extern "C" {

void SetArrayToFixedValue(float buf[], int length, float value) {
    for (int i = 0; i < length; ++i) // how pre-increment? clicks?
        buf[i] = value;
}

void SetArrayToSingleValue(float a[], int length, float val) {
    for (int i = 0; i < length; ++i)
        a[i] = val;
}

void MultiplyArrayBySingleValue(float buffer[], int length, float val) {
    for (int i = 0; i < length; ++i)
        buffer[i] *= val;
}

void AddArrays(float a[], float b[], int length) {
    for (int i = 0; i < length; ++i) {
        a[i] += b[i];
    }
}

void CopyArray(float from[], float to[], int length) {
    for (int i = 0; i < length; ++i) {
        to[i] = from[i];
    }
}

void DuplicateArrayAndReset(float from[], float to[], int length, float val) {
    for (int i = 0; i < length; ++i) {
        to[i] = from[i] * val;
        from[i] = 0;
    }
}

void NormalizeClip(float buffer[], int length) {
    float maxVal = 0;
    for (int i = 0; i < length; i += 2) {
        if (buffer[i] > maxVal)
            maxVal = buffer[i];
    }

    if (maxVal == 1)
        return;

    float mod = 1.0f / maxVal;
    for (int i = 0; i < length; ++i) {
        buffer[i] *= mod;
    }
}

void combineArrays(float buffer[], float bufferB[], int length, float levelA, float levelB) {
    for (int i = 0; i < length; ++i) {
        buffer[i] = buffer[i] * levelA + bufferB[i] * levelB;
    }
}
}
