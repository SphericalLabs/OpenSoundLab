// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright � 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
//
// Copyright � 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright � 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright � 2017 Apache 2.0 Google LLC SoundStage VR
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
//  Created by hb on 17.02.22.
//

#ifndef resample_h
#define resample_h

#include "main.h"
#include "CRingBuffer.hpp"

#define VALUES_PER_ZEROCROSSING 4096
#define ZEROCROSSINGS_PER_AXIS 11
#define CUTOFF_FREQ_NORMALIZED 0.85
#define CONV_LENGTH 2 * ZEROCROSSINGS_PER_AXIS + 1
#define TABLE_SIZE (VALUES_PER_ZEROCROSSING * (ZEROCROSSINGS_PER_AXIS + 1))

typedef struct _sincTable {
    float val[TABLE_SIZE];
    float diff[TABLE_SIZE];
    float temp[CONV_LENGTH];
    int ptr;
} sincTable;

#ifdef __cplusplus
extern "C" {
#endif

void createResampleTable();
/* ratio must be in range [-1...1]. */
float wsinc_resample(float smpls[CONV_LENGTH], float ratio);
// A global buffer you can use to store the input samples so you don't have to allocate extra memory for it.
extern float wsinc_convBuffer[CONV_LENGTH];

#ifdef __cplusplus
}
#endif

#endif /* resample_h */
