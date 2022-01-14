
#include "RingBuffer.h"
#include <stdlib.h>
#include "util.h"
#include <string.h>
#include <stdio.h>

SOUNDSTAGE_API void RingBuffer_Write(float *src, int n, struct RingBuffer *x)
{
    if(n > x->n)
    {
        src = src + (n - x->n);
        n = x->n;
    }
    int n1 = _min( x->n - x->ptr, n);
    memcpy(x->buf + x->ptr, src, n1*sizeof(float));
    x->ptr = (x->ptr + n1) % x->n;
    if(n1 < n)
    {
        int n2 = n - n1;
        memcpy(x->buf, src + n1, n2*sizeof(float));
        x->ptr = (x->ptr + n2) % x->n;
    }
}

SOUNDSTAGE_API void RingBuffer_Read(float *dest, int n, int offset, struct RingBuffer *x)
{
    int m = 0; //number of elements to read in current step
    int rPtr = x->ptr + offset; //read pointer
     while(rPtr >= x->n)
         rPtr -= x->n;
     while(rPtr < 0)
         rPtr += x->n;
    while(n)
    {
        m = _min(x->n - rPtr, n);
        memcpy(dest, x->buf + rPtr, m*sizeof(float));
        n -= m;
        dest += m;
        rPtr = (rPtr + m) % x->n;
    }
}

SOUNDSTAGE_API void RingBuffer_ReadAndAdd(float *dest, int n, int offset, struct RingBuffer *x)
{
    int m = 0; //number of elements to read in current step
    int rPtr = x->ptr + offset; //read pointer
    while(rPtr >= x->n)
        rPtr -= x->n;
    while(rPtr < 0)
        rPtr += x->n;
    while(n)
    {
        m = _min(x->n - rPtr, n);
        for(int i = 0; i < m; i++)
        {
            *(dest + i) += x->buf[x->ptr + i];
        }
        n -= m;
        dest += m;
        rPtr = (rPtr + m) % x->n;
    }
}

SOUNDSTAGE_API struct RingBuffer *RingBuffer_New(int n)
{
    struct RingBuffer *x = (struct RingBuffer*)_malloc(sizeof(struct RingBuffer));
    x->buf = (float*)_malloc(n * sizeof(float));
    x->ptr = 0;
    x->n = n;
    _fZero(x->buf, n);
    return x;
}

SOUNDSTAGE_API void RingBuffer_Free(struct RingBuffer *x)
{
    _free(x->buf);
    _free(x);
}
