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

#include "NoiseApi.h"
#include "pcg_random.hpp"
#include <random>

extern "C" {

// Define the NoiseProcessor structure in C++
struct NoiseProcessor {
    pcg32 rng;
    std::uniform_real_distribution<float> dist;
    uint64_t seed;
    uint64_t step;
    NoiseProcessor(uint64_t seed) : rng(seed), dist(-1.0f, 1.0f), seed(seed), step(0) {
    }
    float generate() {
        step++;
        return dist(rng);
    }
    void jump_ahead(uint64_t steps) {
        rng.advance(steps);
        step += steps;
    }
};

NoiseProcessor* CreateNoiseProcessor(int seed) {
    return new NoiseProcessor(seed);
}

void DestroyNoiseProcessor(NoiseProcessor* processor) {
    delete processor;
}

void NoiseProcessBuffer(NoiseProcessor* processor, float* buffer, int length, int channels, float sampleRatePercent,
                        float* lastSample, int* counter, int speedFrames, bool* updated) {
    if (sampleRatePercent > .95f) {
        *updated = true;
        for (int i = 0; i < length; i += channels) {
            *lastSample = processor->generate();
            for (int c = 0; c < channels; ++c) {
                buffer[i + c] = *lastSample;
            }
        }
    } else {
        for (int i = 0; i < length; i += channels) {
            (*counter)++;
            if (*counter > speedFrames) {
                *updated = true;
                *counter = 0;
                *lastSample = processor->generate();
            }
            for (int c = 0; c < channels; ++c) {
                buffer[i + c] = *lastSample;
            }
        }
    }
}

void SyncNoiseProcessor(NoiseProcessor* processor, int seed, int steps) {
    processor->rng.seed(seed);
    processor->rng.advance(steps);
    processor->seed = seed;
    processor->step = steps;
}

int GetCurrentSeed(NoiseProcessor* processor) {
    return processor->seed;
}

int GetCurrentStep(NoiseProcessor* processor) {
    return processor->step;
}
}
