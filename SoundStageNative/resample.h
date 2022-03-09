//
//  resample.hpp
//  SoundStageNative
//
//  Created by hb on 17.02.22.
//

#ifndef resample_h
#define resample_h

#include "main.h"
#include "CRingBuffer.hpp"

#define VALUES_PER_ZEROCROSSING 4096
#define ZEROCROSSINGS_PER_AXIS 11
#define CUTOFF_FREQ_NORMALIZED 0.85
#define CONV_LENGTH 2*ZEROCROSSINGS_PER_AXIS+1
#define TABLE_SIZE (VALUES_PER_ZEROCROSSING * (ZEROCROSSINGS_PER_AXIS+1))

typedef struct _sincTable {
    float val[TABLE_SIZE];
    float diff[TABLE_SIZE];
    float temp[CONV_LENGTH];
    int ptr;
} sincTable;

#ifdef __cplusplus
extern "C" {
#endif

void createResampleTable();
/* ratio must be in range [-1...1]. */
float wsinc_resample(float smpls[CONV_LENGTH], float ratio);
//A global buffer you can use to store the input samples so you don't have to allocate extra memory for it.
extern float wsinc_convBuffer[CONV_LENGTH];

#ifdef __cplusplus
}
#endif


#endif /* resample_h */
