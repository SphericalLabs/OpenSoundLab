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

#include "Delay.h"
#include "util.h"
#include <string.h>
#include <math.h>
#include <assert.h>
#include <algorithm>

#define DELAY_MAXVECTORSIZE 4096

enum DelayParams
{
    P_TIME,
    P_FEEDBACK,
    P_WET,
    P_DRY,
    P_CLEAR,
    P_INTERPOLATION,
    P_MIN_SAMPLES,
    P_MAX_SAMPLES,
    P_N
};

/* This version uses oversampling at write time and is extremely expensive with small delay times. */
void Delay_ProcessPadded(float buffer[], int n, int channels, DelayData* x)
{
    RingBuffer *tap = (RingBuffer*)x->tap;
    
    // Prepare
    int time = x->time, prevTime = x->prevTime;
    float feedback = x->feedback;
    float prevFeedback = x->prevFeedback;
    float prevDry = x->prevDry;
    float prevWet = x->prevWet;
    float temp;
    
    assert(time > 0);
    
    float stride = (float)x->maxTime / (float)time;
        
    int nPerChannel = n/channels;
    if(channels > 1)
        _fDeinterleave(buffer, buffer, n, channels);
    
    // Process delay
    int m;
    int r = nPerChannel;
    float* bufOffset = buffer;
    while(r)
    {
        m = time < r ? time : r;
        
        RingBuffer_ReadPadded(x->temp, m, -(x->maxTime), stride, tap);
        
        ///Crossfade interpolation if delay time changed
        if(time != prevTime)
        {
            float prevOversampling = (float)x->maxTime / (float)prevTime;
            RingBuffer_ReadPadded(x->temp2, m, -(x->maxTime), prevOversampling, tap);
            _fCrossfadeLinear(x->temp2, x->temp, x->temp, m);
            prevTime = time;
        }
        
        _fCopy(x->temp, x->temp2, m);
        
        ///Multiply those samples with the feedback gain
        if(prevFeedback != feedback)
        {
            temp = prevFeedback + ((float)m/nPerChannel) * (feedback - prevFeedback);
            _fLerp(x->temp, x->temp, prevFeedback, temp, m);
            prevFeedback = temp;
        }
        else
        {
            _fScale(x->temp, x->temp, feedback, m);
        }
        
        _fAdd(bufOffset, x->temp, x->temp, m);
        RingBuffer_WritePadded(x->temp, m, stride, x->interpolation, tap);

        ///Scale the input samples and the previously read delay samples for output
        if(prevDry != x->dry)
        {
            temp = prevDry + ((float)m/nPerChannel) * (x->dry - prevDry);
            _fLerp(bufOffset, bufOffset, prevDry, temp, m);
            prevDry = temp;
        }
        else
        {
            _fScale(bufOffset, bufOffset, x->dry, m);
        }
        
        if(prevWet != x->wet)
        {
            temp = prevWet + ((float)m/nPerChannel) * (x->wet - prevWet);
            _fLerp(x->temp2, x->temp2, prevWet, temp, m);
            prevWet = temp;
        }
        else
        {
            _fScale(x->temp2, x->temp2, x->wet, m);
        }
            
        _fAdd(bufOffset, x->temp2, bufOffset, m);
        
        bufOffset += m;
        r -= m;
    }
    
    // Finalize
    if(channels > 1)
    {
        for(int i = 1; i < channels; i++)
        {
            _fCopy(buffer, buffer+i*nPerChannel, nPerChannel);
        }
        _fInterleave(buffer, buffer, n, channels);
    }
    
    x->prevTime = x->time;
    x->prevFeedback = x->feedback;
    x->prevWet = x->wet;
    x->prevDry = x->dry;
}

OSL_API void Delay_SetRange(int min, int max, DelayData* x)
{
    if(x->delayMode == DELAYMODE_PADDED && x->maxTime != max)
    {
        RingBuffer *tap = (RingBuffer*)x->tap;
        RingBuffer_Resize(max, tap);
        Delay_Clear(x);
        x->maxTime = max;
    }
}

OSL_API void Delay_SetMode(int mode, DelayData* x)
{
    if(mode == x->delayMode)
        return;
    
    switch (x->delayMode) {
        case DELAYMODE_PADDED:
        {
            if(mode == DELAYMODE_INTERPOLATED)
            {
                RingBuffer* tap1 = (RingBuffer*)x->tap;
                RingBuffer_Free(tap1);
                x->tap = (void*)FrameRingBuffer_New(12.5f * 48000); //TODO: this is an ugly hack
            }
            break;
        }
        case DELAYMODE_INTERPOLATED:
        {
            if(mode != DELAYMODE_INTERPOLATED)
            {
                FrameRingBuffer* tap2 = (FrameRingBuffer*)x->tap;
                FrameRingBuffer_Free(tap2);
                x->tap = (void*)RingBuffer_New(x->maxTime);
            }
            break;
        }
        default:
            break;
    }
    
    x->delayMode = mode;
    printv("Delay is now in mode %d\n", mode);
}

/* This is the version we are using right now. */
void Delay_ProcessInterpolated2(float buffer[], int n, int channels, float timeBuffer[], float feedbackBuffer[], float mixBuffer[], DelayData* x)
{
    FrameRingBuffer *tap = (FrameRingBuffer*)x->tap;
    
    ///Prepare
    int m, time = x->time, prevTime = x->prevTime, nPerChannel = n/channels, r = nPerChannel;
    float temp, feedback = x->feedback, prevFeedback = x->prevFeedback, prevDry = x->prevDry, prevWet = x->prevWet;
    float *bufOffset = buffer, *tb = x->cTime;
    
    if(time <= 0)
        time = 1;
        
    ///Deinterleave if necessary
    if(channels > 1)
    {
        _fDeinterleave(buffer, buffer, n, channels);
        if(timeBuffer) _fDeinterleave(timeBuffer, timeBuffer, n, channels);
        if(feedbackBuffer) _fDeinterleave(feedbackBuffer, feedbackBuffer, n, channels);
    }
    
    ///Generate control signal(s) and calculate average oversampling
    std::fill(x->cTime, x->cTime+nPerChannel, 1.0f);
    _fLerp(x->cTime, x->cTime, prevTime, time, nPerChannel);
    if(timeBuffer != NULL)
    {

        _fClamp(timeBuffer, -1, 1, nPerChannel);
        //_fScale(timeBuffer, timeBuffer, 0.5f, nPerChannel);
        //_fAddSingle(timeBuffer, 0.5f, timeBuffer, nPerChannel);
        for (int i = 0; i < nPerChannel; i++) {
          x->cTime[i] *= powf(2, timeBuffer[i] * 9.0f);
        }        
        //_fPow(timeBuffer, timeBuffer, 3, nPerChannel); // it's a bit unusual to apply pow to a cv input buffer at this stage
        //_fScale(timeBuffer, timeBuffer, 2.0f, nPerChannel);
        //_fAddSingle(timeBuffer, -1.0f, timeBuffer, nPerChannel);
        //_fScale(timeBuffer, timeBuffer, x->modeMaxTime - x->modeMinTime, nPerChannel); // -1,1 to actual mode range +/-
        //_fAdd(timeBuffer, x->cTime, x->cTime, nPerChannel);
        _fClamp(x->cTime, x->minSamples, x->maxSamples, nPerChannel);
        //_fClamp(x->cTime, 5, 30 * 48000, nPerChannel); // sync 
        
        // low ranges are broken, is clamping broken? 
        // consider making it respond in 1v/oct, pow(2, n) with modeMaxTime as reference

    }
    float oversampling = x->maxTime / _fAverageSumOfMags(x->cTime, nPerChannel); //this is the "average" oversampling over the whole input buffer. For writing, we use this to avoid many small frames in the ringbuffer, as this affects read performance negatively.
        
    ///We first read from the delay buffer and then write the new samples to it. If the delay buffer is smaller than n (the DSP vector size), we have to repeat this procedure until we consumed all n samples.
    while(r)
    {
        m = time < r ? time : r;
        
        FrameRingBuffer_Read2(x->temp, m, -x->maxTime, tb, false, x->interpolation, tap);
        
        _fCopy(x->temp, x->temp2, m);
        
        ///Multiply samples with the feedback gain
        temp = prevFeedback + ((float)m/nPerChannel) * (feedback - prevFeedback);
        _fLerp(x->temp, x->temp, prevFeedback, temp, m);
        prevFeedback = temp;
        
        /// Add the new input samples and write everything into the delay buffer
        _fAdd(bufOffset, x->temp, x->temp, m);
        FrameRingBuffer_Write(x->temp, m, oversampling, tap);

        ///Scale the input samples and the previously read delay samples for output
        temp = prevDry + ((float)m/nPerChannel) * (x->dry - prevDry);
        _fLerp(bufOffset, bufOffset, prevDry, temp, m);
        prevDry = temp;

        temp = prevWet + ((float)m/nPerChannel) * (x->wet - prevWet);
        _fLerp(x->temp2, x->temp2, prevWet, temp, m);
        prevWet = temp;
        
        _fAdd(bufOffset, x->temp2, bufOffset, m);
        
        bufOffset += m;
        tb += m;
        if(timeBuffer) timeBuffer += m;
        if(feedbackBuffer) feedbackBuffer += m;
        r -= m;
    }
    
    ///Finally, if there are > 1 output channels, we copy channel 1 to all other channels
    if(channels > 1)
    {
        for(int i = 1; i < channels; i++)
        {
            _fCopy(buffer, buffer+i*nPerChannel, nPerChannel);
        }
        /// We deinterleaved the data to process it, so we have to interleave it again
        _fInterleave(buffer, buffer, n, channels);
        if(timeBuffer) _fInterleave(timeBuffer, timeBuffer, n, channels);
        if(feedbackBuffer) _fInterleave(feedbackBuffer, feedbackBuffer, n, channels);
    }
    
    x->prevTime = x->time;
    x->prevFeedback = x->feedback;
    x->prevWet = x->wet;
    x->prevDry = x->dry;
}

OSL_API void Delay_Process(float buffer[], float timeBuffer[], float feedbackBuffer[], float mixBuffer[], int n, int channels, DelayData* x)
{
    if(x->delayMode == DELAYMODE_INTERPOLATED)
        Delay_ProcessInterpolated2(buffer, n, channels, timeBuffer, feedbackBuffer, mixBuffer, x);
    else if (x->delayMode == DELAYMODE_PADDED)
        Delay_ProcessPadded(buffer, n, channels, x);
}

OSL_API void Delay_Clear(DelayData* x)
{
    if(x->delayMode == DELAYMODE_INTERPOLATED)
        FrameRingBuffer_Clear((FrameRingBuffer*)x->tap);
    else
        _fZero(((RingBuffer*)x->tap)->buf, x->maxTime);
}

OSL_API void Delay_SetParam(float value, int param, struct DelayData *x)
{
    assert(param < P_N);
    
    int intval = (int)value;
        
    switch (param) {
        case P_TIME:
            x->prevTime = x->time;
            x->time = intval;
            //printv("Set delay time to %d\n", intval);
            break;
        case P_FEEDBACK:
            x->feedback = value;
            //printv("Set delay feedback to %f\n", value);
            break;
        case P_WET:
            x->wet = value;
            //printv("Set delay wet mix to %f\n", value);
            break;
        case P_DRY:
            x->dry = value;
            //printv("Set delay dry mix to %f\n", value);
            break;
        case P_INTERPOLATION:
            x->interpolation = intval;
            break;
        case P_MIN_SAMPLES:
            x->minSamples = intval;
            break;
        case P_MAX_SAMPLES:
            x->maxSamples = intval;
            break;
        default:
            break;
    }
}

OSL_API struct DelayData *Delay_New(int n)
{
    DelayData *x = (DelayData*)_malloc(sizeof(struct DelayData));
    x->tap = FrameRingBuffer_New(n);
    x->temp = (float*)_malloc(DELAY_MAXVECTORSIZE * sizeof(float));
    x->temp2 = (float*)_malloc(DELAY_MAXVECTORSIZE * sizeof(float));
    x->cTime = (float*)_malloc(DELAY_MAXVECTORSIZE * sizeof(float));
    _fZero(x->temp, DELAY_MAXVECTORSIZE);
    _fZero(x->temp2, DELAY_MAXVECTORSIZE);
    _fZero(x->cTime, DELAY_MAXVECTORSIZE);
    x->wet = 1;
    x->dry = 0.7f;
    x->time = n;
    x->prevTime = n;
    x->prevDry = 0.7f;
    x->prevWet = 1;
    x->prevFeedback = 0.3f;
    x->maxTime = n;
    x->feedback = 0.3f;
    x->interpolation = INTERPOLATION_LINEAR;
    x->delayMode = DELAYMODE_INTERPOLATED;
    return x;
}

OSL_API void Delay_Free(struct DelayData *x)
{
    if(x->delayMode == DELAYMODE_INTERPOLATED)
        FrameRingBuffer_Free((FrameRingBuffer*)x->tap);
    else
        RingBuffer_Free((RingBuffer*)x->tap);
    _free(x->temp);
    _free(x->temp2);
    _free(x->cTime);
    _free(x);
}
