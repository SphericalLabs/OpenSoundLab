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

#include "EnvelopeMath.h"

float getADSR(int curFrame, float startVal, int frameCount, int frames[], float volumes[]) {
    switch (curFrame) {
    case 0:
        return startVal + (volumes[0] - startVal) * frameCount / frames[0];
    case 1:
        return volumes[0] + (volumes[1] - volumes[0]) * (float) frameCount / (float) frames[1];
    case 2:
        return volumes[1];
    case 3:
        return volumes[1] * (1.f - (float) frameCount / (float) frames[3]);
    case 4:
        return 0;
    default:
        break;
    }
    return 0;
}
