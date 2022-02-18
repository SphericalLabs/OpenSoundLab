//
//  CRingBuffer.cpp
//  SoundStageNative
//
//  Created by hb on 19.01.22.
//

#include "CRingBuffer.hpp"
#include "util.h"
#include <assert.h>

#define _MAX(a, b) a > b ? a : b
#define _MIN(a, b) a < b ? a : b

FrameRingBuffer* FrameRingBuffer_New(int n)
{
    FrameRingBuffer *x = new FrameRingBuffer();
    x->data = (float*)malloc(n * sizeof(float));
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

void InsertAndRestore(int n, float oversampling, FrameRingBuffer *x)
{
    int p = -1, length = 0, head = x->ptr, tail = -1, delta1, delta2;
    
    ///1. Get the frame where ringbuffer ptr is located => this is where the new frame will start
    FrameHeader *existingFrame = &x->headers[p];
    
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
    
    ///2. Consume existing frames until length is reached.
    while(length < n)
    {
        p = (p + 1) % x->headers.size();
        existingFrame = &x->headers[p];
        delta1 = n - length;
        ///2a. The existing frame mus be completely consumed, so delete it.
        if(existingFrame->length <= delta1)
        {
            length += existingFrame->length;
            x->headers.erase(x->headers.begin() + p);
            p--;
        }
        ///2b. The existing frame does not need to be consumed completely, so just decrease its size.
        else
        {
            delta2 = existingFrame->length - delta1;
            existingFrame->head = (existingFrame->head + delta1) % x->n;
            existingFrame->length = delta2;
            assert(existingFrame->length >= 0);
            length = n;
        }
    }
    
    ///3. If a previous frame exists and it has the same oversampling, we consume its samples and delete it.
    ///This way, we guarantee that adjacent frames always have different oversampling, except at array boundaries
    if(x->headers.size() > 1)
    {
        existingFrame = &x->headers[x->headers.size() - 1]; //previous frame
        if(existingFrame->oversampling == oversampling)
        {
            length += existingFrame->length;
            head = existingFrame->head;
            x->headers.pop_back();
        }
    }
    
    ///4. To guarantee no adjacent frames with same oversampling also at array boundaries, we could do the same check as in 5. also for the NEXT frame.
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
    
    ///5. Finally, we calculate the new tail and insert the new frame header.
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
    ///Clamp n
    if(n > x->n)
    {
        src = src + (n - x->n);
        n = x->n;
    }
    
    ///Insert new frame header
    InsertAndRestore(n, oversampling, x);

    /// Copy data to buffer
    int n1 = _min( x->n - x->ptr, n);
    memcpy(x->data + x->ptr, src, n1*sizeof(float));
    x->ptr = (x->ptr + n1) % x->n;
    if(n1 < n)
    {
        int n2 = n - n1;
        memcpy(x->data, src + n1, n2*sizeof(float));
        x->ptr = (x->ptr + n2) % x->n;
    }
}

void FrameRingBuffer_Read(float* dest, int n, int offset, float oversampling, FrameRingBuffer *x)
{
    assert(offset < 0);
    assert(oversampling > 0);
    assert(abs(offset) <= x->n);
    assert(n <= x->n);

    int headerIndex = x->headers.size();
    float samplesInFrame;
    float samplesNeeded = -offset;
    FrameHeader *header;
    
    ///1. Back-trace the frame headers to find where to start reading from
    while(samplesNeeded > 0)
    {
        headerIndex--;
        if(headerIndex < 0)
            headerIndex = x->headers.size() - 1;
        
        header = &(x->headers[headerIndex]);
        
        samplesInFrame = header->length * header->oversampling / oversampling; //number of samples as viewed from the reader's perspective.
        
        samplesNeeded -= samplesInFrame;
    }
    
    /* The position to start reading from is in the current frame with an offset of (-samplesNeeded). */

    int ptr;
    float fPtr = header->head; //the start index of the frame in which the first sample to read is located
    
    samplesInFrame = header->length * header->oversampling / oversampling;
    // Calculate the offset from the start of the frame to the first sample to read
    // At this point, samplesNeeded is either 0 or negative; negative meaning we backtracked "too far", so we have to start reading so much into the frame
    samplesNeeded = -samplesNeeded;
    
    for(int i = 0; i < n; i++)
    {
        //Skip frame(s) if the don't contain the next sample to read
        while(samplesInFrame < samplesNeeded)
        {
            //assert(i != 0); //in first iteration we should always be in the correct frame
            samplesNeeded -= samplesInFrame;
            headerIndex = (headerIndex + 1) % x->headers.size();
            header = &x->headers[headerIndex];
            samplesInFrame = header->length * header->oversampling / oversampling;
            fPtr = header->head;
        }
                
        //We have found the frame in which the next sample to copy is located, so we update our pointers and copy 1 sample to dest.
        fPtr += samplesNeeded * oversampling / header->oversampling;
        if(fPtr >= x->n)
            fPtr -= x->n;
        ptr = (int)(fPtr + 0.5f) % x->n;
        //printv("copying: %f\n", x->data[ptr]);
        dest[i] = x->data[ptr];
        
        //We copied a sample, so we need 1 whole new sample now.
        samplesNeeded = 1;
        
        //we advanced samplesNeeded samples into the frame, so they are not available anymore.
        samplesInFrame -= samplesNeeded;
    }
}

void FrameRingBuffer_Clear(FrameRingBuffer *x)
{
    _fZero(x->data, x->n);
    x->headers.clear();
    FrameHeader h;
    h.length = x->n;
    h.head = 0;
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
