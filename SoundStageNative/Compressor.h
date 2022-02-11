
#ifndef Compressor_h
#define Compressor_h

#include "main.h"

struct CompressorData
{
    //public
    float* params;
    
    //internal
    float sampleRate;
    float aA;
    float aR;
    float aD; //delay smoothing coefficient
    float y1_prev[2];
    float yL_prev[2];
    int d_prev; //previous delay, for smoothing delay
    float *buf;
    int bufPtr;
    int bufLength;
    float attenuation;
    bool clipping; //Indicates if at least one sample in the last buffer exceeded 0dB
};

#ifdef __cplusplus
extern "C" {
#endif
    ///Processes one block of interleaved stereo audio data.
    SOUNDSTAGE_API void Compressor_Process(float buffer[], float sc[], int length, int channels, CompressorData* x);
    ///Returns a new compressor instance.
    SOUNDSTAGE_API struct CompressorData *Compressor_New(float sampleRate);
    ///Frees all resources of a compressor instance.
    SOUNDSTAGE_API void Compressor_Free(struct CompressorData *x);
    ///Sets the parameter to the specified value.
    SOUNDSTAGE_API void Compressor_SetParam(float value, int param, struct CompressorData *x);
    ///Returns the average attenuation applied to the most recently processed block of audio data.
    SOUNDSTAGE_API float Compressor_GetAttenuation(struct CompressorData *x);
    ///Returns true if the compressor is still approaching a lookahead time specified before (smoothing is applied to the lookahead parameter to avoid artifacts).
    SOUNDSTAGE_API bool Compressor_IsRamping(struct CompressorData *x);
    ///Returns true if at least 1 output sample of the most recently processed block of audio data exceeded 0dB.
    SOUNDSTAGE_API bool Compressor_IsClipping(struct CompressorData *x);
#ifdef __cplusplus
}
#endif

#endif /* Compressor_h */
