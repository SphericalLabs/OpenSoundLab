// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
//
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
//
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
//
//
// Copyright © 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright © 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright © 2017 Apache 2.0 Google LLC SoundStage VR
//
// Licensed under the Apache License, Version 2.0 (the "License");
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

//
//  main.cpp
//  SoundStageTest
//
//  Created by hb on 16.12.21.
//

#include <iostream>
#include <stdio.h>
#include "Compressor.h"
#include <string.h>
#include <stdlib.h>
#include "StereoVerb.h"
#include "RingBuffer.h"
#include "Delay.h"
#include "CompressedRingBuffer.h"
#include "CRingBuffer.hpp"
#include "resample.h"
#include "Artefact.h"

#define VECTORSIZE 512
#define ITERATIONS 100000

void CompressorTest() {
    printv("%s\n", __func__);
    struct CompressorData* x = Compressor_New(44100);
    int i = 0;
    float buf[512];
    memset(buf, 0, 512 * sizeof(float));
    while (i < 100) {
        Compressor_SetParam(i++, 6, x);
        Compressor_Process(buf, buf, 512, 2, x);
    }

    Compressor_Free(x);
}

void CompressedRingBufferTest(int iterations) {
    printv("%s\n", __func__);
    int n = 1024;
    CompressedRingBuffer* x = CompressedRingBuffer_New(n);

    /*for(int i = 0; i < n; i++)
    {
        printv("%f\n", x->buf[i]);
    }*/

    float src[n];
    for (int i = 0; i < n; i++) {
        src[i] = i;
    }

    float f2[1400];

    for (int i = 0; i < iterations; i++) {
        CompressedRingBuffer_Write(src, n, 1, x);

        // CompressedRingBuffer_Print(x);

        CompressedRingBuffer_Read(f2, n, -1000, 200, x);
    }

    /*for(int i = 0; i < 16; i++)
    {
        printv("%f\n", f2[i]);
    }*/

    CompressedRingBuffer_Free(x);
}

void RingBufferTest() {
    printv("%s\n", __func__);
    int n = 1024;
    struct RingBuffer* x = RingBuffer_New(n);

    /*for(int i = 0; i < n; i++)
    {
        printv("%f\n", x->buf[i]);
    }*/

    float src[n];
    for (int i = 0; i < n; i++) {
        src[i] = i;
    }

    for (int i = 0; i < n; i++) {
        // printv("%f\n", src[i]);
        RingBuffer_Write(src + i, 1, x);
    }

    /*for(int i = 0; i < n; i++)
    {
        printv("%f\n", x->buf[i]);
    }*/

    /*float f[4];
    for(int i = 0; i < n/4; i++)
    {
        RingBuffer_Read(f, 4, 4*i, x);
        printv("%f\n", f[0]);
        printv("%f\n", f[1]);
        printv("%f\n", f[2]);
        printv("%f\n", f[3]);
    }*/

    float f2[1400];
    RingBuffer_Read(f2, 1400, 0, x);
    for (int i = 0; i < 1400; i++) {
        printv("%f\n", f2[i]);
    }

    RingBuffer_Free(x);
}

void RingBufferPaddedWriteTest(int iterations) {
    printv("%s\n", __func__);
    int n = 32;
    struct RingBuffer* x = RingBuffer_New(n);

    float result[n];

    int k = 32;

    float a[k];

    _fZero(result, n);
    _fZero(a, k);

    for (int i = 0; i < k; i++)
        a[i] = 1 + i;

    // int delay = (int) (0.030f * 48000);
    int delay = k / 5;

    float writeStride = (float) n / (float) delay;
    float readStride = writeStride;
    int offset = -(k * (writeStride)); // seems correct

    // assert(writeStride < n /2);
    // assert(delay >= k);

    for (int i = 0; i < iterations; i++) {
        printv("\n\niteration %d:", i);
        int wrt = RingBuffer_WritePadded(a, k, writeStride, INTERPOLATION_NONE, x);
        // int wrt = RingBuffer_WritePadded(a, k, writeStride, x);

        RingBuffer_ReadPadded(result, k, offset, readStride, x);
        // RingBuffer_ReadPadded(result, k, -k, readStride, x);
        printv("stride %f, wrote %d samples\n", writeStride, wrt);
        for (int j = 0; j < n; j++) {
            printv("ringbuffer[%d] == %f\n", j, x->buf[j]);
        }
        printv("rb pointer: %d\n", x->ptr);
        for (int j = 0; j < k; j++) {
            // printv("a[%d] == %f\n", i, result[i]);
            // assert(a[j] == result[j]);
            if (a[j] != result[j])
                printv("!!!!");
            printv("result[%d] == %f // a[%d] == %f\n", j, result[j], j, a[j]);
        }
    }

    RingBuffer_Free(x);
}

void BufferComparison(int iterations, float writeStride, float readStride, int readOffset) {
    int bufSize = 48000;
    int dspSize = 512;

    CompressedRingBuffer* cb = CompressedRingBuffer_New(bufSize);
    struct RingBuffer* rb = RingBuffer_New(bufSize);
    FrameRingBuffer* fb = FrameRingBuffer_New(bufSize);

    float src1[dspSize];
    float src2[dspSize];
    float src3[dspSize];
    for (int i = 0; i < 0; i++)
        src1[i] = src2[i] = src3[i] = i;

    printv("--------\n");
    printv("CompressedBuffer (%d iterations):\n", iterations);
    double startTime, endTime, startcpuTime, endcpuTime;
    startTime = _wallTime();
    startcpuTime = _cpuTime();

    for (int i = 0; i < iterations; i++) {
        CompressedRingBuffer_Write(src1, dspSize, writeStride, cb);
        CompressedRingBuffer_Read(src1, dspSize, readOffset, readStride, cb);
    }

    endTime = _wallTime();
    endcpuTime = _cpuTime();
    printv("duration: %f (walltime) / %f (cpu time)\n", endTime - startTime, endcpuTime - startcpuTime);
    printv("--------\n");

    printv("--------\n");
    printv("RingBuffer (%d iterations):\n", iterations);
    startTime = _wallTime();
    startcpuTime = _cpuTime();

    for (int i = 0; i < iterations; i++) {
        RingBuffer_WritePadded(src2, dspSize, writeStride, INTERPOLATION_NONE, rb);
        RingBuffer_ReadPadded(src2, dspSize, readOffset, readStride, rb);
    }

    endTime = _wallTime();
    endcpuTime = _cpuTime();
    printv("duration: %f (walltime) / %f (cpu time)\n", endTime - startTime, endcpuTime - startcpuTime);
    printv("--------\n");

    printv("--------\n");
    printv("FrameRingBuffer (%d iterations):\n", iterations);
    startTime = _wallTime();
    startcpuTime = _cpuTime();

    for (int i = 0; i < iterations; i++) {
        FrameRingBuffer_Write(src3, dspSize, writeStride, fb);
        FrameRingBuffer_Read(src3, dspSize, readOffset, readStride, INTERPOLATION_NONE, fb);
    }

    endTime = _wallTime();
    endcpuTime = _cpuTime();
    printv("duration: %f (walltime) / %f (cpu time)\n", endTime - startTime, endcpuTime - startcpuTime);
    printv("--------\n");
}

void StereoVerbTest(int iterations) {
    printv("%s\n", __func__);
    size_t n = 16;

    float buf[n];
    for (int i = 0; i < n; i += 2) {
        buf[i] = 0.25f + 1.0f / (i + 1);
        buf[i + 1] = 0.25f + 1.0f / (i + 1);
    }

    for (int i = 0; i < n; i++) {
        printv("%f\n", buf[i]);
    }
    printv("-----------\n");

    freeverb::ReverbModel* x = StereoVerb_New(48000);
    StereoVerb_SetParam(0, 0.5f, x); // roomsize
    // StereoVerb_SetParam(4, 0.0f, x); //width
    // StereoVerb_SetParam(2, 0.0f, x); //dry level
    // StereoVerb_SetParam(3, 2.0f, x); //wet level
    for (int i = 0; i < iterations; i++)
        StereoVerb_Process(buf, n, 2, x);

    for (int i = 0; i < n; i++) {
        printv("%f\n", buf[i]);
    }

    StereoVerb_Free(x);
}

void DelayTest(int iterations) {
    printv("%s\n", __func__);

    int maxTime = 480000;
    int n = 256;
    int channels = 1;

    DelayData* x = Delay_New(maxTime);
    Delay_SetParam(INTERPOLATION_LINEAR, 5, x);

    float buf[n];
    float temp[n];
    _fZero(temp, n);

    /*for(int i = 0; i < n; i++)
    {
        printv("%f\n", buf[i]);
    }*/

    x->time = 48;
    x->wet = 1.0f;
    x->dry = 0.0f;
    x->feedback = 0;

    for (int j = 0; j < n / channels; j++) {
        // buf[j] = j;
        for (int c = 0; c < channels; c++) {
            buf[j * channels + c] = j + 1;
        }
    }

    for (int i = 0; i < iterations; i++) {

        /*printv("Delay Input:\n");
        for(int k = 0; k < n; k++)
        {
            printv("%f\n", buf[k]);
        }
        printv("----\n");*/

        if (i % 2 == 0) {
            float f = rand() / (float) RAND_MAX;
            int delay = 1 + (int) (f * (maxTime - 1));
            // int delay = 100000;
            printv("delay : %d\n", delay);

            Delay_SetParam(delay, 0, x);
        }

        Delay_Process(buf, n, channels, x);

        // FrameRingBuffer_Validate(x->tap);

        /*printv("Delay Output:\n");
        for(int k = 0; k < n; k++)
        {
            printv("%f\n", buf[k]);
        }
        printv("----\n");*/

        /*printv("Ringbuffer:\n");
        for(int k = 0; k < x->tap->n; k++)
        {
            printv("%f\n", x->tap->buf[k]);
        }
        printv("----\n");*/

        for (int j = 0; j < n / channels; j++) {
            // buf[j] = j;
            for (int c = 0; c < channels; c++) {
                buf[j * channels + c] = j + 1;
            }
        }
    }

    // FrameRingBuffer_Print(x->tap);
    // FrameRingBuffer_Validate(x->tap);

    Delay_Free(x);
}

void InterleaveTestInPlace(int iterations) {
    printv("%s\n", __func__);
    int n = VECTORSIZE;
    int channels = 2;
    float buf[n];

    int m = n / channels;
    for (int i = 0; i < m; i++) {
        for (int j = 0; j < channels; j++)
            buf[j * m + i] = i;
    }

    for (int i = 0; i < iterations; i++) {
        printv("Start array:\n");
        for (int i = 0; i < n; i++) {
            printv("%f\n", buf[i]);
        }

        _fInterleave(buf, buf, n, channels);

        printv("Interleaved:\n");
        for (int i = 0; i < n; i++) {
            printv("%f\n", buf[i]);
        }

        _fDeinterleave(buf, buf, n, channels);

        printv("De-interleaved:\n");
        for (int i = 0; i < n; i++) {
            printv("%f\n", buf[i]);
        }
    }
}

void InterleaveTest(int iterations) {
    printv("%s\n", __func__);
    int n = VECTORSIZE;
    int channels = 2;
    float interleaved[n];
    float deinterleaved[n];
    _fZero(deinterleaved, n);
    _fZero(interleaved, n);

    int m = n / channels;
    for (int i = 0; i < m; i++) {
        for (int j = 0; j < channels; j++)
            deinterleaved[j * m + i] = i;
    }

    for (int i = 0; i < iterations; i++) {
        printv("Start array:\n");
        for (int i = 0; i < n; i++) {
            printv("%f\n", deinterleaved[i]);
        }

        _fZero(interleaved, n);
        _fInterleave(deinterleaved, interleaved, n, channels);

        printv("Interleaved:\n");
        for (int i = 0; i < n; i++) {
            printv("%f\n", interleaved[i]);
        }

        _fZero(deinterleaved, n);
        _fDeinterleave(interleaved, deinterleaved, n, channels);

        printv("De-interleaved:\n");
        for (int i = 0; i < n; i++) {
            printv("%f\n", deinterleaved[i]);
        }
    }
}

void SumOfMagsTest(int iterations) {
    printv("%s\n", __func__);
    int n = VECTORSIZE;
    float buf[n];

    for (int i = 0; i < n; i++)
        buf[i] = -1.0f;

    float sum;
    for (int i = 0; i < iterations; i++)
        sum = _fSumOfMags(buf, n);

    printv("sum of mags: %f\n", sum);
}

void ScaleTest(int iterations) {
    printv("%s\n", __func__);
    int n = VECTORSIZE;
    float buf[n];

    for (int i = 0; i < n; i++)
        buf[i] = i;

    for (int i = 0; i < iterations; i++)
        _fScale(buf, buf, 2.0f, n);

    /*for(int i = 0; i < n; i++)
        printv("%f\n", buf[i]);*/
}

void MultiplyTest(int iterations) {
    printv("%s\n", __func__);
    int n = VECTORSIZE;
    float buf1[n];
    float buf2[n];
    float dest[n];
    _fZero(dest, n);

    for (int i = 0; i < n; i++) {
        buf1[i] = i;
        buf2[i] = i - 50;
    }

    for (int i = 0; i < iterations; i++)
        _fMultiply(buf1, buf2, dest, n);

    /*for(int i = 0; i < n; i++)
        printv("%f\n", dest[i]);*/
}

void AddTest(int iterations) {
    printv("%s\n", __func__);
    int n = VECTORSIZE;
    float buf1[n];
    float buf2[n];
    float dest[n];
    _fZero(dest, n);

    for (int i = 0; i < n; i++) {
        buf1[i] = 1.0f / i;
        buf2[i] = 1.0f / i;
    }

    for (int i = 0; i < iterations; i++)
        _fAdd(buf1, buf2, dest, n);

    /*for(int i = 0; i < n; i++)
        printv("%f\n", dest[i]);*/
}

void MultiplyAddTest(int iterations) {
    printv("%s\n", __func__);
    int n = VECTORSIZE;
    float buf1[n];
    float buf2[n];
    float dest[n];
    _fZero(dest, n);

    for (int i = 0; i < n; i++) {
        buf1[i] = i;
        buf2[i] = i;
        dest[i] = 1;
    }

    for (int i = 0; i < iterations; i++)
        _fMultiplyAdd(buf1, buf2, dest, n);

    /*for(int i = 0; i < n; i++)
        printv("%f\n", dest[i]);*/
}

void AbsTest(int iterations) {
    printv("%s\n", __func__);
    int n = VECTORSIZE;
    float buf[n];

    for (int i = 0; i < n; i++) {
        buf[i] = -i;
    }

    for (int i = 0; i < iterations; i++)
        _fAbs(buf, buf, n);

    for (int i = 0; i < n; i++)
        printv("%f\n", buf[i]);
}

void CopyTest(int iterations) {
    printv("%s\n", __func__);
    int n = VECTORSIZE;
    float buf[n];
    float dest[n];

    for (int i = 0; i < n; i++) {
        buf[i] = i;
    }

    for (int i = 0; i < iterations; i++)
        _fCopy(buf, dest, n);

    /*for(int i = 0; i < n; i++)
        printv("%f\n", dest[i]);*/
}

void ZeroTest(int iterations) {
    printv("%s\n", __func__);
    int n = VECTORSIZE;
    float buf[n];

    for (int i = 0; i < n; i++) {
        buf[i] = -i;
    }

    for (int i = 0; i < iterations; i++)
        _fZero(buf, n);

    for (int i = 0; i < n; i++)
        printv("%f\n", buf[i]);
}

void ExpCurveTest(int iterations) {
    float grid = 0.1f;
    float x = 0.0f;
    float ym = 0.1f;

    for (int i = 0; i < iterations; i++) {
        while (x <= 1) // Take care, float values will often be something like 1.0000034, which makes the loop terminate
                       // earlier than you would except...
        {
            printv("%f\n", _expCurve(x, ym));
            x += grid;
        }
    }
}

void MapTest(int iterations) {
    float start1 = 0;
    float stop1 = 1;
    float start2 = 0;
    float stop2 = -40;

    float grid = 0.1f;
    float x = start1;
    float ym = 0.2f;

    for (int i = 0; i < iterations; i++) {
        while (x <= stop1) // Take care, float values will often be something like 1.0000034, which makes the loop
                           // terminate earlier than you would except...
        {
            printv("%f\n", _map(x, start1, stop1, start2, stop2, ym));
            x += grid;
        }
    }
}

void FrameBufferStressTest(int iterations) {
    int n = 44100;
    float src[n];
    float dest[n];
    for (int i = 0; i < n; i++) {
        src[i] = (float) ((double) rand() / INT_MAX);
    }
    _fZero(dest, n);

    FrameRingBuffer* x = FrameRingBuffer_New(n);

    int lower = 256;
    int upper = 512;

    for (int i = 0; i < iterations; i++) {
        int m = (rand() % (upper - lower + 1)) + lower;
        float sr = (float) rand() / (float) (RAND_MAX / 300.0f);
        sr += 0.0001f;
        printv("m = %d, sr = %f\n", m, sr);

        FrameRingBuffer_Write(src, m, sr, x);
        FrameRingBuffer_Validate(x);
    }

    /*for(int i = 0; i < iterations; i++)
    {
        int m = (rand() % (upper - lower + 1)) + lower;
        float sr = (float)rand()/(float)(RAND_MAX/1.0f);
        int offset = (rand() % (upper - lower + 1)) + lower;
        sr += 0.0001f;
        printv("m = %d, sr = %f\n", m, sr);

        FrameRingBuffer_Read(src, m, -offset, sr, x);
    }*/

    printv("===========\n");
    FrameRingBuffer_Print(x);
    printv("===========\n");
    for (int i = 0; i < iterations; i++) {
        // printv("%d: ", i);
        int m = (rand() % (upper - lower + 1)) + lower;
        float sr = (float) rand() / (float) (RAND_MAX / 300.0f);
        int offset = (rand() % (upper - lower + 1)) + lower;
        // FrameRingBuffer_Read(dest, 1, -n+i, 5000.0f, x);
        FrameRingBuffer_Read(src, m, -offset, sr, INTERPOLATION_NONE, x);
        // printv("\n");
    }

    /// Same stride always:

    /*float sr = (float)rand()/(float)(RAND_MAX/300.0f);
    sr += 0.0001f;
    for(int i = 0; i < iterations; i++)
    {
        int m = (rand() % (upper - lower + 1)) + lower;
        printv("m = %d, sr = %f\n", m, sr);

        FrameRingBuffer_Write(src, m, sr, x);

        //float f = 75.0f;
        //FrameRingBuffer_Write(&f, 2, 1.0f, x);
    }

    sr = (float)rand()/(float)(RAND_MAX/1.0f);
    sr += 0.0001f;
    for(int i = 0; i < iterations; i++)
    {
        int m = (rand() % (upper - lower + 1)) + lower;
        int offset = (rand() % (upper - lower + 1)) + lower;
        printv("m = %d, sr = %f\n", m, sr);

        FrameRingBuffer_Read(src, m, -offset, sr, x);
    }*/
}

void FrameBufferTest(int iterations) {
    int n = 32;
    float src[n];

    for (int i = 0; i < n; i++) {
        src[i] = i;
    }

    float dest[n];
    _fZero(dest, n);

    FrameRingBuffer* x = FrameRingBuffer_New(n);

    FrameRingBuffer_Write(src, 10, 1, x);
    // FrameRingBuffer_Validate(x);
    FrameRingBuffer_Read(dest, 32, -1, 1, INTERPOLATION_NONE, x);
    // FrameRingBuffer_Print(x);

    FrameRingBuffer_Write(src, 20, 0.9f, x);
    // FrameRingBuffer_Validate(x);
    FrameRingBuffer_Read(dest, n, -1, 1, INTERPOLATION_NONE, x);
    // FrameRingBuffer_Print(x);

    FrameRingBuffer_Write(src, 24, 0.0023f, x);
    // FrameRingBuffer_Validate(x);
    FrameRingBuffer_Read(dest, n, -1, 1, INTERPOLATION_NONE, x);
    // FrameRingBuffer_Print(x);

    FrameRingBuffer_Write(src, 5, 1.05f, x);
    // FrameRingBuffer_Validate(x);
    FrameRingBuffer_Read(dest, n, -n, 1, INTERPOLATION_NONE, x);

    // FrameRingBuffer_Print(x);
    printv("dest array (x->ptr == %d):\n", x->ptr);
    for (int i = 0; i < n; i++) {
        printv("%d: %f\n", i, dest[i]);
    }
    printv("--------\n");

    // FrameRingBuffer_Write(src, 15, 0.95f, x);
    // FrameRingBuffer_Validate(x);

    // FrameRingBuffer_Print(x);
}

void Xfade_Log_Test(int iterations) {
    printv("%s\n", __func__);

    int n = 256;
    float src1[n];
    float src2[n];

    for (int i = 0; i < n; i++) {
        src1[i] = rand() / (float) RAND_MAX;
        src2[i] = rand() / (float) RAND_MAX;
        // src1[i] = 0.5f;
        // src2[i] = 0.0f;
    }

    for (int i = 0; i < iterations; i++) {
        //_fCrossfadeLinear(src1, src2, src2, n);
        _fCrossfadeLogarithmic(src1, src2, src2, true, n);
        /*for(int j = 0; j < n; j++)
        {
            printv("%f, ", src2[j]);
        }
        printv("\n\n");*/
    }
}

void MeasuredTestCase(void (*foo)(void), int iterations) {
    printv("--------\n");
    printv("Starting measured test case (%d iterations):\n", iterations);
    double startTime, endTime, startcpuTime, endcpuTime;
    startTime = _wallTime();
    startcpuTime = _cpuTime();
    for (int i = 0; i < iterations; i++)
        foo();
    endTime = _wallTime();
    endcpuTime = _cpuTime();
    printv("duration: %f (walltime) / %f (cpu time)\n", endTime - startTime, endcpuTime - startcpuTime);
    printv("--------\n");
}

void MeasuredNestedTestCase(void (*foo)(int), int iterations) {
    printv("--------\n");
    printv("Starting measured test case (%d iterations):\n", iterations);
    double startTime, endTime, startcpuTime, endcpuTime;
    startTime = _wallTime();
    startcpuTime = _cpuTime();
    foo(iterations);
    endTime = _wallTime();
    endcpuTime = _cpuTime();
    printv("duration: %f (walltime) / %f (cpu time)\n", endTime - startTime, endcpuTime - startcpuTime);
    printv("--------\n");
}

int main(int argc, const char* argv[]) {
    printv("Hello, World!\n");
    //_neonFeatures();
    // CompressorTest();
    // RingBufferTest();
    // MeasuredNestedTestCase(StereoVerbTest, 1);
    MeasuredNestedTestCase(DelayTest, 4000);
    // MeasuredNestedTestCase(CompressedRingBufferTest, 1000);
    // MeasuredNestedTestCase(RingBufferPaddedWriteTest, 1);

    // BufferComparison(1000, 1, 200, -1000); //Here CompressedBuffer should be very slow
    // BufferComparison(1000, 200, 1, -1000); //Here RingBuffer should be very slow

    // BufferComparison(1000, 1, 1, -1000);

    // FrameBufferTest(1);
    // FrameBufferStressTest(200);

    // MeasuredTestCase(InterleaveTest, 100000);
    // MeasuredNestedTestCase(InterleaveTest, 1);
    // MeasuredNestedTestCase(InterleaveTestInPlace, 1);

    // MeasuredNestedTestCase(SumOfMagsTest, ITERATIONS);

    // MeasuredNestedTestCase(ScaleTest, ITERATIONS);
    // MeasuredNestedTestCase(MultiplyTest, 100000);
    // MeasuredNestedTestCase(AddTest, 100000);
    // MeasuredNestedTestCase(MultiplyAddTest, ITERATIONS);
    // MeasuredNestedTestCase(AbsTest, 1);

    // MeasuredNestedTestCase(CopyTest, ITERATIONS);
    // MeasuredNestedTestCase(ZeroTest, ITERATIONS);

    // ExpCurveTest(1);
    // MapTest(1);

    // RingBufferPaddedWriteTest(10);

    /*for(int i = 0; i < 256; i++)
        printv("%d => %d\n", i, _isPowOf2(i));*/
    // insert code here...

    // float w = _map(0.5f, 0, 1, -96, 0, 0.5f);
    // float x = _dbtoa(w);
    // printv("w is %f\n", w);
    // printv("x is %f\n", x);

    // MeasuredNestedTestCase(Xfade_Log_Test, 100000);

    // createResampleTable();

    // Artefact_Process(NULL, 0, 0, 0);

    printv("Bye, World!\n");
    return 0;
}
