/// A basic ring buffer that can be used for delay lanes etc.
///
/// All functions are not thread-safe, hence the caller must avoid simultaneous access from multiple threads.
///
/// However, it is easy to use the RingBuffer in a multi-threaded environment with one reader and one writer thread:
/// You just have to ensure that the reader thread and the writer thread operate on non-overlapping parts of the RingBuffer.

#ifndef RingBuffer_h
#define RingBuffer_h

#include "main.h"

struct RingBuffer
{
    int ptr;
    int n;
    float *buf;
};

#ifdef __cplusplus
extern "C" {
#endif

/* Writing into the buffer */

///Writes n samples to the ring buffer. The RingBuffer takes care of advancing pointers, wrap-around etc.
SOUNDSTAGE_API void RingBuffer_Write(float *src, int n, struct RingBuffer *x);
///Writes samples to the ringbuffer with a specified stride. If the stride is 1, all samples are written to the ringbuffer. If stride < 1, some samples are skipped. If stride > 1, some samples are written more than once (=padded). If desired, the samples are interpolated before writing. This can be useful to simulate oversampling.
SOUNDSTAGE_API int RingBuffer_WritePadded(float *src, int n, float stride, int interpolation, struct RingBuffer *x);
///Writes 1 value into the RingBuffer n times. This is a lot faster than calling RingBuffer_Write() n times.
SOUNDSTAGE_API void RingBuffer_WriteSingleValue(float val, int n, struct RingBuffer *x);

/* Reading from the buffer */

///Reads n samples from the ring buffer with a specified offset. The offset is added to the current writePtr.
///Example: To read the most recent 64 samples from RingBuffer *rb into float* a, call:
///RingBuffer_Read(a, 64, -64, rb);
///Example 2: To read the oldest 100 samples from the RingBuffer *rb into float* b, call:
///RingBuffer_Read(b, 100, 0, rb);
///Real-life example for delay audio effect: To read 512 samples from the RingBuffer* rb into an audioBuffer with a "delay" of 48000 samples (1 second @ 48kHz), call:
///RingBuffer_Read(audioBuffer, -48000, rb);
SOUNDSTAGE_API void RingBuffer_Read(float *dest, int n, int offset, struct RingBuffer *x);
///Read n samples from the ringbuffer starting at an absolutely specified index.
SOUNDSTAGE_API void RingBuffer_Read_Absolute(float *dest, int n, int startIndex, struct RingBuffer *x);
///Reads n samples with a specific stride
SOUNDSTAGE_API void RingBuffer_ReadPadded(float *dest, int n, int offset, float stride, struct RingBuffer *x);

/* Allocating, Freeing, Resizing */

///Resizes the buffer. This includes a memory re-allocation, so use with caution!
SOUNDSTAGE_API void RingBuffer_Resize(int n, struct RingBuffer *x);
///Allocates a new RIngBuffer with maximum capacity n and returns a reference to it.
SOUNDSTAGE_API struct RingBuffer *RingBuffer_New(int n);
///Frees all resources.
SOUNDSTAGE_API void RingBuffer_Free(struct RingBuffer *x);
#ifdef __cplusplus
}
#endif

#endif /* RingBuffer_h */
