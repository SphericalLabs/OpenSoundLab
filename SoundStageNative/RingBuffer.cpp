
#include "RingBuffer.h"
#include <stdlib.h>
#include "util.h"
#include <string.h>
#include <stdio.h>
#include <assert.h>

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

SOUNDSTAGE_API int RingBuffer_WritePadded(float *src, int n, float stride, struct RingBuffer *x)
{
    int total = 0; //later remove
    int tn = (int)(n * stride);
    if(tn > x->n)
    {
        //TODO: no need to write all samples, as the first ones will be overwritten anyway.
    }
    
    float fPtr = x->ptr;
    int m; //number of times to write 1 sample
    for(int i = 0; i < n; i++)
    {
        //Advance floating pointer
        fPtr += stride;
        //calcualte m (number of times to write current sample) BEFORE wrapping fPtr
        m = (int)(fPtr + 0.5f) - x->ptr;
        //Wrap fPtr
        while(fPtr >= x->n) // >= bc max valid index is n-1
            fPtr -= x->n;
        while(m > x->n) // > bc can write n samples max
            m -= x->n;
        
        //printv("fPtr == %f, m == %d, x->ptr == %d\n", fPtr, m, x->ptr);
        
        //Write sample 0, 1 or multiple times. After this loop, x->ptr corresponds to (int)(fPtr + 0.5f).
        for(int j = 0; j < m; j++)
        {
            //printv("x->ptr == %d\n", x->ptr);
            x->buf[x->ptr] = *src;
            x->ptr = (x->ptr + 1) % x->n;
            total++;
        }
        //assert(x->ptr == (int)(fPtr = 0.5f) );
        
        //Increment source pointer
        src++;
    }
    
    //assert(tn == total); //later remove
    return total;
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
