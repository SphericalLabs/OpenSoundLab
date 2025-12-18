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

//
//  Created by hb on 22.02.22.
//

#include "Artefact.h"
#include "util.h"

OSL_API void Artefact_Process(float buffer[], float noiseAmount, int downsampleFactor, float jitterAmount,
                              int bitReduction, int channels, int n) {
    if (channels > 1) {
        _fDeinterleave(buffer, buffer, n, channels);
    }

    int nPerChan = n / channels;

    for (int i = 0; i < channels; i++) {
        _fNoiseAdditive(buffer + i * nPerChan, noiseAmount, nPerChan);
        _fJitter(buffer + i * nPerChan, jitterAmount, nPerChan);
        _fBitCrush(buffer + i * nPerChan, bitReduction, nPerChan);
        _fDownSample(buffer + i * nPerChan, downsampleFactor, nPerChan);
    }

    if (channels > 1) {
        _fInterleave(buffer, buffer, n, channels);
    }
}
