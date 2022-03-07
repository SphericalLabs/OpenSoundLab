
#include "RingBuffer.h"
#include <stdlib.h>
#include "util.h"
#include <string.h>
#include <stdio.h>
#include <assert.h>
#include <algorithm>

#if __ARM_NEON
    #include <arm_neon.h>
#endif

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

SOUNDSTAGE_API void RingBuffer_WriteSingleValue(float val, int n, struct RingBuffer *x)
{
    if(n > x->n)
    {
        n = x->n;
    }
    int n1 = _min( x->n - x->ptr, n);
    std::fill(x->buf + x->ptr, x->buf + x->ptr + n1, val);
    x->ptr = (x->ptr + n1) % x->n;
    if(n1 < n)
    {
        int n2 = n - n1;
        std::fill(x->buf, x->buf + n2, val);
        x->ptr = (x->ptr + n2) % x->n;
    }
}

SOUNDSTAGE_API int RingBuffer_WritePadded(float *src, int n, float stride, int interpolation, struct RingBuffer *x)
{
    if(stride == 1)
    {
        RingBuffer_Write(src, n, x);
        return n;
    }
    
    int total = 0; //later remove
    int tn = (int)(n * stride + 0.5f);
    if(tn > x->n)
    {
        //TODO: no need to write all samples, as the first ones will be overwritten anyway.
    }
    
    float fPtr;
    
    //If we don't have to perform any interpolation, we can just loop over the ringbuffer with a floating pointer and copy each source sample m times.
    if(interpolation == INTERPOLATION_NONE)
    {
        fPtr = x->ptr;
        int m; //number of times to write 1 sample
        for(int i = 0; i < n; i++)
        {
            fPtr += stride;
            m = (int)(fPtr + 0.5f) - x->ptr;
            while(fPtr >= x->n) // >= bc max valid index is n-1
                fPtr -= x->n;
            while(m > x->n) // > bc can write n samples max
                m -= x->n;
            RingBuffer_WriteSingleValue(*src, m, x);
            total += m;
            src++;
        }
    }

    //To perform linear interpolation, we use a floating pointer to loop over the src array and write the interpolated samples to the ringbuffer.
    else if (interpolation == INTERPOLATION_LINEAR)
    {
        fPtr = 0;
        float step = 1 / stride;
        float frac, a, b;
        int srcPtr;
        for(int i = 0; i < tn; i++)
        {
            srcPtr = (int)fPtr;
            frac = fPtr - srcPtr;
            a = src[srcPtr];
            b = src[(srcPtr + 1) % n];
            x->buf[x->ptr] = a + frac * (b - a); //bc (1-c)a + cb = a + c(b-a)
            x->ptr = (x->ptr + 1) % x->n;
            fPtr += step;
        }
        total = tn;
    }
    
    //assert(tn == total); //later remove
    return total;
}

SOUNDSTAGE_API void RingBuffer_Read_Absolute(float *dest, int n, int startIndex, struct RingBuffer *x)
{
    int m = 0; //number of elements to read in current step
    int rPtr = startIndex; //read pointer
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

SOUNDSTAGE_API void RingBuffer_ReadPadded(float *dest, int n, int offset, float stride, struct RingBuffer *x)
{
    if(stride == 1)
    {
        RingBuffer_Read(dest, n, offset, x);
        return;
    }
    
    int rPtr = x->ptr + offset;
    while(rPtr >= x->n)
        rPtr -= x->n;
    while(rPtr < 0)
        rPtr += x->n;
    
    float fPtr = (float)rPtr;
    
    for(int i = 0; i < n; i++)
    {
        dest[i] = x->buf[((int)(fPtr + 0.5f)) % x->n];
        fPtr += stride;
        if(fPtr >= x->n)
            fPtr -= x->n;
    }
}

/* The following functions are NOT thread-safe! Snychronize accordingly if using them in a multi-threaded setting. */

SOUNDSTAGE_API void RingBuffer_Resize(int n, struct RingBuffer *x)
{
    x->buf = (float*)realloc(x->buf, n * sizeof(float));
    assert(x->buf != NULL);
    x->n = n;
    x->ptr = 0;
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
