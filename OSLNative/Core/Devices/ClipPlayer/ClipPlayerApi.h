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

#pragma once

#include "OSLNativeExport.h"

#ifdef __cplusplus
extern "C" {
#endif

OSL_API double ClipSignalGenerator(float buffer[], float freqExpBuffer[], float freqLinBuffer[], float ampBuffer[], float seqBuffer[], int length, float lastSeqGen[2], int channels, bool freqExpGen, bool freqLinGen, bool ampGen, bool seqGen, double floatingBufferCount, int sampleBounds[2], float playbackSpeed, float lastPlaybackSpeed, void* clip0, void* clip1, void* clip2, void* clip3, void* clip4, int clipFrames0, int clipFrames1, int clipFrames2, int clipFrames3, int clipFrames4, int availableLayers, int interpolationMode, bool useSampleLayers, int clipChannels, float amplitude, float lastAmplitude, bool playdirection, bool looping, double _sampleDuration, int bufferCount, bool& active, int windowLength);

#ifdef __cplusplus
}
#endif
