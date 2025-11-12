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
//  CompressedRingBuffer.cpp
//  OSLNative
//
//  Created by hb on 19.01.22.
//

#include "CompressedRingBuffer.h"
#include "util.h"
#include <string.h>
#include <assert.h>
#include <math.h>

/* bc this needs oversampling factor x backtracking, it is just as inefficient as the original Ringbuffer_WritePadded
 * function :( */
void CompressedRingBuffer_Read(float* dest, int n, int offset, float stride, CompressedRingBuffer* x) {
    /* Find starting point */
    assert(offset <= 0);
    assert(stride > 0);

    int ptr = x->ptr;
    float o = 0;
    float vStride;
    while (o > offset) {
        ptr -= 1;
        if (ptr < 0)
            ptr = x->n - 1;
        vStride = x->weights[ptr] /
                  stride; // example: a sample that has been written with a stride of 4 (x->weights) but is now read
                          // with a stride of 1, needs to be copied 4 times, therefore it advances the offset by 4
                          // samples. In a delay, this results in audio stretching / lower pitch.
        o -= vStride;
    }
    // Take into account that we may have to start in the middle of x->data[ptr]; or in other words, for the first
    // physical sample, we do not necessarily have to read weights[ptr] virtual samples:
    vStride = stride / x->weights[ptr];            // Note that this time it is the other way round.
    float fPtr = ptr + fabs(o - offset) * vStride; // abs(o-offset) is the number of samples we do not want to copy.
                                                   // vStride is the advancement of the fPtr when copying 1 sample.
    ptr = (int) (fPtr + 0.5f) % x->n;

    for (int i = 0; i < n; i++) {
        dest[i] = x->data[ptr];
        vStride = stride / x->weights[ptr]; // Note that this time it is the other way round.
        fPtr += vStride;
        ptr = (int) (fPtr + 0.5f) % x->n;
    }
}

void CompressedRingBuffer_Write(float* src, int n, float stride, CompressedRingBuffer* x) {
    assert(stride > 0);

    if (n > x->n) {
        src = src + (n - x->n);
        n = x->n;
    }
    int n1 = _min(x->n - x->ptr, n);
    memcpy(x->data + x->ptr, src, n1 * sizeof(float));
    for (int i = 0; i < n1; i++)
        x->weights[x->ptr + i] = stride;
    x->ptr = (x->ptr + n1) % x->n;
    if (n1 < n) {
        int n2 = n - n1;
        memcpy(x->data, src + n1, n2 * sizeof(float));
        for (int i = 0; i < n2; i++)
            x->weights[i] = stride;
        x->ptr = (x->ptr + n2) % x->n;
    }
}

void CompressedRingBuffer_Print(CompressedRingBuffer* x) {
    printv("================= \n");
    printv("CompressedRingBuffer: \n");
    for (int i = 0; i < x->n; i++) {
        printv("%d: %f (weight: %f)\n", i, x->data[i], x->weights[i]);
    }
    printv("================= \n");
}

CompressedRingBuffer* CompressedRingBuffer_New(int n) {
    CompressedRingBuffer* x = (CompressedRingBuffer*) _malloc(sizeof(CompressedRingBuffer));

    x->data = (float*) _malloc(n * sizeof(float));
    x->weights = (float*) _malloc(n * sizeof(float));
    _fZero(x->data, n);
    for (int i = 0; i < n; i++)
        x->weights[i] = 1;
    x->ptr = 0;
    x->n = n;

    return x;
}

void CompressedRingBuffer_Free(CompressedRingBuffer* x) {
    _free(x->data);
    _free(x->weights);
    _free(x);
}
