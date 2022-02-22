//
//  Artefact.cpp
//  SoundStageNative
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
