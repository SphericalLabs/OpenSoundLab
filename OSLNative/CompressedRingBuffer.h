//
//  CompressedRingBuffer.hpp
//  OSLNative
//
//  Created by hb on 19.01.22.
//

#ifndef CompressedRingBuffer_hpp
#define CompressedRingBuffer_hpp

#include "main.h"

typedef struct _CompressedRingBuffer
{
    float* data;
    float* weights;
    int ptr;
    int n;
} CompressedRingBuffer;

void CompressedRingBuffer_Read(float *dest, int n, int offset, float stride, CompressedRingBuffer *x);
void CompressedRingBuffer_Write(float *src, int n, float stride, CompressedRingBuffer *x);
CompressedRingBuffer *CompressedRingBuffer_New(int n);
void CompressedRingBuffer_Free(CompressedRingBuffer *x);
void CompressedRingBuffer_Print(CompressedRingBuffer *x);

#endif /* CompressedRingBuffer_hpp */
