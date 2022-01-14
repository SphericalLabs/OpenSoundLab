/*
 Hannes Barfuss 17.12.2021
 
 These are just some wrapper functions for FreeVerb.
 
 */

#include "main.h"
#include "util.h"
#ifdef _WIN32
#include "Freeverb/freeverb/components/revmodel.hpp"
#else
#include "revmodel.hpp"
#endif

#ifdef __cplusplus
extern "C" {
#endif
///Allocates and returns a new freeverb::ReverbModel instance.
SOUNDSTAGE_API freeverb::ReverbModel* StereoVerb_New(int sampleRate);
///Releases all allocated resources.
SOUNDSTAGE_API void StereoVerb_Free(freeverb::ReverbModel* x);
///Sets a given parameter to a given value.
SOUNDSTAGE_API void StereoVerb_SetParam(int param, float value, freeverb::ReverbModel *x);
///Returns the current value of the selected parameter.
SOUNDSTAGE_API float StereoVerb_GetParam(int param, freeverb::ReverbModel *x);
///Clears all buffers of the freeverb::ReverbModel instance.
SOUNDSTAGE_API void StereoVerb_Clear(freeverb::ReverbModel *x);
///Processes 1 block of interleaved stereo audio data.
SOUNDSTAGE_API void StereoVerb_Process(float buffer[], int length, int channels, freeverb::ReverbModel *x);
#ifdef __cplusplus
}
#endif
