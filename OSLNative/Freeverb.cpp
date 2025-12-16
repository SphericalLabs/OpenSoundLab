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

#include "Freeverb.h"

OSL_API freeverb::ReverbModel* Freeverb_New(int sampleRate) {
    return new freeverb::ReverbModel((double) sampleRate);
}
OSL_API void Freeverb_Free(freeverb::ReverbModel* x) {
    delete x;
}
OSL_API void Freeverb_SetParam(int param, float value, freeverb::ReverbModel* x) {
    switch (param) {
    case 0:
        x->setRoomSize(value);
        break;
    case 1:
        x->setDamping(value);
        break;
    case 2:
        x->setDryLevel(value);
        break;
    case 3:
        x->setWetLevel(value);
        break;
    case 4:
        x->setWidth(value);
        break;
    case 5:
        x->setFreezeMode(value);
        break;
    default:
        break;
    }
}
OSL_API float Freeverb_GetParam(int param, freeverb::ReverbModel* x) {
    switch (param) {
    case 0:
        return x->getRoomSize();
        break;
    case 1:
        return x->getDamping();
        break;
    case 2:
        return x->getDryLevel();
        break;
    case 3:
        return x->getWetLevel();
        break;
    case 4:
        return x->getWidth();
        break;
    case 5:
        return x->getFreezeMode();
        break;
    default:
        return -1;
        break;
    }
    return 0;
}
OSL_API void Freeverb_Clear(freeverb::ReverbModel* x) {
    x->clear();
}
OSL_API void Freeverb_Process(float buffer[], int length, int channels, freeverb::ReverbModel* x) {
    // This calls a modified FreeVerb function that operates on interleaved audio buffers and also considers the
    // modulation buffers:
    x->processInterleaved(buffer, length, channels);
}
