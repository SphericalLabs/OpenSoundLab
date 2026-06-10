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

#include "OscillatorApi.h"
#include "Oscillator.h"

extern "C" {

OscillatorProcessor* CreateOscillatorProcessor(float sampleRate) {
    return oscillatorCreateProcessor(sampleRate);
}

void DestroyOscillatorProcessor(OscillatorProcessor* processor) {
    oscillatorDestroyProcessor(processor);
}

void OscillatorSetPhase(OscillatorProcessor* processor, double phase) {
    oscillatorSetPhase(processor, phase);
}

void OscillatorSignalGenerator(OscillatorProcessor* processor, float buffer[], int length, int channels, double& _phase,
                               float analogWave, bool bLfo, float frequency, float amplitude,
                               float frequencyExpBuffer[], float frequencyLinBuffer[], float amplitudeBuffer[],
                               float syncBuffer[], float pwmBuffer[], bool bFreqExpGen, bool bFreqLinGen, bool bAmpGen,
                               bool bSyncGen, bool bPwmGen, double& dspTime) {
    oscillatorProcessBuffer(processor, buffer, length, channels, _phase, analogWave, bLfo, frequency, amplitude,
                            frequencyExpBuffer, frequencyLinBuffer, amplitudeBuffer, syncBuffer, pwmBuffer,
                            bFreqExpGen, bFreqLinGen, bAmpGen, bSyncGen, bPwmGen, dspTime);
}
}
