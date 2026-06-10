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

#include "MaracaApi.h"
#include <math.h>

#define PI 3.14159265

extern "C" {

void MaracaProcessBuffer(float buffer[], int length, int channels, float amp, double& _phase, double _sampleDuration) {
    for (int i = 0; i < length; i += channels) {
        buffer[i] = buffer[i + 1] = ((amp * sin(_phase * 2 * PI)) - .5f) * 2;
        ;
        _phase += amp * 16 * _sampleDuration;
    }
}

void MaracaProcessAudioBuffer(float buffer[], float controlBuffer[], int length, int channels, double& _phase,
                              double _sampleDuration) {
    for (int i = 0; i < length; i += channels) {
        float sample = sin(_phase * 2 * PI);
        buffer[i] = buffer[i + 1] = ((controlBuffer[i] + 1) / 2.0f) * sample;

        float endFrequency = 200.0f + ((controlBuffer[i] + 1) / 2.0f) * 300.0f;
        _phase += endFrequency * _sampleDuration;
        if (_phase > 1.0)
            _phase -= 1.0;
    }
}
}
