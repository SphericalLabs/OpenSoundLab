// Copyright 2017-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
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
//  Created by hb on 22.02.22.
//

#include "Artefact.h"
#include "util.h"

SOUNDSTAGE_API void Artefact_Process(float buffer[], float noiseAmount, int downsampleFactor, float jitterAmount, int bitReduction, int channels, int n)
{
    if(channels > 1)
    {
        _fDeinterleave(buffer, buffer, n, channels);
    }
    
    int nPerChan = n / channels;
    
    for(int i = 0; i < channels; i++)
    {
        _fNoiseAdditive(buffer+i*nPerChan, noiseAmount, nPerChan);
        _fJitter(buffer+i*nPerChan, jitterAmount, nPerChan);
        _fBitCrush(buffer+i*nPerChan, bitReduction, nPerChan);
        _fDownSample(buffer+i*nPerChan, downsampleFactor, nPerChan);
    }
    
    if(channels > 1)
    {
        _fInterleave(buffer, buffer, n, channels);
    }
}
