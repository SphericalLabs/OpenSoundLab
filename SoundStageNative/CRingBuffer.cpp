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

void insertFrame(int length, float stride, FrameRingBuffer *x)
{
    assert(length > 0);
    
    if(length > x->n)
        length = x->n;
    //vector<FrameHeader> frames = x->headers;
    int ptr = x->ptr;
    int N = x->n;
    FrameHeader *nextFrame;
    FrameHeader frame;
    frame.length = 0;
    frame.head = ptr;
    frame.stride = stride;
    int head, tail;
    
    //Check if we can attach to oldest frame
    /*nextFrame = &x->headers[0];
    if(nextFrame->stride == frame.stride)
    {
        //concatenate
    }
    //Check if we can attach to most recent frame
    else if(x->headers.size() > 1)
    {
        nextFrame = &x->headers.back();
        if(nextFrame->stride == frame.stride)
        {
            //concatenate
        }
    }*/
    
    //Delete headers until we have enough samples
    while(frame.length < length)
    {
        assert(x->headers.size() > 0);
        nextFrame = &x->headers[0];
        //need to concatenate frames, so delete existing one and resize new one
        if(nextFrame->stride == stride)
        {
            /*
             need to check if
             (1)
             |----|     (nextFrame)
                |----|  (frame)
             or (2)
                |----|
             |-----|
             or (3)
             |--------|
               |---|
             or (4)
               |---|
             |--------|
             */
            head = frame.head; //only valid in case 2!
            //case 1: head = nextFrame.head; frame.length = ???)
            //case 3: frame.head = nextFrame.head; frame.frameLength = nextframe.LframeLength; delete nextFrame;
            //case 4: frame.head = head; frame.
            tail = (nextFrame->head + nextFrame->length) % N;
            frame.length = tail - head;
            if( frame.length <= 0)
                frame.length += N;
            //Note: at this point, frame.frameLength can be > length, which is alright.
            
            //TODO: what if frame.frameLength > N?
            assert(frame.length <= N);
            
            x->headers.erase(x->headers.begin());
        }
        else
        {
            int remaining = length - frame.length;
            //need to consume whole frame, so delete it
            if(nextFrame->length <= remaining)
            {
                frame.length += nextFrame->length;
                x->headers.erase(x->headers.begin());
            }
            //no need to consume whole frame, so resize it
            else
            {
                nextFrame->length -= remaining;
                nextFrame->head = (nextFrame->head + remaining) % N;
                frame.length = length;
            }
        }
    }
    
    //check if previous frame has same stride, if so concatenate
    if(x->headers.size() > 0)
    {
        FrameHeader *previousFrame = &x->headers.back();
        if(previousFrame->stride == frame.stride)
        {
            frame.length += previousFrame->length;
            frame.head = previousFrame->head;
            x->headers.pop_back();
            assert(frame.length <= N);
        }
    }
    
    x->headers.push_back(frame);
}

inline bool _overlap(int head1, int tail1, int head2, int tail2)
{
    if(tail1 == head1 || tail2 == head2)
    {
        /*
         |--------------------|-------------------|
         0                head1/tail1             n-1
         */
        return true;
    }
    else if(tail1 > head1)
    {
        /*
         |        |-----------------|         |
         0      head1              tail1        n-1
         */
        if(tail2 > head2)
        {
            /*
             |        |-----------------|         |
             |    |-----------------|             |
             */
            
            //char *x = a, *y =  b;
            //return (x<=y && x+n>y) || (y<=x && y+n>x);
            
            return head1 <= head2 && head1 + (tail1 - head1) > head2 || head2 >= head1 && head2 + (tail2 - head2) > head1;
        }
        else
        {
            /*
             |        |-----------------|         |
             |----------|              |----------|
             */
            return head2 < tail1 || tail2 >= head1;
        }
    }
    else
    {
        /*
         |-----|         |-------------|
         0    tail1      head1          n-1
         */
        
        if(tail2 > head2)
        {
            /*
             |----------|              |----------|
             |              |-----------------|   |
             */
            return head1 < tail2 || tail1 >= head2;
        }
        else
        {
            /*
             |-------------|              |-------|
             |----------|              |----------|
             */
            return tail2 > 0 || head2 < tail1; //TODO: second part is bullshit
        }
    }
}

/* Returns the number of samples that overlap in the two arrays defined by [head1, tail1] and [head2, tail2]. */
inline int _delta(int head1, int tail1, int head2, int tail2, int n)
{
    int delta = 0;
    int length1 = head1 < tail1 ? tail1 - head1 : n - head1 + tail1;
    int length2 = head2 < tail2 ? tail2 - head2 : n - head2 + tail2;
    
    if(tail1 == head1)
    {
        /*
         |--------------------|-------------------|
         0                head1/tail1             n-1
         */
        delta = length2;
    }
    else if(tail2 == head2)
    {
        delta = length1;
    }
    else if(tail1 > head1)
    {
        if(tail2 > head2)
        {
            /*
             0      head1             tail1     n-1
             |        |-----------------|         |
             |    |-----------------|             |
             0   head2             tail2        n-1
             */
            
            //char *x = a, *y =  b;
            //return (x<=y && x+n>y) || (y<=x && y+n>x);
            
            //return head1 <= head2 && head1 + (tail1 - head1) > head2 || head2 >= head1 && head2 + (tail2 - head2) > head1;
            delta = length2 - (abs(head2 - head1) + abs(tail2 - tail2));
        }
        else
        {
            /*
             0       head1             tail1     n-1
             |        |-----------------|         |
             |----------|              |----------|
             0        tail1           head1      n-1
             */
            if(tail1 > head2) delta += tail1 - head2;
            if(tail2 > head1) delta += tail2 - head1;
        }
    }
    else
    {
        if(tail2 > head2)
        {
            /*
             0        tail1          head1       n-1
             |----------|              |----------|
             |              |-----------------|   |
             0             head2            tail2
             */
            if(tail1 > head2) delta += tail1 - head2;
            if(tail2 > head1) delta += tail2 - head1;
            //return head1 < tail2 || tail1 >= head2;
        }
        else
        {
            /*
             |-------------|              |-------|
             |----------|              |----------|
             */
            delta += tail1 > tail2 ? tail2 : tail1;
            delta += head1 > head2 ? n - head1 : n - head2;
        }
    }
    
    return delta;
}

inline void _strip(FrameHeader* frame, int head1, int tail1, int head2, int tail2, int n)
{
    int length1 = head1 < tail1 ? tail1 - head1 : n - head1 + tail1;
    int length2 = head2 < tail2 ? tail2 - head2 : n - head2 + tail2;
    
    if(tail1 == head1)
    {
        /*
         |--------------------|-------------------|
         0                head1/tail1             n-1
         */
        frame->length = 0;
        //no need to do anything else, frame will be deleted anyway
    }
    else if(tail2 == head2)
    {
        frame->length -= length1;
        frame->head = tail1;
        /*
                --------
         -----------------|--------
         */
    }
    else if(tail1 > head1)
    {
        if(tail2 > head2)
        {
            /*
             0      head1             tail1     n-1
             |        |-----------------|         |
             |    |-----------------|             |
             0   head2             tail2        n-1
             */
            
            //char *x = a, *y =  b;
            //return (x<=y && x+n>y) || (y<=x && y+n>x);
            
            //return head1 <= head2 && head1 + (tail1 - head1) > head2 || head2 >= head1 && head2 + (tail2 - head2) > head1;
            int delta = length2 - (abs(head2 - head1) + abs(tail2 - tail2));
            frame->length -= delta;
            frame->head += delta;
        }
        else
        {
            /*
             0       head1             tail1     n-1
             |        |-----------------|         |
             |----------|              |----------|
             0        tail1           head1      n-1
             */
            if(tail1 > head2)
            {
                frame->length -= tail1 - head2;
            }
            if(tail2 > head1)
            {
                frame->length -= tail2 - head1;
            }
        }
    }
    else
    {
        if(tail2 > head2)
        {
            /*
             0        tail1          head1       n-1
             |----------|              |----------|
             |              |-----------------|   |
             0             head2            tail2
             */
            if(tail1 > head2)
            {
                frame->length -= tail1 - head2;
                frame->head = tail1;
            }
            if(tail2 > head1)
            {
                frame->length -= tail2 - head1;
            }
            //return head1 < tail2 || tail1 >= head2;
        }
        else
        {
            /*
             |-------------|              |-------|
             |             --|          |-        | THIS CANNOT BE ALLOWED TO HAPPEN!!!!!!
             */
            if(tail1 > tail2)
            {
                frame->length -= tail2;
            }
            else
            {
                frame->length -= tail1;
            }
            if(head1 > head2)
            {
                frame->length -= head2;
            }
            else
            {
                frame->length -= head1;
            }
            frame->length -= tail1 > tail2 ? tail2 : tail1;
            frame->length -= head1 > head2 ? n - head1 : n - head2;
        }
    }
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
    x->headers[0].stride = 1;
    x->headers[0].head = 0;
    
    return x;
}

void FrameRingBuffer_Free(FrameRingBuffer *x)
{
    _free(x->data);
    _free(x);
}

void FrameRingBuffer_Write(float* src, int n, float stride, FrameRingBuffer *x)
{
    ///Clamp n
    if(n > x->n)
    {
        src = src + (n - x->n);
        n = x->n;
    }
    
    ///Insert new frame header
    insertFrame(n, stride, x);

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

/* This is efficient as long as n is reasonably large, e.g. writing buffers of 512 samples. */
/*void FrameRingBuffer_Write(float* src, int n, float stride, FrameRingBuffer *x)
{
    ///Clamp n
    if(n > x->n)
    {
        src = src + (n - x->n);
        n = x->n;
    }
    
    int head, tail;
    
    /// Write new header or modify previous header
    FrameHeader *header = &(x->headers.back());
    if(header->stride == stride)
    {
        ///Previous frame has same stride, can modify
        header->length += n;
        if(header->length > x->n)
            header->length = x->n;
        head = header->head;
        tail = (head + header->length) % x->n;
    }
    else
    {
        header = &(x->headers[0]);
        if(header->stride == stride)
        {
            ///Next frame has same stride, can modify
            header-> length += n;
            if(header->length > x->n)
                header->length = x->n;
            header->head = x->ptr;
            
            head = header->head;
            tail = (head + header->length) % x->n;
            
            //quick hack to ensure the frame that is written to is at the end of the vector
            std::rotate(x->headers.begin(), x->headers.begin()+1, x->headers.end());
        }
        else
        {
            ///Have to insert new frame with new stride
            FrameHeader newHeader;
            newHeader.length = n;
            newHeader.stride = stride;
            newHeader.head = x->ptr;
            x->headers.push_back(newHeader);
            header = &x->headers.back();
            
            head = newHeader.head;
            tail = (head + newHeader.length) % x->n;
        }
    }
    
    ///Delete/modify existing headers
    int delta, frameStart, frameLength;
    while(x->headers.size() > 1) // > 1 bc at this point at least the header we modified / added must stay in the vector.
    {
        header = &x->headers[0];
        frameStart = header->head;
        frameLength = header->length;
        
        ///
        ///    |----------|                  |--------------------------------|
        ///    0        tail2              x->ptr                           tail1
        ///
        /// ----: samples that will be overwritten
        delta = _delta(head, tail, frameStart, (frameStart + frameLength) % x->n, x->n);
        if(delta >= header->length)
        {
            x->headers.erase(x->headers.begin());
        }
        else if(delta > 0)
        {
            
 /// It seems there are cases that are not considered:
 ///            1. both tail and head of an existing frame have to be shifted
 ///               2. the new frame is completely contained within an existing frame, so the existing frame becomes now 2 frames (one before and one after the new frame)
             
 ///           This breaks in the following condition:
 ///           -------------------|-------------
 ///                            -----
 ///           (head2 is between head1 and tail1)
 ///           delta is correct but just shifting the framestart is not correct, have to -= tail as well as += head...
             
 ///            case: n is 5, ptr is 18, frameStart 20, frameLength 32.
 ///            delta is correctly calculated as 5.
 ///            but
            header->head += delta;
            header->head %= x->n;
            header->length -= delta;
            break;
        }
        else
        {
            break;
        }
    }

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
}*/

void FrameRingBuffer_Read(float* dest, int n, int offset, float stride, FrameRingBuffer *x)
{
    /* Find starting point */
    assert(offset < 0);
    assert(stride > 0);
    assert(abs(offset) <= x->n);
    assert(n <= x->n);
    
    float smplsInFrame;
    float samplesNeeded = -offset;
    float vStride = FB_UNINITIALIZED;
    FrameHeader *header = NULL;
    int headerIndex = x->headers.size() - 1;
    
    /* Find the position to start reading from */
    assert(x->headers.size() > 0);
    while(samplesNeeded > 0 && headerIndex >= 0)
    {
        header = &(x->headers[headerIndex]);
        vStride = header->stride / stride; //example: a sample that has been written with a stride of 4 but is now read with a stride of 1, needs to be copied 4 times, therefore it advances the offset by 4 samples. In a delay, this results in audio stretching / lower pitch.
        
        smplsInFrame = vStride * header->length; //number of samples as viewed from the reader's perspective.
        
        samplesNeeded -= smplsInFrame;
        if(samplesNeeded > 0)
        {
            headerIndex--;
            if(headerIndex < 0)
                headerIndex = x->headers.size() - 1;
        }
    }
    
    assert(headerIndex < x->headers.size());
    assert(headerIndex >= 0);
    /* The position to start reading from is in the current frame with an offset of (-samplesNeeded). */
    
    //assert(fPtr != FB_UNINITIALIZED);
    assert(vStride != FB_UNINITIALIZED);
    assert(header != NULL);
    int ptr;
    vStride = stride / header->stride;
    float fPtr = header->head; //the start index of the frame in which the first sample to read is located
    float samplesAvailable = header->length;
    
    // Calculate the offset from the start of the frame to the first sample to read
    // At this point, samplesNeeded is either 0 or negative; negative meaning we backtracked "too far", so we have to start reading so much into the frame
    samplesNeeded = (-samplesNeeded) * vStride;
    
    for(int i = 0; i < n; i++)
    {
        //Skip some frames if necessary
        while(samplesAvailable < samplesNeeded)
        {
            assert(i != 0); //in first iteration we should always be in the correct frame
            samplesNeeded -= samplesAvailable;
            headerIndex = (headerIndex + 1) % x->headers.size();
            header = &x->headers[headerIndex];
            samplesAvailable = header->length;
            samplesNeeded = (samplesNeeded / vStride) * (header->stride / stride);
            //samplesNeeded = samplesNeeded * (vStride / header->stride); //Scale samples with new stride
            vStride = stride / header->stride;
            fPtr = header->head;
        }
        
        //we will advance samplesNeeded samples into the frame in order to copy the next sample, so they are not available anymore.
        samplesAvailable -= samplesNeeded;
        
        //We have found the frame in which the next sample to copy is located, so we update our pointers and copy 1 sample to dest.
        fPtr += samplesNeeded;
        if(fPtr >= x->n)
            fPtr -= x->n;
        ptr = (int)(fPtr + 0.5f) % x->n;
        printv("copying: %f\n", x->data[ptr]);
        dest[i] = x->data[ptr];
        
        //we need to advance vStride samples to be able to copy the next sample.
        samplesNeeded = vStride;
        
        /*ptr = (int)(fPtr + 0.5f) % x->n;
        
        //TODO: This is not correct. if we stride into a new frame, then the part of the stride that leaps into the new frame is not correct in case header->frameStride != previous header->frameStride...
        while(ptr >= header->frameStart + header->frameLength)
        {
            headerIndex++;
            header = &(x->headers[headerIndex]);
            vStride = stride / header->frameStride;
        }
        dest[i] = x->data[ptr];
        fPtr += vStride;*/
        
        
        
        /*leaking = fPtr - (header->frameStart + header->frameLength);
        while(leaking > 0)
        {
            headerIndex++;
            header = &(x->headers[headerIndex]);
            fPtr -= leaking;
            fPtr += leaking * (vStride / header->frameStride);
            vStride = stride / header->frameStride;
            leaking = fPtr - (header->frameStart + header->frameLength);
        }

        ptr = (int)(fPtr + 0.5f) % x->n;
        dest[i] = x->data[ptr];
        fPtr += vStride;*/
    }
}

bool FrameRingBuffer_Validate(FrameRingBuffer *x)
{
    bool legit = true;
    
    int numSmpls = 0;
    for(int i = 0; i < x->headers.size(); i++)
        numSmpls += x->headers[i].length;
    assert(numSmpls == x->n);
    
    int adjacentFramesWithSameStride = 0;
    if(x->headers.size() > 1)
    {
        for(int i = 0; i < x->headers.size(); i++)
        {
            float stride1 = x->headers[i].stride;
            float stride2 = x->headers[(i + 1) % x->headers.size()].stride;
            if (stride1 == stride2)
                adjacentFramesWithSameStride++;
        }
    }

    assert(adjacentFramesWithSameStride == 0);
    
    int corruptCounds = 0;
    for(int i = 0; i < x->headers.size() - 1; i++)
    {
        int tail = (x->headers[i].head + x->headers[i].length) % x->n;
        int nextHead = x->headers[i+1].head;
        if(tail > nextHead)
            corruptCounds++;
    }
    assert(corruptCounds == 0);
        
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
        printv("%d: start=%d, length=%d, stride=%f\n", i, h.head, h.length, h.stride);
    }
    printv("======\n");
    printv("BUFFER:\n");
    for(int i = 0; i < x->n; i++)
    {
        printv("%d: %f\n", i, x->data[i]);
    }
}
