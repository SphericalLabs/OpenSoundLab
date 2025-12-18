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

/// FrameRingBuffer is a float RingBuffer that is organized in so-called "frames". Each frame can have a distinct
/// oversampling factor.
///
//                    0                                                                                             n-1
// buffer with samples |----------------------------------------------------------------------------------------------|
// frame metadata      |--------------------|--------------------------|----------------------------------------------|
//                     head: 0               head: 13                  head: 39
//                     length: 13            length: 26                length: 46
//                     tail: 13              tail: 39                  tail: 0 (wrap-around)
//                     overs.: 1.46          oversampling: 0.34        oversampling: 1.46
///
/// FrameRingBuffer guarantees the following at any time:
///
/// 1. Write and Read operation complexity is independent from oversampling factor.
/// 2. The ptr of the ringBuffer is always in frame 0 with offset 0.
/// 3. The ringbuffer boundary is always a frame boundary; in other words: There is always a frame starting at
/// ringbuffer index 0, and there is always a frame ending at ringuffer index (n-1).
/// 4. Adjacent frames never have the same oversampling factor. Exception: The last and the first frame in the
/// ringbuffer can have the same oversampling factor (also see see 3).
/// 5. Frames are non-overlapping
/// 6. Frames cover the whole ringbuffer
///
/// The RingBuffer itself is identical to RingBuffer.cpp. The frames are stored in an additional vector of FrameHeader
/// metastructs. The overhead for storing the frames is very small.
///
/// FrameRingBuffer is not thread-safe.

#ifndef CRingBuffer_hpp
#define CRingBuffer_hpp

#include <vector>

using std::vector;

typedef struct _FrameHeader {
    int head; // inclusive
    int tail; // exclusive
    int length;
    float oversampling;
} FrameHeader;

typedef struct _FrameRingBuffer {
    float* data;
    int ptr;
    int n;
    vector<FrameHeader> headers;
} FrameRingBuffer;

/* Writing into the buffer */

/// Writes n samples from src to the FrameRingBuffer. Writes a new frame header with specified oversampling.
/// Concatenating of frame headers with same oversampling is also taken care of.
void FrameRingBuffer_Write(float* src, int n, float oversampling, FrameRingBuffer* x);
/// Fills the FrameRingBuffer with 0's.
void FrameRingBuffer_Clear(FrameRingBuffer* x);

/* Reading from the buffer */

/// Reads n samples from the FrameRingBuffer. Stride between samples is calculated based on the specified oversampling
/// as well as the oversampling in the frame headers. Returns the index of the last sample that was read. Note that this
/// index is fracitonal, in other words, it can be located between two samples.
float FrameRingBuffer_Read(float* dest, int n, int offset, float oversampling, int interpolation, FrameRingBuffer* x);
float FrameRingBuffer_Read2(float* dest, int n, int offset, float* oversampling, int singleValueOversampling,
                            int interpolation, FrameRingBuffer* x);

/* Allocating, Freeing */

/// Allocates a new FrameRingBuffer with capacity n and returns a pointer to it.
FrameRingBuffer* FrameRingBuffer_New(int n);
/// Frees all resources of a FrameRingBuffer.
void FrameRingBuffer_Free(FrameRingBuffer* x);

/* Validating, Printing */

/// Checks if the FrameRingBuffer's frames do not violate any restrictions (see comments above).
bool FrameRingBuffer_Validate(FrameRingBuffer* x);
/// Prints the frame headers of the FrameRingBuffer.
void FrameRingBuffer_Print(FrameRingBuffer* x);

#endif /* CRingBuffer_hpp */
