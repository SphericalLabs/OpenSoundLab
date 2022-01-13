//  Created by Hannes Barfuss on 28.12.2021.
//
//  This is a collection of utility functions that are often used in audio processing. Some of them are optimized with Neon Instrinsics for arm64 CPUs. Make sure you compile with Neon support if available on the target hardware, as performance gains are substantial (up to 4x faster).

#pragma once

#include <stdlib.h>
#include <stdio.h>

#if defined(ANDROID) || defined(__ANDROID__)
    #include <android/log.h>
    #define ANDDOIR_LOG_TAG "SoundStageNative"
    #define  printv(...) __android_log_print(ANDROID_LOG_INFO,ANDDOIR_LOG_TAG,__VA_ARGS__)
#else
    #define printv(...) printf(__VA_ARGS__)
#endif

#define one_minus_oneOverE 0.6321205588285576784044762298 //1-(1/e)

#ifdef __cplusplus
extern "C" {
#endif
    void *_malloc(size_t n);

    void _free(void *x);

    /* Converts millisceonds to samples */
    int _mstosmpls(float ms, float sampleRate);

    /* Converts seconds to samples */
    int _stosmpls(float s, float sampleRate);

    /* Converts one linear domain sample to logarithmic domain sample */
    float _atodb(float a);

    /* Converts one logarithmic domain sample to linear domain sample */
    float _dbtoa(float a);

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

    /* Adds corresponding elements of src1 and src2 and writes the result to dest */
    void _fAdd(float *src1, float *src2, float *dest, int n);

    /* Multiplies elements of src1 and src2 and adds the results to dest */
    void _fMultiplyAdd(float *src1, float *src2, float *dest, int n);

    float _fSumOfMags(float *src, int n);

    /* Clears the negative sign from all elements in src */
    void _fAbs(float *src, float* dest, int n);

    /* Returns the next power of 2 that is >= n. */
    int _nextPowOf2(int n);
    
    /* Returns 1 if n is a power of 2, and 0 otherwise. */
    int _isPowOf2(int n);

    /* Visual Studio does not allow variable length arrays, therefore (de-)interleaving functions are deactivated until further notice. */
#ifndef _WIN32
    /* Interleaves the src vector and writes the result to the dest vector. If src and dest point to the same memory address, an out-of-place operation with a temporary buffer is performed. If src and dest point to different memory addresses, a slightly faster in-place operation is performed. */
    void _fInterleave(const float* src, float *dest, int n, int channels);

    /* De-interleaves the src vector and writes the result to the dest vector. If src and dest point to the same memory address, an out-of-place operation with a temporary buffer is performed. If src and dest point to different memory addresses, a slightly faster in-place operation is performed. */
    void _fDeinterleave(const float* src, float *dest, int n, int channels);
#endif /* _WIN32 */
    double _wallTime(void);

    double _cpuTime(void);

    void _neonFeatures(void);

#ifdef __cplusplus
}
#endif
