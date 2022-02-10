/* A basic ring buffer that can be used for delay lanes etc. */

#ifndef RingBuffer_h
#define RingBuffer_h

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
///Writes samples to the ringbuffer with a specified stride. If the stride is 1, all samples are written to the ringbuffer. If stride < 1, some samples are skipped. If stride > 1, some samples are written more than once (=padded)f. No interpolation is performed. Returns the difference between old and new writeptr.
SOUNDSTAGE_API int RingBuffer_WritePadded(float *src, int n, float stride, struct RingBuffer *x);
SOUNDSTAGE_API void RingBuffer_Read_Absolute(float *dest, int n, int startIndex, struct RingBuffer *x);
///Reads n samples from the ring buffer
SOUNDSTAGE_API void RingBuffer_Read(float *dest, int n, int offset, struct RingBuffer *x);
//Reads n samples with a specific stride
SOUNDSTAGE_API void RingBuffer_ReadPadded(float *dest, int n, int offset, float stride, struct RingBuffer *x);
///Reads n samples from the ring buffer and adds the values to the dest array.
SOUNDSTAGE_API void RingBuffer_ReadAndAdd(float *dest, int n, int offset, struct RingBuffer *x);
///Resizes the buffer. This includes a memory re-allocation, so use with caution!
SOUNDSTAGE_API void RingBuffer_Resize(int n, struct RingBuffer *x);
SOUNDSTAGE_API struct RingBuffer *RingBuffer_New(int n);
///Frees all resources.
SOUNDSTAGE_API void RingBuffer_Free(struct RingBuffer *x);
#ifdef __cplusplus
}
#endif

#endif /* RingBuffer_h */
