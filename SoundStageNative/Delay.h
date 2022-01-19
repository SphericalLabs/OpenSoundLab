
#pragma once

#include "main.h"
#include "RingBuffer.h"
#include "CompressedRingBuffer.h"

struct DelayData
{
    //public
    int time; //in samples, therefore int.
    float feedback;
    float wet;
    float dry;
    
    //internal
    int maxTime;
    struct RingBuffer *tap;
    float *temp;
    float *temp2;
};

#ifdef __cplusplus
extern "C" {
#endif
///Allocates and returns a new delay instance with maximum delay time n (samples).
SOUNDSTAGE_API struct DelayData *Delay_New(int n);
///Releases allocated resources.
SOUNDSTAGE_API void Delay_Free(struct DelayData *x);
///Sets the parameter to the specified value.
SOUNDSTAGE_API void Delay_SetParam(float value, int param, struct DelayData *x);
///Processes 1 block of interleaved audio data.
SOUNDSTAGE_API void Delay_Process(float buffer[], int n, int channels, float cTime, float cFeedback, DelayData* x);
//Clears the delay buffer.
SOUNDSTAGE_API void Delay_Clear(DelayData* x);
#ifdef __cplusplus
}
#endif
