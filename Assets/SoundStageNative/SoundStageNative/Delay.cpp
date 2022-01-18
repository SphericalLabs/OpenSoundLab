
#include "Delay.h"
#include "util.h"
#include <string.h>
#include <math.h>
#include <assert.h>

#define DELAY_MAXVECTORSIZE 4096

#define DELAY_MAXTIME 96000 //in samples
#define DELAY_MINTIME 512 //in bucket brigade mode, shorter delay means more oversampling, so we have to set a reasonable limit.

enum DelayParams
{
    P_TIME,
    P_FEEDBACK,
    P_WET,
    P_DRY,
    P_N
};

void Delay_ProcessPitchShift(float buffer[], int n, int channels, DelayData* x)
{
    assert(x->time > 0);
    float stride = (float)x->maxTime / (float)x->time;
    
    if(x->time < n)
    {
        /* if the delay time is shorter than the buffer, then we should avoid writing samples we will not use anyway.
         This is especially important since short delay times resut in more oversampling and thus more CPU load...
         */
    }
    
    //float offset = -(x->time * stride); //this is unnecessary, as with fixed buffer length, offset is always == -(x->maxTime);
    
    int nPerChannel = n/channels;
    if(channels > 1)
        _fDeinterleave(buffer, buffer, n, channels);
    
    RingBuffer_ReadPadded(x->temp, nPerChannel, -(x->maxTime), stride, x->tap);
    _fCopy(x->temp, x->temp2, nPerChannel);
    
    _fScale(x->temp, x->temp, x->feedback, nPerChannel);
    _fAdd(buffer, x->temp, x->temp, nPerChannel);
    RingBuffer_WritePadded(x->temp, nPerChannel, stride, x->tap);

    _fScale(buffer, buffer, x->dry, nPerChannel);
    _fScale(x->temp2, x->temp2, x->wet, nPerChannel);
    //RingBuffer_ReadPadded(x->temp, nPerChannel, -(x->maxTime + nPerChannel * stride), stride, x->tap);
    
    printv("stride: %f\n", stride);
    for(int i = 0; i < nPerChannel; i++)
        printv("temp[%d] == %f\n", i, x->temp[i]);
    printv("====\n");
    
    //_fScale(x->temp, x->temp, x->wet, nPerChannel);
    
    _fAdd(buffer, x->temp2, buffer, nPerChannel);
    
    if(channels > 1)
    {
        for(int i = 1; i < channels; i++)
        {
            _fCopy(buffer, buffer+i*nPerChannel, nPerChannel);
        }
        _fInterleave(buffer, buffer, n, channels);
    }
}

void Delay_ProcessSimple(float buffer[], int n, int channels, DelayData* x) {
    assert(n <= DELAY_MAXVECTORSIZE);
    //assert(n <= x->time);

    //Read samples from ringbuffer
    RingBuffer_Read(x->temp, n, -(x->time), x->tap);
    //Scale samples for feeding back
    _fScale(x->temp, x->temp, x->feedback, n);
    //Add input samples
    _fAdd(buffer, x->temp, x->temp, n);
    //Write everything to ringbuffer
    RingBuffer_Write(x->temp, n, x->tap);

    //Scale the direct sound
    _fScale(buffer, buffer, x->dry, n);
    //Read the same samples once again from ringbuffer (note that the buffer pointer has changed, so we need -(n + x->time) now)
    //This is not ideal, can't we do the same thing without copying twice?!
    RingBuffer_Read(x->temp, n, -(n + x->time), x->tap);
    //Scale delayed samples for output
    _fScale(x->temp, x->temp, x->wet, n);

    //Add scaled delay samples to output
    _fAdd(buffer, x->temp, buffer, n);
}


SOUNDSTAGE_API void Delay_Process(float buffer[], int n, int channels, DelayData* x)
{
    Delay_ProcessPitchShift(buffer, n, channels, x);
    //Delay_ProcessSimple(buffer, n, channels, x);
}

SOUNDSTAGE_API void Delay_SetParam(float value, int param, struct DelayData *x)
{
    assert(param < P_N);
    
    int intval = (int)value;
        
    switch (param) {
        case P_TIME:
            assert(value <= x->tap->n);
            if(intval % 2 != 0)
            {
                //This is a quick hack to ensure the ringbuffer will not randomly switch left and right channels when used with interleaved audio data.
                value--;
            }
            x->time = intval;
            //printv("Set delay time to %d\n", intval);
            break;
        case P_FEEDBACK:
            assert(value >= 0 && value <= 1);
            x->feedback = value;
            //printv("Set delay feedback to %f\n", value);
            break;
        case P_WET:
            assert(value >= 0 && value <= 1);
            x->wet = value;
            //printv("Set delay wet mix to %f\n", value);
            break;
        case P_DRY:
            assert(value >= 0 && value <= 1);
            x->dry = value;
            //printv("Set delay dry mix to %f\n", value);
            break;
        default:
            break;
    }
}

SOUNDSTAGE_API struct DelayData *Delay_New(int n)
{
    DelayData *x = (DelayData*)_malloc(sizeof(struct DelayData));
    x->tap = RingBuffer_New(n);
    x->temp = (float*)_malloc(DELAY_MAXVECTORSIZE * sizeof(float));
    x->temp2 = (float*)_malloc(DELAY_MAXVECTORSIZE * sizeof(float));
    _fZero(x->temp, DELAY_MAXVECTORSIZE);
    _fZero(x->temp2, DELAY_MAXVECTORSIZE);
    x->wet = 1;
    x->dry = 0.7f;
    x->time = n;
    x->maxTime = n;
    x->feedback = 0.3f;
    return x;
}

SOUNDSTAGE_API void Delay_Free(struct DelayData *x)
{
    RingBuffer_Free(x->tap);
    _free(x->temp);
    _free(x);
}
