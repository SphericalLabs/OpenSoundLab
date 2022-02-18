//
//  CRingBuffer.cpp
//  SoundStageNative
//
//  Created by hb on 19.01.22.
//

#include "CRingBuffer.hpp"
#include "util.h"
#include <assert.h>

#define FB_UNINITIALIZED -1
#define _MAX(a, b) a > b ? a : b
#define _MIN(a, b) a < b ? a : b

void insertFrame(int length, float oversampling, FrameRingBuffer *x)
{
    assert(length > 0);
    
#if DEBUG
    //printv("Adding %d samples from ptr %d..\n", length, x->ptr);
    vector<FrameHeader> oldFrames(x->headers);
    /*if(FrameRingBuffer_Warn(stride, x) > 1)
    {
        printv("warning: there is already a frame with stride %f\n", stride);
    }*/
#endif
    
    if(length > x->n)
        length = x->n;
    int ptr = x->ptr;
    const int N = x->n;
    FrameHeader *existingFrame;
    FrameHeader newFrame;
    newFrame.length = 0;
    newFrame.head = ptr;
    newFrame.oversampling = oversampling;
    
    bool push = true;
    
    //Check if the array contains only 1 frame which already has the correct stride. if so, there is nothing to do.
    existingFrame = &x->headers[0];
    if(x->headers.size() == 1)
    {
        if(existingFrame->oversampling == newFrame.oversampling)
        {
            return; //Nothing to change
        }
    }
    //If the oldest frame has the same stride as the new samples, we concatenate them.
    else if(existingFrame->oversampling == newFrame.oversampling)
    {
        newFrame.length = existingFrame->length;
        x->headers.erase(x->headers.begin());
    }
    //If the most recent frame has the same stride as the new samples, we concatenate them.
    else
    {
        existingFrame = &x->headers.back();
        if(existingFrame->oversampling == newFrame.oversampling)
        {
            //Calculate how many new samples we can store in the existing frame:
            int samplesAvailable = (existingFrame->head + existingFrame->length) - newFrame.head;
            if(newFrame.head < existingFrame->head)
                samplesAvailable -= N;
            newFrame.length = existingFrame->length;
            newFrame.head = existingFrame->head;
            //we have to add the samples that we inherit from the existing frame to our total:
            length += (newFrame.length - samplesAvailable);
            if(length > N)
                length = N;
            x->headers.pop_back();
        }
    }
    
    //At this point we know we will have to modify or delete 1+ existing headers. TODO: But if ptr != header of oldest frame (which can happen through concatenation of 2 frames), then inserting the new frame will modify the head of the oldest frame, and in some cases even split it into 2 frames!
    // before:
    // |--------------------|
    //         ptr
    // after:
    // |-------|-------|----|
    //                  ptr

    //Now we delete headers, consuming their length, until the length of the new frame is identical to the number of samples we want to store.
    while(newFrame.length < length)
    {
        existingFrame = &x->headers[0];
        
        int remaining = length - newFrame.length; //this is only correct if existingFrame->head == ptr, which is not necessarily the case
        //need to consume whole frame, so delete it
        if(existingFrame->length <= remaining)
        {
            newFrame.length += existingFrame->length;
            x->headers.erase(x->headers.begin());
        }
        //no need to consume whole frame, so resize it
        else
        {
            existingFrame->length -= remaining;
            //TODO: if the ptr is between the frame boundaries, the next line is wrong!
            existingFrame->head = (existingFrame->head + remaining) % N;
            newFrame.length = length;
        }
    }
    
    //We check once again if the adjacent frame has the same stride, which could be possible if we deleted some frames and thus "closed a gap" between frames of the same stride. If so, concatenate. We have to leave the frame in its place at the beginning of the vector bc it contains the ptr.
    if(x->headers.size() > 0)
    {
        existingFrame = &x->headers[0];
        if(existingFrame->oversampling == newFrame.oversampling)
        {
            existingFrame->length += newFrame.length;
            existingFrame->head = newFrame.head;
            push = false;
        }
    }
    
    if(push)
        x->headers.push_back(newFrame);
    
#if DEBUG
    bool legit = FrameRingBuffer_Validate(x);
    if(!legit)
    {
        printv("Validation failed in InsertFrame with input: \n");
        FrameHeader h;
        for(int i = 0; i < oldFrames.size(); i++)
        {
            h = oldFrames[i];
            printv("%d: start=%d, length=%d, tail=%d, stride=%f\n", i, h.head, h.length, h.tail, h.oversampling);
        }
        printv("======\n");
    }
    /*if(FrameRingBuffer_Warn(stride, x) > 1)
    {

    }*/
#endif
}

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

void FrameRingBuffer_GetPtrFrame(int *frame, int *offset, FrameRingBuffer *x)
{
    FrameHeader *f;
    int h, t, l;
    int N = x->n;
    int ptr = x->ptr;
    for(int i = 0; i < x->headers.size(); i++)
    {
        f = &x->headers[i];
        h = f->head;
        l = f->length;
        t = f->tail;

        if(
           (t == h)
           ||
           (h < t && ptr >= h)
           ||
           (h > t && (ptr < t || ptr >= h))
           )
        {
            *frame = i;
            if(ptr >= h) //TODO: or should it be ptr > h?
                *offset = ptr - h;
            else
                *offset = (ptr + N) - h;
            if(*offset > N)
                printv("%d - %d = %d\n", ptr, h, *offset);
            return;
        }
    }
    printv("ERROR: GetPtrFrame: uncaught case!!!\n");
    FrameRingBuffer_Print(x);
}


void InsertAndRestore(int n, float oversampling, FrameRingBuffer *x)
{
    //TODO: There is 1 uncaught case: if the previous frame has same oversampling and it exactly adjacent to ptr => should concatenate
    
    int p, o, originalP, length = 0, behind, ahead, head = x->ptr, tail = -1, delta1, delta2;
    
    ///1. Get the frame where head is located
    FrameRingBuffer_GetPtrFrame(&p, &o, x);
    originalP = p;
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
    
    ///1. split frame if pointer is in the middle of a frame
    behind = o;
    ahead = existingFrame->length - o;
    if(behind > 0)
    {
        FrameHeader h;
        h.head = existingFrame->head;
        h.tail = head;
        h.length = behind;
        h.oversampling = existingFrame->oversampling;
        
        //edit beofre inserting new frame, otherwise memory address is wrong:
        existingFrame->head = head;
        existingFrame->length -= behind;
        
        //Insert new frame before ptrFrame:
        x->headers.insert(x->headers.begin() + p, h);
        p++;
    }
    ///2. consume (part of the) second half of the frame
    if(ahead > n)
    {
        //       head                   tail                   head                   tail
        //        |nnnnnnnnnnnnnnnnnnnnnn|            =>        |nnnnnnnnnnnnnnnnnnnnnn|oooooooo|
        //        |ooooooooooooooooooooooooooooooo|
        
        //head does not change
        length += n;
        
        existingFrame->length -= n;
        assert(existingFrame->length >= 0);
        existingFrame->head = (head + length) % x->n;
        //existingFrame->tail is OK
    }
    else
    {
        length += ahead;
        x->headers.erase(x->headers.begin() + p);
        p--;
    }
    
    ///3. Consume frames until length is reached
    while(length < n)
    {
        p = (p + 1) % x->headers.size();
        existingFrame = &x->headers[p];
        delta1 = n - length;
        if(existingFrame->length <= delta1)
        {
            length += existingFrame->length;
            x->headers.erase(x->headers.begin() + p);
            p--;
        }
        else
        {
            delta2 = existingFrame->length - delta1;
            existingFrame->head = (existingFrame->head + delta1) % x->n;
            existingFrame->length = delta2;
            assert(existingFrame->length >= 0);
            length = n;
        }
    }
    
    ///4. Concatenate with previous frame if exists and has same oversampling
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
    ///5. Concatenate with next frame if exists and has same oversampling (This breaks. disabling it makes the ringbuffer have 2 frames with same stride, worst case. we'll live with it for now...)
    /*if(x->headers.size() > 1)
    {
        existingFrame = &x->headers[0]; //nextFrame
        if(existingFrame->oversampling == oversampling)
        {
            length += existingFrame->length;
            x->headers.erase(x->headers.begin());
        }
    }*/
    
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
    //insertFrame(n, oversampling, x);
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
    /* Find starting point */
    assert(offset < 0);
    assert(oversampling > 0);
    assert(abs(offset) <= x->n);
    assert(n <= x->n);
    
    int f;
    int o;
    FrameRingBuffer_GetPtrFrame(&f, &o, x);
    //printv("ptr (%d) is in frame %d with offset %d\n", x->ptr, f, o);
    int headerIndex = f;
    FrameHeader *header = &(x->headers[f]);
    
    float samplesInFrame = o * (header->oversampling / oversampling);
    float samplesNeeded = -offset - samplesInFrame;
    
    /* Find the position to start reading from */
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
    
    //vStride = stride / header->stride;
    samplesInFrame = header->length * header->oversampling / oversampling;
    // Calculate the offset from the start of the frame to the first sample to read
    // At this point, samplesNeeded is either 0 or negative; negative meaning we backtracked "too far", so we have to start reading so much into the frame
    samplesNeeded = -samplesNeeded;
    
    for(int i = 0; i < n; i++)
    {
        //Skip some frames if necessary
        while(samplesInFrame < samplesNeeded)
        {
            //assert(i != 0); //in first iteration we should always be in the correct frame
            samplesNeeded -= samplesInFrame;
            headerIndex = (headerIndex + 1) % x->headers.size();
            header = &x->headers[headerIndex];
            //vStride = stride / header->stride;
            samplesInFrame = header->length * header->oversampling / oversampling;
            fPtr = header->head;
        }
        
        //we will advance samplesNeeded samples into the frame in order to copy the next sample, so they are not available anymore.
        samplesInFrame -= samplesNeeded;
        
        //We have found the frame in which the next sample to copy is located, so we update our pointers and copy 1 sample to dest.
        fPtr += samplesNeeded * oversampling / header->oversampling;
        if(fPtr >= x->n)
            fPtr -= x->n;
        ptr = (int)(fPtr + 0.5f) % x->n;
        //printv("copying: %f\n", x->data[ptr]);
        dest[i] = x->data[ptr];
        
        //we need to advance vStride samples to be able to copy the next sample.
        samplesNeeded = 1;
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

int FrameRingBuffer_Warn(float stride, FrameRingBuffer *x)
{
    int count = 0;
    for(int i = 0; i < x->headers.size(); i++)
    {
        if(x->headers[i].oversampling == stride)
            count++;
    }
    return count;
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
