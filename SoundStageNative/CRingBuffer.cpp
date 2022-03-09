//
//  CRingBuffer.cpp
//  SoundStageNative
//
//  Created by hb on 19.01.22.
//

#include "CRingBuffer.hpp"
#include "util.h"
#include <assert.h>
#include "resample.h"
#include "RingBuffer.h"

#define _MAX(a, b) a > b ? a : b
#define _MIN(a, b) a < b ? a : b

FrameRingBuffer* FrameRingBuffer_New(int n)
{
    FrameRingBuffer *x = new FrameRingBuffer();
    x->data = (float*)_malloc(n * sizeof(float));
    _fZero(x->data, n);
    x->ptr = 0;
    x->n = n;
    x->headers.push_back(FrameHeader());
    x->headers[0].length = n;
    x->headers[0].oversampling = 1;
    x->headers[0].head = 0;
    x->headers[0].tail = 0;
    
    return x;
}

void FrameRingBuffer_Free(FrameRingBuffer *x)
{
    _free(x->data);
    _free(x);
}

void InsertFrame(int n, float oversampling, FrameRingBuffer *x)
{
    int p = -1, length = 0, head = x->ptr, tail = head, delta1, delta2;
    FrameHeader *existingFrame;
        
    /*printv("\n\n=======Start:InsertAndRestore========\n");
    printv("ptr: %d\n", x->ptr);
    printv("n: %d\n", n);
    printv("ptrFrame: %d\n", p);
    printv("======\n");
    printv("before insert:\n");
    for(int i = 0; i < x->headers.size(); i++)
    {
        FrameHeader h = x->headers[i];
        printv("%d: start=%d, length=%d, tail=%d, stride=%f\n", i, h.head, h.length, h.tail, h.oversampling);
    }*/
    
    ///1. Consume existing frames until length is reached.
    while(length < n)
    {
        p = (p + 1) % x->headers.size();
        existingFrame = &x->headers[p];
        delta1 = n - length;
        ///1a. The existing frame must be completely consumed, so we delete it.
        if(existingFrame->length <= delta1)
        {
            length += existingFrame->length;
            x->headers.erase(x->headers.begin() + p);
            p--;
        }
        ///1b. The existing frame does not need to be consumed completely, so we just decrease its size.
        else
        {
            delta2 = existingFrame->length - delta1;
            existingFrame->head = (existingFrame->head + delta1) % x->n;
            existingFrame->length = delta2;
            length = n;
        }
    }
    
    ///2. If a previous frame exists and it has the same oversampling, we consume its samples and delete it.
    ///This way, we guarantee that adjacent frames always have different oversampling, except at array boundaries
    if(x->headers.size() > 1)
    {
        existingFrame = &x->headers[x->headers.size() - 1]; //most recent frame
        if(existingFrame->oversampling == oversampling)
        {
            length += existingFrame->length;
            head = existingFrame->head;
            x->headers.pop_back();
        }
    }
    
    ///3. To guarantee no adjacent frames with same oversampling also at array boundaries, we could do the same check as in 5. also for the NEXT frame.
    ///The code below is an incomplete stub; it does not work correctly. Use as an inspiration if you want to extend.
    /*if(x->headers.size() > 1)
    {
        existingFrame = &x->headers[0]; //nextFrame
        if(existingFrame->oversampling == oversampling)
        {
            length += existingFrame->length;
            x->headers.erase(x->headers.begin());
        }
    }*/
    
    ///4. Finally, we calculate the new tail and insert the new frame header.
    tail = (head + length) % x->n;
    FrameHeader h;
    h.oversampling = oversampling;
    h.length = length;
    h.head = head;
    h.tail = tail;
    x->headers.push_back(h);
    
    //TODO: debug only
    /*printv("after insert:\n");
    for(int i = 0; i < x->headers.size(); i++)
    {
        FrameHeader h = x->headers[i];
        printv("%d: start=%d, length=%d, tail=%d, stride=%f\n", i, h.head, h.length, h.tail, h.oversampling);
    }
    bool legit = FrameRingBuffer_Validate(x);
    printv("=======End:InsertAndRestore========\n\n\n");*/
}

/* This is efficient as long as n is reasonably large, e.g. writing buffers of 256 samples. */
void FrameRingBuffer_Write(float* src, int n, float oversampling, FrameRingBuffer *x)
{
    ///1. Clamp n
    if(n > x->n)
    {
        src = src + (n - x->n);
        n = x->n;
    }
    
    ///2. Insert new frame header
    InsertFrame(n, oversampling, x);

    ///3. Copy data to buffer
    int m = _min( x->n - x->ptr, n);
    memcpy(x->data + x->ptr, src, m*sizeof(float));
    x->ptr = x->ptr + m; //no modulus, as we ruled out wrap-around
    if(m < n)
    {
        src += m;
        m = n - m;
        memcpy(x->data, src, m*sizeof(float));
        x->ptr = m; //bc x->ptr is 0 if we reach this block
    }
}

void FrameRingBuffer_GetFrame(int sampleIndex, int* frameIndex, float* frameOffset, FrameRingBuffer *x)
{
    while(sampleIndex < 0)
        sampleIndex += x->n;
    
    FrameHeader *h;
    for(int i = 0; i < x->headers.size(); i++)
    {
        h = &x->headers[i];
        if(h->head == h->tail)
        {
            *frameIndex = i;
            *frameOffset = (float) (sampleIndex - h->head);
            if(*frameOffset < 0)
                *frameOffset += x->n;
            return;
        }
        else if (h->head < h->tail && h->head <= sampleIndex && h->tail > sampleIndex)
        {
            *frameIndex = i;
            *frameOffset = (float) (sampleIndex - h->head);
            return;
        }
        else if (h->head > h->tail && (h->head <= sampleIndex || h->tail > sampleIndex))
        {
            *frameIndex = i;
            *frameOffset = (float) (sampleIndex - h->head);
            if(*frameOffset < 0)
                *frameOffset += x->n;
            return;
        }
    }
    assert(false);
}

//Normalize value to samplerate of 1. Is identical to _convert(numSamples, srcOversampling, 1.0f).
inline double _normalize(double numSamples, double oversampling)
{
    return numSamples * oversampling;
}

//Convert numSamples from srcOversampliing to targetOversampling.
inline double _convert(double numSamples, double srcOversampling, double targetOversampling)
{
    return numSamples / targetOversampling * srcOversampling;
}

//Note: The occasional use of double precision is intended here, as using single precision can cause offsets of multiple samples!
float FrameRingBuffer_Read(float* dest, int n, int offset, float oversampling, int interpolation, FrameRingBuffer *x)
{
    assert(offset < 0);
    assert(oversampling > 0);
    assert(abs(offset) <= x->n);
    assert(n <= x->n);

    int headerIndex = x->headers.size();
    double samplesInFrame;
    double samplesNeeded = _normalize((double)-offset, (double)oversampling);
    FrameHeader *header = NULL;
    
    ///1. Back-trace the frame headers to find where to start reading from
    while(samplesNeeded > 0)
    {
        headerIndex--;
        if(headerIndex < 0)
            headerIndex = x->headers.size() - 1;
        header = &(x->headers[headerIndex]);
        samplesInFrame = _normalize((double)header->length, (double)header->oversampling);
        samplesNeeded -= samplesInFrame;
    }
    
    int ptr;
    double fPtr = header->head; //the start index of the frame in which the first sample to read is located
    
    /// At this point, samplesNeeded is either 0 or negative; negative meaning we backtracked "too far", so we have to start reading somewhere in the middle of the frame.
    /// In other words: The position to start reading from is in the current frame with an offset of (-samplesNeeded).
    samplesNeeded = -samplesNeeded;
    float frac, a, b;
    int wsincPtr;
    
    /// 2. Copy n samples into the destination buffer
    for(int i = 0; i < n; i++)
    {
        /// To advance 1 sample from the reader's perspective, we have to take into account oversampling rates as follows:
        fPtr += _convert(samplesNeeded, 1.0, (double)header->oversampling);
        if(fPtr >= x->n)
            fPtr -= x->n;

        if(interpolation == INTERPOLATION_LINEAR)
        {
            ptr = (int)fPtr;
            frac = fPtr - ptr;
            a = x->data[ptr];
            b = x->data[(ptr + 1) % x->n];
            
            dest[i] = a + frac * (b - a); //bc (1-c)a + cb = a + c(b-a)
        }
        else if(interpolation == INTERPOLATION_NONE)
        {
            ptr = (int)(fPtr + 0.5f) % x->n;
            /// Sometimes it still happens that ptr == tail, which is not correct, ptr should always be < tail... reason is probably floating point imprecision / rounding errors. Quickfix for now:
            if(ptr == header->tail)
                ptr = _MAX(0, ptr - 1);
            dest[i] = x->data[ptr];
        }
        else if(interpolation == INTERPOLATION_WSINC)
        {
            ptr = (int)(fPtr + 0.5f);
            frac = fPtr - ptr;
            ptr = ptr % x->n;
            wsincPtr = ptr - ZEROCROSSINGS_PER_AXIS;
            if(wsincPtr < 0)
                wsincPtr += x->n;
            for(int j = 0; j < CONV_LENGTH; j++)
            {
                wsinc_convBuffer[j] = x->data[wsincPtr];
                wsincPtr = (wsincPtr + 1) % x->n;
            }
            dest[i] = wsinc_resample(wsinc_convBuffer, -frac);
        }
        
        ///we advanced samplesNeeded samples into the frame, so they are not available anymore.
        samplesInFrame -= samplesNeeded;
        
        ///We copied a sample, so we need 1 whole new sample now.
        samplesNeeded = oversampling;
        
        ///Skip frame(s) if they don't contain the next sample to read:
        while(samplesInFrame < samplesNeeded)
        {
            //assert(i != 0); //in first iteration we should always be in the correct frame
            samplesNeeded -= samplesInFrame;
            headerIndex = (headerIndex + 1) % x->headers.size();
            header = &x->headers[headerIndex];
            samplesInFrame = _normalize((double)header->length, (double)header->oversampling);
            fPtr = header->head;
        }
    }
    
    return fPtr;
}

float FrameRingBuffer_Read2(float* dest, int n, int offsetTotal, float* offsets, int singleValueDelay, int interpolation, FrameRingBuffer *x)
{
    int headerIndex = x->headers.size();
    double samplesInFrame;
    double samplesNeeded = (double)-offsetTotal;
    FrameHeader *header = NULL;
    double oversampling = (double)x->n / *offsets; //only used if singleValueDelay == true
    
    ///1. Back-trace the frame headers to find where to start reading from
    while(samplesNeeded > 0)
    {
        headerIndex--;
        if(headerIndex < 0)
            headerIndex = x->headers.size() - 1;
        header = &(x->headers[headerIndex]);
        samplesInFrame = _normalize((double)header->length, (double)header->oversampling);
        samplesNeeded -= samplesInFrame;
    }
    
    int ptr;
    double fPtr = header->head; //the start index of the frame in which the first sample to read is located
    
    /// At this point, samplesNeeded is either 0 or negative; negative meaning we backtracked "too far", so we have to start reading somewhere in the middle of the frame.
    /// In other words: The position to start reading from is in the current frame with an offset of (-samplesNeeded).
    samplesNeeded = -samplesNeeded;
    float frac, a, b;
    int wsincPtr;
    
    /// 2. Copy n samples into the destination buffer
    for(int i = 0; i < n; i++)
    {
        /// To advance 1 sample from the reader's perspective, we have to take into account oversampling rates as follows:
        fPtr += _convert(samplesNeeded, 1.0, (double)header->oversampling);
        if(fPtr >= x->n)
            fPtr -= x->n;

        if(interpolation == INTERPOLATION_LINEAR)
        {
            ptr = (int)fPtr;
            frac = fPtr - ptr;
            a = x->data[ptr];
            b = x->data[(ptr + 1) % x->n];
            
            dest[i] = a + frac * (b - a); //bc (1-c)a + cb = a + c(b-a)
        }
        else if(interpolation == INTERPOLATION_NONE)
        {
            ptr = (int)(fPtr + 0.5f) % x->n;
            /// Sometimes it still happens that ptr == tail, which is not correct, ptr should always be < tail... reason is probably floating point imprecision / rounding errors. Quickfix for now:
            if(ptr == header->tail)
                ptr = _MAX(0, ptr - 1);
            dest[i] = x->data[ptr];
        }
        else if(interpolation == INTERPOLATION_WSINC)
        {
            ptr = (int)(fPtr + 0.5f);
            frac = fPtr - ptr;
            ptr = ptr % x->n;
            wsincPtr = ptr - ZEROCROSSINGS_PER_AXIS;
            if(wsincPtr < 0)
                wsincPtr += x->n;
            for(int j = 0; j < CONV_LENGTH; j++)
            {
                wsinc_convBuffer[j] = x->data[wsincPtr];
                wsincPtr = (wsincPtr + 1) % x->n;
            }
            dest[i] = wsinc_resample(wsinc_convBuffer, -frac);
        }
        
        ///we advanced samplesNeeded samples into the frame, so they are not available anymore.
        samplesInFrame -= samplesNeeded;
        
        ///We copied a sample, so we need 1 whole new sample now.
        samplesNeeded = singleValueDelay ? oversampling : (double)x->n / offsets[i];
        
        ///Skip frame(s) if they don't contain the next sample to read:
        while(samplesInFrame < samplesNeeded)
        {
            //assert(i != 0); //in first iteration we should always be in the correct frame
            samplesNeeded -= samplesInFrame;
            headerIndex = (headerIndex + 1) % x->headers.size();
            header = &x->headers[headerIndex];
            samplesInFrame = _normalize((double)header->length, (double)header->oversampling);
            fPtr = header->head;
        }
    }
    
    return fPtr;
}

void FrameRingBuffer_Clear(FrameRingBuffer *x)
{
    _fZero(x->data, x->n);
    x->headers.clear();
    FrameHeader h;
    h.length = x->n;
    h.head = 0;
    h.tail = 0;
    h.oversampling = 1;
    x->headers.emplace_back(h);
    x->ptr = 0;
}

bool FrameRingBuffer_Validate(FrameRingBuffer *x)
{
    bool legit = true;
    
    int numSmpls = 0;
    for(int i = 0; i < x->headers.size(); i++)
        numSmpls += x->headers[i].length;
    if(numSmpls != x->n)
    {
        printv("VALIDATION FAILED: the buffer contains %d samples, but the headers sum up to %d samples: \n", x->n, numSmpls);
        legit = false;
    }
    
    int adjacentFramesWithSameStride = 0;
    if(x->headers.size() > 1)
    {
        for(int i = 0; i < x->headers.size(); i++)
        {
            float stride1 = x->headers[i].oversampling;
            float stride2 = x->headers[(i + 1) % x->headers.size()].oversampling;
            if (stride1 == stride2)
                adjacentFramesWithSameStride++;
        }
    }

    //assert(adjacentFramesWithSameStride == 0);
    if(adjacentFramesWithSameStride != 0)
    {
        printv("VALIDATION FAILED: the buffer contains %d adjacent frames with same stride: \n", adjacentFramesWithSameStride);
        legit = false;
    }
    
    int corruptCounds = 0;
    for(int i = 0; i < x->headers.size(); i++)
    {
        int tail = (x->headers[i].head + x->headers[i].length) % x->n;
        int nextHead = x->headers[(i+1) % x->headers.size()].head;
        if(tail != nextHead || tail != x->headers[i].tail)
        {
            corruptCounds++;
        }
    }
    if(corruptCounds != 0)
    {
        printv("VALIDATION FAILED: the buffer contains %d corrupt bounds. \n", corruptCounds);
        legit = false;
    }
        
    
    if(!legit)
        FrameRingBuffer_Print(x);
        
    return legit;
}

void FrameRingBuffer_Print(FrameRingBuffer *x)
{
    printv("ptr: %d / %d\n", x->ptr, x->n);
    printv("======\n");
    printv("HEADERS:\n");
    FrameHeader h;
    for(int i = 0; i < x->headers.size(); i++)
    {
        h = x->headers[i];
        printv("%d: start=%d, length=%d, tail=%d, stride=%f\n", i, h.head, h.length, h.tail, h.oversampling);
    }
    printv("======\n");
    /*printv("BUFFER:\n");
    for(int i = 0; i < x->n; i++)
    {
        printv("%d: %f\n", i, x->data[i]);
    }*/
}
