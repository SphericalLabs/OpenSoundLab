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

    /* Clamps f between min and max */
    float _clamp(float f, float min, float max);

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

    /* Adds corresponding elements of src1 and src2 and writes the result to dest */
    void _fAdd(float *src1, float *src2, float *dest, int n);

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

    /* Executes a linear crossfade of signal src1 and src2 over n samples and stores the result in dest. Preserves power with totally correlated signals. */
    void _fCrossfadeLinear(float* src1, float* src2, float* dest, int n);

    /* Executes a logarithmic crossfade of signal src1 and src2 over n samples and stores the result in dest. Preserves power with totally uncorrelated signals. */
    /* If destructive is set true (or >= 1), the source arrays are used to store immediate results. You are advised to use the destructive version whenever possible, as the computation is MUCH faster. */
    void _fCrossfadeLogarithmic(float* src1, float* src2, float* dest, int destructive, int n);

    void _fLerp(float* src, float* dest, float gain1, float gain2, int n);

    void _fClamp(float *src, float min, float max, int n);

    void _fNoiseDestructive(float *buf, float amount, int n);

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
