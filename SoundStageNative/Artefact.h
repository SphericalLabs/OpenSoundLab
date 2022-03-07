//
//  Artefact.hpp
//  SoundStageNative
//
//  Created by hb on 22.02.22.
//
// Artefact creates all kinds of digital havoc: white noise, jitter, bit crushing, downsampling.

#ifndef Artefact_h
#define Artefact_h

#include "main.h"

#ifdef __cplusplus
extern "C" {
#endif

SOUNDSTAGE_API void Artefact_Process(float buffer[], float noiseAmount, int downsampleFactor, float jitterAmount, int bitReduction, int channels, int n);

#ifdef __cplusplus
}
#endif

#endif /* Artefact_h */
