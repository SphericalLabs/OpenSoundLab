/* A basic ring buffer that can be used for delay lanes etc. */

#pragma once

#include "main.h"

struct RingBuffer
{
    //public
    
    //internal
    int ptr;
    int n;
    float *buf;
};

#ifdef __cplusplus
extern "C" {
#endif
///Writes n samples to the ring buffer.
SOUNDSTAGE_API void RingBuffer_Write(float *src, int n, struct RingBuffer *x);
///Reads n samples from the ring buffer
SOUNDSTAGE_API void RingBuffer_Read(float *dest, int n, int offset, struct RingBuffer *x);
///Reads n samples from the ring buffer and adds the values to the dest array.
SOUNDSTAGE_API void RingBuffer_ReadAndAdd(float *dest, int n, int offset, struct RingBuffer *x);
///Reads n samples from the ring buffer and copies the values to the dest array.
SOUNDSTAGE_API struct RingBuffer *RingBuffer_New(int n);
///Frees all resources.
SOUNDSTAGE_API void RingBuffer_Free(struct RingBuffer *x);
#ifdef __cplusplus
}
#endif
