///Created by Hannes January 2022
///
///This is a delay audio effect that emulates a bucket-brigade delay. It operates on a fixed-size buffer.
///The delay time is determined by the oversampling factor used to write/read to the buffer.
///This results in the characteristic "pitch-shift" effect when changing the delay time while there is still signal in the delay buffer.
///
///All functions are not thread-safe, hence the caller must avoid simultaneous access from multiple threads.

#ifndef Delay_h
#define Delay_h

#include "main.h"
#include "RingBuffer.h"
#include "CompressedRingBuffer.h"
#include "CRingBuffer.hpp"

#define DELAYMODE_SIMPLE 0
#define DELAYMODE_PADDED 1
#define DELAYMODE_INTERPOLATED 2

struct DelayData
{
    //public
    int time; //in samples, therefore int.
    float feedback;
    float wet;
    float dry;
    int interpolation;
    int delayMode;
    
    //internal
    int maxTime;
    int prevTime; //for time interpolation
    float prevFeedback;
    float prevDry;
    float prevWet;
    void *tap; //either RingBuffer or FrameRingBuffer, depending on the delay's current mode
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
//Sets the possible range of the delay. This is necessary for now bc excessive oversampling causes high CPU loads otherwise.
SOUNDSTAGE_API void Delay_SetRange(int min, int max, DelayData* x);
SOUNDSTAGE_API void Delay_SetMode(int mode, DelayData* x);
///Processes 1 block of interleaved audio data.
SOUNDSTAGE_API void Delay_Process(float buffer[], int n, int channels, DelayData* x);
//Clears the delay buffer.
SOUNDSTAGE_API void Delay_Clear(DelayData* x);
#ifdef __cplusplus
}
#endif

#endif /* Delay_h */
