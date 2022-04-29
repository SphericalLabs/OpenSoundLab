// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of SoundStage Lab.
//
// SoundStage Lab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// SoundStage Lab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SoundStage Lab.  If not, see <http://www.gnu.org/licenses/>.

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
