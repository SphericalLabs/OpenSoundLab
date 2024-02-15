// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
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
// Copyright © 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright © 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright © 2017 Apache 2.0 Google LLC SoundStage VR
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

#include "Compressor.h"
#include "util.h"
#include <string.h>
#include <math.h>
#include <assert.h>

#define MAX_LOOKAHEAD 100
#define DELAY_ATTACK 10

enum CompressorParams
{
    P_ATTACK, //0
    P_RELEASE, //1
    P_THRESHOLD, //2
    P_RATIO, //3
    P_KNEE, //4
    P_MAKEUP, //5
    P_LOOKAHEAD, //6
    P_LIMIT, //7
    P_BYPASS, //8
    P_N
};

float _coeff(float t, float samplerate) {
    float smpls = (t / 1000) * samplerate;
    return powf((1-one_minus_oneOverE), (1.0/smpls));
}

inline float _interpolate(float *buf, float index)
{
    int floored = (int)index;
    float f2 = index - floored;
    float f1 = 1 - f2;
    return f1 * buf[floored] + f2 * buf[floored + 1];
}

OSL_API void Compressor_Process(float buffer[], float sc[], int length, int channels, CompressorData* x)
{
    ///Attention: inbuffer & outbuffer are interleaved!
    
    x->clipping = false;
    x->attenuation = 0.0f;
    
    if (channels != 2)
    {
        return;
    }
    
    ///TODO: This causes the compressor to not kick in immediately after periods of silence!
    /*float sumIn = _fSumOfMags(buffer, length);
    if(sumIn < 0.0000000001)
    {
        return;
    }*/
        
    float threshold = x->params[P_THRESHOLD];
    float ratio = x->params[P_RATIO];
    float knee = x->params[P_KNEE];
    float makeup = x->params[P_MAKEUP];
    
    float attenuation = 0;
    float tmp;
    float xG;
    float yG;
    float xL;
    float y1;
    float yL;
    float cdb;
    float c;
    int lookaheadSmpls = _mstosmpls(x->params[P_LOOKAHEAD], x->sampleRate);
    int delay;
    float *inBuf = buffer;
    float *sidechain = sc;
    float *outBuf = buffer;
    int readPtr;
    int n = length / channels;
    while(n--)
    {
        ///"Lookahead" is implemented by delaying the input signal.
        ///Envelope smoothing is applied to delay time to reduce artifacts at sudden changes
        delay = (lookaheadSmpls == x->d_prev) ? lookaheadSmpls : (int)roundf( x->aD * x->d_prev + (1.0f - x->aD) * lookaheadSmpls );
        
        readPtr = x->bufPtr - channels * delay;
        while(readPtr < 0)
            readPtr += x->bufLength;
        
        for(int k = 0; k < channels; k++)
        {
            ///Write sample to lookahead buffer
            x->buf[x->bufPtr+k] = inBuf[k];
            
            ///Convert to decibel so we can operate in log domain
            if(!x->params[P_LIMIT])
                xG = _atodb(fabsf(sidechain[k]));
            ///If we are in normal compressor mode, we want to look only at the sidechain signal. In the most common usecase of a compressor, the sidechain signal is the same signal as the audio signal to process. Using a distinct signal as a sidechain (for example a kick drum) to compress another signal allows for the typical "ducking" effect used in modern electronic music.
            else
             xG = _atodb(_max(fabs(sidechain[k]), fabs(x->buf[readPtr+k])));
            ///On the other hand, if we are in limiter mode, we look at current sidechain value as well as current input signal value, and use whichever is bigger to calculate the gain reduction. If we would only use the sidechain, then transients that are shorter than the lookahead time will not get attenuated.
            
            ///apply threshold, ratio, knee: eq.4
            tmp = 2*(xG - threshold);
            ///signal is below treshold and not within knee, no attenuation at all:
            if(tmp <= -(knee)) yG = xG;
            ///signal is above threshold and not within knee, full attenuation:
            else if ((tmp > knee)) yG = threshold + (xG - threshold) / ratio;
            ///signal is within knee, attenuate smoothly:
            else yG = xG + (1/ratio-1) * powf(xG - threshold + knee/2, 2) / (2*knee);
            
            ///smooth level detection: eq.17
            ///The calculated attenuation is smoothed by an envelope follower.
            xL = xG - yG;
            y1 = _max(xL, x->aR * x->y1_prev[k] + (1 - x->aR) * xL);
            yL = x->aA * x->yL_prev[k] + (1 - x->aA) * y1;
            ///If compressor is in bypass state, we still apply the makeup gain.
            ///People should not get the impression that a compressor "makes things louder"
            ///bc it is just wrong...
            cdb = x->params[P_BYPASS] ? makeup : -yL + makeup;
            attenuation += -yL;
            c = _dbtoa(cdb);
            
            ///store current samples for later
            x->y1_prev[k] = y1;
            x->yL_prev[k] = yL;
            
            ///write final output sample
            outBuf[k] = c * x->buf[readPtr+k];
            
            ///Check if clipping
            if(fabs(outBuf[k]) > 1)
                x->clipping = true;
        }
        
        ///Increment pointers
        outBuf += channels;
        sidechain += channels;
        inBuf += channels;
        x->bufPtr = (x->bufPtr + channels) % x->bufLength;
        x->d_prev = delay;
    }

    x->attenuation = x->params[P_BYPASS] ? 0 : attenuation / length;
    return;
}


OSL_API struct CompressorData *Compressor_New(float sampleRate)
{
    struct CompressorData *x = (struct CompressorData *)_malloc(sizeof(struct CompressorData));
    
    //public
    float *params = (float*)_malloc(P_N * sizeof(float));
    params[P_LOOKAHEAD] = 0;
    params[P_KNEE] = 0;
    params[P_RATIO] = 1;
    params[P_ATTACK] = 0;
    params[P_RELEASE] = 0;
    params[P_THRESHOLD] = 0;
    params[P_MAKEUP] = 0;
    params[P_LIMIT] = 0;
    params[P_BYPASS] = 0;
    x->params = params;
    
    //internal
    x->aA = 0;
    x->aR = 0;
    x->aD = _coeff(DELAY_ATTACK, sampleRate);
    x->y1_prev[0] = 0;
    x->yL_prev[0] = 0;
    x->y1_prev[1] = 0;
    x->yL_prev[1] = 0;
    x->d_prev = 0;
    x->sampleRate = sampleRate;
    x->bufLength = _nextPowOf2 ( 2 * _mstosmpls(MAX_LOOKAHEAD, sampleRate) );
    x->buf = (float*)_malloc(x->bufLength * sizeof(float));
    _fZero(x->buf, x->bufLength);
    x->bufPtr = 0;
    x->attenuation = 0;
    
    printv("Created new Compressor instance with sampleRate %f\n", sampleRate);
    
    return x;
}

OSL_API void Compressor_Free(struct CompressorData *x)
{
    free(x->buf);
    free(x->params);
    free(x);
}

OSL_API void Compressor_SetParam(float value, int param, struct CompressorData *x)
{
    assert(param < P_N);
        
    switch (param) {
        case P_ATTACK:
            if(value != x->params[param])
                x->aA = _coeff(value, x->sampleRate);
            break;
        case P_RELEASE:
            if(value != x->params[param])
                x->aR = _coeff(value, x->sampleRate);
            break;
        case P_LOOKAHEAD:
            assert(value <= MAX_LOOKAHEAD);
            break;
        default:
            break;
    }
    
    x->params[param] = value;
}

OSL_API float Compressor_GetAttenuation(struct CompressorData *x)
{
    return x->attenuation;
}

OSL_API bool Compressor_IsRamping(struct CompressorData *x)
{
    return (_mstosmpls(x->params[P_LOOKAHEAD], x->sampleRate) == x->d_prev);
}

OSL_API bool Compressor_IsClipping(struct CompressorData *x)
{
    return x->clipping;
}
