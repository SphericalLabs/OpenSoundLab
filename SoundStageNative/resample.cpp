// Copyright 2017-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
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

#include "resample.h"
#include "stdlib.h"
#include "util.h"
#include "math.h"
#include "resample_tables.h"

#ifdef _WIN32
#define M_PI 3.14159265
#endif

#ifdef __cplusplus
extern "C" {
#endif

sincTable *table = NULL;

float wsinc_convBuffer[CONV_LENGTH];

void createResampleTable()
{
    table = (sincTable*)_malloc(sizeof(sincTable));
    
    table->val[0] = CUTOFF_FREQ_NORMALIZED;
    table->diff[TABLE_SIZE - 1] = 0; //TODO: this it out of boundary, isn't it?!?
    _fZero(table->temp, CONV_LENGTH);
    
      
    float a0 = 0.35875f, a1 = 0.48829f, a2 = 0.14128f, a3 = 0.01168f;

    for (int i=1; i<TABLE_SIZE; i++) {
        float idx = 0.5*(M_PI*i/TABLE_SIZE - M_PI);
        float windowed = a0 - a1*cosf(2*idx) + a2*cosf(4*idx) - a3*cosf(6*idx);
        table->val[i] = sinf(CUTOFF_FREQ_NORMALIZED*M_PI*(float)i/VALUES_PER_ZEROCROSSING)/(CUTOFF_FREQ_NORMALIZED*M_PI*(float)i/VALUES_PER_ZEROCROSSING) * windowed * CUTOFF_FREQ_NORMALIZED;
        table->diff[i-1] = table->val[i]-table->val[i-1];
    }
    
    /*printv("\n{");
    for(int i = 0; i < TABLE_SIZE - 1; i++)
    {
        printv("%ff, ", table->val[i]);
    }
    printv("%ff\n}", table->val[TABLE_SIZE - 1]);*/
    
    /*printv("\n{");
    for(int i = 0; i < TABLE_SIZE - 1; i++)
    {
        printv("%ff, ", table->diff[i]);
    }
    printv("%ff\n}", table->diff[TABLE_SIZE - 1]);*/
}

float wsinc_resample(float smpls[CONV_LENGTH], float ratio)
{
    float y = 0;
    float exact_idx = 0;
    int sincf_idx = 0;
    float rest_idx = 0;
    
    exact_idx = fabs(ratio)*(float)VALUES_PER_ZEROCROSSING;
    sincf_idx = roundf(exact_idx);
    rest_idx = exact_idx-sincf_idx;
    // midpoint
    y = y+smpls[ZEROCROSSINGS_PER_AXIS]*(wsinc_table[sincf_idx]+rest_idx*wsinc_diff_table[sincf_idx]);
    for(int i=1; i<=ZEROCROSSINGS_PER_AXIS; i++) {
        // negative half
        exact_idx = ((float)i+ratio)*(float)VALUES_PER_ZEROCROSSING;
        sincf_idx = roundf(exact_idx);
        rest_idx = exact_idx-sincf_idx;
        y = y+smpls[ZEROCROSSINGS_PER_AXIS-i]*(wsinc_table[sincf_idx]+rest_idx*wsinc_diff_table[sincf_idx]);
        // positive half
        exact_idx = ((float)i-ratio)*(float)VALUES_PER_ZEROCROSSING;
        sincf_idx = roundf(exact_idx);
        rest_idx = exact_idx-sincf_idx;
        y = y+smpls[ZEROCROSSINGS_PER_AXIS+i]*(wsinc_table[sincf_idx]+rest_idx*wsinc_diff_table[sincf_idx]);
    }
    return y;
}

/*SOUNDSTAGE_API void Resample(FrameRingBuffer *src, float* dest, float samplerate, int offset, int n)
{
    if(!table)
        init();
    
    int halfConvLength = CONV_LENGTH / 2; //is always odd, so will be floored; that's what we want
    
    for(int i = 0; i < n; i++) {
        FrameRingBuffer_Read(table->temp, CONV_LENGTH, offset+i-halfConvLength, 1, src); //TODO: does FrameRingBuffer also manage wraparound with the offset argument?
        float ratio = samplerate / ?; //TODO: where to get the original samplerate?
        dest[i] = ratio == 1 ? table->temp[(CONV_LENGTH/2) + 1] : ProcessSample(table->temp, -ratio);
    }
}*/

#ifdef __cplusplus
}
#endif
