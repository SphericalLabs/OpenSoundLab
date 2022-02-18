//
//  CRingBuffer.hpp
//  SoundStageNative
//
//  Created by hb on 19.01.22.
//

#ifndef CRingBuffer_hpp
#define CRingBuffer_hpp

#include <vector>

#define MAX_NUM_HEADERS 2048

using std::vector;

typedef struct _FrameHeader
{
    int head; //inclusive
    int tail; //exclusive
    int length;
    float oversampling;
} FrameHeader;

typedef struct _FrameRingBuffer
{
    float* data;
    int ptr;
    int n;
    vector<FrameHeader> headers;
} FrameRingBuffer;

FrameRingBuffer* FrameRingBuffer_New(int n);
void FrameRingBuffer_Free(FrameRingBuffer *x);
void FrameRingBuffer_Write(float* src, int n, float oversampling, FrameRingBuffer *x);
void FrameRingBuffer_Read(float* dest, int n, int offset, float oversampling, FrameRingBuffer *x);
void FrameRingBuffer_Clear(FrameRingBuffer *x);
int FrameRingBuffer_Warn(float stride, FrameRingBuffer *x);
bool FrameRingBuffer_Validate(FrameRingBuffer *x);
void FrameRingBuffer_Print(FrameRingBuffer *x);

#endif /* CRingBuffer_hpp */
