// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 
// 
// Copyright © 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright © 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright © 2017 Apache 2.0 Google LLC SoundStage VR
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

///  Created by Hannes Barfuss on 28.12.2021.
///
///  This is a collection of utility functions that are often used in audio processing. Some of them are optimized with Neon Instrinsics for arm64 CPUs. Make sure you compile with Neon support if available on the target hardware, as performance gains are substantial (up to 4x faster).
///
///  Please follow these style conventions:
///  * all functions start with an underscore: _func(...)
///  * functions on float buffers start with _f: _fCopy(...)
///  * Document "what it does & how to use it" only in util.h
///  * Document "how it works" only in util.c

#ifndef util_h
#define util_h

#include <stdlib.h>
#include <stdio.h>

#if defined(ANDROID) || defined(__ANDROID__)
    #include <android/log.h>
    #define ANDROID_LOG_TAG "SoundStageNative"
    #define  printv(...) __android_log_print(ANDROID_LOG_INFO,ANDROID_LOG_TAG,__VA_ARGS__)
#else
    #define printv(...) printf(__VA_ARGS__)
#endif

//Windows needs explicit import of the intxx_t types
#ifdef _WIN32
#include <stdint.h>
#endif

#define one_minus_oneOverE 0.6321205588285576784044762298 //1-(1/e)

#define WAV_BIG_ENDIAN 0
#define WAV_LITTLE_ENDIAN 1

#define INTERPOLATION_NONE 1
#define INTERPOLATION_LINEAR 2
#define INTERPOLATION_WSINC 3

#ifdef __cplusplus
extern "C" {
#endif
    /* Allocate n bytes of memory. */
    void *_malloc(size_t n);

    /* Frees a previously allocated pointer. */
    void _free(void *x);

    /* Converts millisceonds to samples */
    int _mstosmpls(float ms, float sampleRate);

    /* Converts seconds to samples */
    int _stosmpls(float s, float sampleRate);

    /* Converts one linear domain sample to logarithmic domain sample */
    float _atodb(float a);

    /* Converts one logarithmic domain sample to linear domain sample */
    float _dbtoa(float a);

    /* Clamps f between min and max */
    float _clamp(float f, float min, float max);

    /* Performs linear interpolation between two samples. The output is the hypothetical sample at position [a + frac]. */
    float _interpolate_linear(float a, float b, float frac);

    /* Converts a 32bit float to a 16bit signed int */
    int16_t _float32toint16(float f);

    void _float32toint16buffer(float* src, int16_t* dest, int n);

    void _float32toint24buffer(float* src, int8_t* dest, int endianness, int n);

    /* Returns the bigger of two floats */
    float _max(float a, float b);

    /* Returns the smaller of two floats */
    float _min(float a, float b);

    /* Fills a buffer with 0's */
    void _fZero(float *dest, int n);

    /* Copies n samples of src into dest */
    void _fCopy(float *src, float *dest, int n);

    /* Multiples all elements of src with factor and writes them to dest */
    void _fScale(float *src, float *dest, float factor, int n);

    /* Multiplies all elements of src1 with the corresponding elements of src2 and writes the results to dest */
    void _fMultiply(float *src1, float *src2, float *dest, int n);

    /* Applies pow to all elements of src and writes to dest */ 
    void _fPow(float* src, float* dest, float power, int n);

    /* Adds corresponding elements of src1 and src2 and writes the result to dest */
    void _fAdd(float *src1, float *src2, float *dest, int n);

    void _fAddSingle(float* src1, float val, float* dest, int n);

    /* Multiplies elements of src1 and src2 and adds the results to dest */
    void _fMultiplyAdd(float *src1, float *src2, float *dest, int n);

    /* Calculates the sum of magnitudes of src. */
    float _fSumOfMags(float *src, int n);

    /* Calculates the average signal energy of src. */
    float _fAverageSumOfMags(float *src, int n);

    /* Clears the negative sign from all elements in src */
    void _fAbs(float *src, float* dest, int n);

    /* Returns the next power of 2 that is >= n. */
    int _nextPowOf2(int n);
    
    /* Returns 1 if n is a power of 2, and 0 otherwise. */
    int _isPowOf2(int n);

    /* Interleaves the src vector and writes the result to the dest vector. If src and dest point to the same memory address, an out-of-place operation with a temporary buffer is performed. If src and dest point to different memory addresses, a slightly faster in-place operation is performed. */
    void _fInterleave(const float* src, float *dest, int n, int channels);

    /* De-interleaves the src vector and writes the result to the dest vector. If src and dest point to the same memory address, an out-of-place operation with a temporary buffer is performed. If src and dest point to different memory addresses, a slightly faster in-place operation is performed. */
    void _fDeinterleave(const float* src, float *dest, int n, int channels);

    void _fDryWetLogarithmic(float *dryBuf, float* wetBuf, float* dest, float dry, float wet, int n);

    /* scales dryBuf by dry and wetBuf by wet, adding the result in dest. dryBuf and wetBuf are not modified. */
    void _fDryWetLinear(float *dryBuf, float* wetBuf, float* dest, float dry, float wet, int n);

    /* Executes a linear crossfade of signal src1 and src2 over n samples and stores the result in dest. Preserves power with totally correlated signals. */
    void _fCrossfadeLinear(float* src1, float* src2, float* dest, int n);

    /* Executes a logarithmic crossfade of signal src1 and src2 over n samples and stores the result in dest. Preserves power with totally uncorrelated signals. */
    /* If destructive is set true (or >= 1), lookup tables ares used for calculation and the source arrays are used to store immediate results. You are advised to use the destructive version whenever possible, as the computation is MUCH faster. */
    void _fCrossfadeLogarithmic(float* src1, float* src2, float* dest, int destructive, int n);

    /* Multiplies the src array with a linear ramp that starts at gain1 and ends at gain2.
     * If gain1 == gain2, the more efficient _fScale operation is called.
     */
    void _fLerp(float* src, float* dest, float gain1, float gain2, int n);

    /* Clamps all values in src array. */
    void _fClamp(float *src, float min, float max, int n);

    /* Fills the input buffer with white noise of the specified amplitude. */
    void _fNoise(float *buf, float amplitude, int n);

    /* Adds noise to the input signal. Overall amplitudes are preserved. 0 corresponds to no noise, 1 corresponds to "only noise". */
    void _fNoiseAdditive(float *buf, float amount, int n);

    /* Reduces the sample rate of the input signal by factor. */
    void _fDownSample(float *buf, int factor, int n);

    /* Adds jitter to the input signal. An amount of 1 corresponds to jitter of 2/samplerate. An amount of 0 corresponds to no jitter at all. */
    void _fJitter(float *buf, float amount, int n);

    /* Reduces the bit depth of the input signal. */
    void _fBitCrush(float *buf, int bitReduction, int n);

    /* Evaluates y = ab^x with slope ym. x will be clamped to [0..1]. ym < 0.5 yields an exponential curve, ym > 0.5 yields a logarithmic curve. Return val will be in range [0..1]. */
    float _expCurve(float x, float ym);

    /* Maps a value from one range to another, applying a slope between 0 and 1 (0.5 is linear or "no slope"). */
    float _map(float x, float start1, float stop1, float start2, float stop2, float slope);

#if defined(ANDROID) || defined(__ANDROID__) || defined(__APPLE__)
    double _wallTime(void);

    double _cpuTime(void);

    void _neonFeatures(void);
#endif

#ifdef __cplusplus
}
#endif

#endif /* util_h */
