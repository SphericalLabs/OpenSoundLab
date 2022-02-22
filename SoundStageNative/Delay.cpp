
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
    P_N
};

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
            _fCrossfadeLogarithmic(x->temp2, x->temp, x->temp, true, m);
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

void Delay_ProcessSimple(float buffer[], int n, int channels, DelayData* x) {
    RingBuffer *tap = (RingBuffer*)x->tap;

    //Read samples from ringbuffer
    RingBuffer_Read(x->temp, n, -(x->time), tap);
    
    ///Crossfade interpolation if delay time changed
    if(x->time != x->prevTime)
    {
        RingBuffer_Read(x->temp2, n, -(x->prevTime), tap);
        _fCrossfadeLinear(x->temp2, x->temp, x->temp, n);
    }
    
    //Scale samples for feeding back
    _fScale(x->temp, x->temp, x->feedback, n);
    //Add input samples
    _fAdd(buffer, x->temp, x->temp, n);
    //Write everything to ringbuffer
    RingBuffer_Write(x->temp, n, tap);

    //Scale the direct sound
    _fScale(buffer, buffer, x->dry, n);
    //Read the same samples once again from ringbuffer (note that the buffer pointer has changed, so we need -(n + x->time) now)
    //This is not ideal, can't we do the same thing without copying twice?!
    RingBuffer_Read(x->temp, n, -(n + x->time), tap);
    //Scale delayed samples for output
    _fScale(x->temp, x->temp, x->wet, n);

    //Add scaled delay samples to output
    _fAdd(buffer, x->temp, buffer, n);
    
    x->prevTime = x->time;
}

SOUNDSTAGE_API void Delay_SetRange(int min, int max, DelayData* x)
{
    if(x->delayMode == DELAYMODE_PADDED && x->maxTime != max)
    {
        RingBuffer *tap = (RingBuffer*)x->tap;
        RingBuffer_Resize(max, tap);
        Delay_Clear(x);
        x->maxTime = max;
    }
}

SOUNDSTAGE_API void Delay_SetMode(int mode, DelayData* x)
{
    if(mode == x->delayMode)
        return;
    
    switch (x->delayMode) {
        case DELAYMODE_SIMPLE || DELAYMODE_PADDED:
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

void Delay_ProcessInterpolated(float buffer[], int n, int channels, DelayData* x)
{
    FrameRingBuffer *tap = (FrameRingBuffer*)x->tap;
    
    // Prepare
    int time = x->time, prevTime = x->prevTime;
    float feedback = x->feedback;
    float prevFeedback = x->prevFeedback;
    float prevDry = x->prevDry;
    float prevWet = x->prevWet;
    float temp;
    
    if(time <= 0)
        time = 1;
    
    float oversampling = (float)x->maxTime / (float)time;
        
    int nPerChannel = n/channels;
    if(channels > 1)
        _fDeinterleave(buffer, buffer, n, channels);
    
    // Process delay
    int m;
    int r = nPerChannel;
    float* bufOffset = buffer;
        
    ///We first read from the delay buffer and then write the new samples to it. If the delay buffer is smaller than n (the DSP vector size), we have to repeat this procedure until we consumed all n samples.
    while(r)
    {
        m = time < r ? time : r;
        
        ///Read some samples from the delay buffer
        FrameRingBuffer_Read(x->temp, m, -time, oversampling, x->interpolation, tap);
        
        ///Crossfade interpolation if delay time changed
        if(time != prevTime)
        {
            float prevOversampling = (float)x->maxTime / (float)prevTime;
            FrameRingBuffer_Read(x->temp2, m, -prevTime, prevOversampling, x->interpolation, tap);
            _fCrossfadeLogarithmic(x->temp2, x->temp, x->temp, true, m);
            prevTime = time;
        }
        
        _fCopy(x->temp, x->temp2, m);
        
        ///Multiply those samples with the feedback gain
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
    }
    
    x->prevTime = x->time;
    x->prevFeedback = x->feedback;
    x->prevWet = x->wet;
    x->prevDry = x->dry;
}

/*void Delay_ProcessInterpolated2(float buffer[], int n, int channels, float timeBuffer[], float feedbackBuffer[], DelayData* x)
{
    FrameRingBuffer *tap = (FrameRingBuffer*)x->tap;
    
    // Prepare
    int time = x->time, prevTime = x->prevTime;
    float feedback = x->feedback;
    float prevFeedback = x->prevFeedback;
    float prevDry = x->prevDry;
    float prevWet = x->prevWet;
    float temp;
    
    if(time <= 0)
        time = 1;
    
    float oversampling = (float)x->maxTime / (float)time;
        
    int nPerChannel = n/channels;
    if(channels > 1)
    {
        _fDeinterleave(buffer, buffer, n, channels);
        if(timeBuffer) _fDeinterleave(timeBuffer, timeBuffer, n, channels);
        if(feedbackBuffer) _fDeinterleave(feedbackBuffer, feedbackBuffer, n, channels);
    }
    
    // Process delay
    int m;
    int r = nPerChannel;
    float* bufOffset = buffer;
        
    ///We first read from the delay buffer and then write the new samples to it. If the delay buffer is smaller than n (the DSP vector size), we have to repeat this procedure until we consumed all n samples.
    while(r)
    {
        m = time < r ? time : r;
        
        temp = prevTime + ((float)m/nPerChannel) * (time - prevTime);
        std::fill(x->temp2, x->temp2+m, 1.0f);
        _fLerp(x->temp2, x->temp2, prevTime, temp, m);
        prevTime = temp;
        if(timeBuffer != NULL)
        {
            _fAdd(timeBuffer, x->temp2, x->temp2, m);
            _fClamp(x->temp2, 1, tap->n, m);
        }
        FrameRingBuffer_Read2(x->temp, m, -x->maxTime, x->temp2, false, x->interpolation, tap);
        
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
}*/

SOUNDSTAGE_API void Delay_Process(float buffer[], int n, int channels, DelayData* x)
{
    if(x->delayMode == DELAYMODE_INTERPOLATED)
        Delay_ProcessInterpolated(buffer, n, channels, x);
    else if (x->delayMode == DELAYMODE_PADDED)
        Delay_ProcessPadded(buffer, n, channels, x);
    else if(x->delayMode == DELAYMODE_SIMPLE)
        Delay_ProcessSimple(buffer, n, channels, x);
}

SOUNDSTAGE_API void Delay_Clear(DelayData* x)
{
    if(x->delayMode == DELAYMODE_INTERPOLATED)
        FrameRingBuffer_Clear((FrameRingBuffer*)x->tap);
    else
        _fZero(((RingBuffer*)x->tap)->buf, x->maxTime);
}

SOUNDSTAGE_API void Delay_SetParam(float value, int param, struct DelayData *x)
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
        default:
            break;
    }
}

SOUNDSTAGE_API struct DelayData *Delay_New(int n)
{
    DelayData *x = (DelayData*)_malloc(sizeof(struct DelayData));
    x->tap = FrameRingBuffer_New(n);
    x->temp = (float*)_malloc(DELAY_MAXVECTORSIZE * sizeof(float));
    x->temp2 = (float*)_malloc(DELAY_MAXVECTORSIZE * sizeof(float));
    _fZero(x->temp, DELAY_MAXVECTORSIZE);
    _fZero(x->temp2, DELAY_MAXVECTORSIZE);
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

SOUNDSTAGE_API void Delay_Free(struct DelayData *x)
{
    if(x->delayMode == DELAYMODE_INTERPOLATED)
        FrameRingBuffer_Free((FrameRingBuffer*)x->tap);
    else
        RingBuffer_Free((RingBuffer*)x->tap);
    _free(x->temp);
    _free(x);
}
