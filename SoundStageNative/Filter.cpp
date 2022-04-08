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

#include "main.h"
#include "util.h"
#include <math.h>
#include "Filter.h"
#include <string.h>

extern "C" {

	  float ProcessSample(FilterData* fd, float sample)
	  {
      
		    float input = _clamp(sample, -1.f, 1.f) - fd->q * fd->b4; //feedback

		    float t1 = fd->b1;
		    fd->b1 = (input + fd->b0) * fd->p - fd->b1 * fd->f;
		    float t2 = fd->b2; fd->b2 = (fd->b1 + t1) * fd->p - fd->b2 * fd->f;
		    t1 = fd->b3;
		    fd->b3 = (fd->b2 + t2) * fd->p - fd->b3 * fd->f;
		    fd->b4 = (fd->b3 + t1) * fd->p - fd->b4 * fd->f;
		    fd->b4 = fd->b4 - fd->b4 * fd->b4 * fd->b4 * 0.166667f; //clipping
		    fd->b0 = input;

        fd->q = _clamp(fd->q, -1.f, 1.f); 
        fd->b0 = _clamp(fd->b0, -1.f, 1.f);
        fd->b1 = _clamp(fd->b1, -1.f, 1.f);
        fd->b2 = _clamp(fd->b2, -1.f, 1.f);
        fd->b3 = _clamp(fd->b3, -1.f, 1.f);
        fd->b4 = _clamp(fd->b4, -1.f, 1.f);
      
		    if (fd->LP) return fd->b4;
		    else return input - fd->b4;
	  }
  

    void processStereoFilter(float buffer[], int length, FilterData* mfL, FilterData* mfR, float cutoffFrequency, float lastCutoffFrequency, bool freqGen, float frequencyBuffer[], float resonance, LoggerFuncPtr log)
    {

        //resonance = _clamp(resonance, 0.f, 1.f);

        float freqDiv = 1 / 24000.f; // 24kHz, Nyquist 48kHz
        bool sameCutoff = cutoffFrequency == lastCutoffFrequency;
        float cut = cutoffFrequency;
     
        for (int i = 0; i < length; i += 2)
        {

            /*if (!sameCutoff)*/ cut = lerp(lastCutoffFrequency, cutoffFrequency, (float)i / length); // slope limiting for dial

            /*if (!sameCutoff || freqGen) {*/

                // exponential 1/Oct, freqMod taken from left channel only
                // clamp fm mod -1,1 and effective frequencies 1,260000
                frequencyBuffer[i] = frequencyBuffer[i + 1] = _clamp(261.6256f * powf(2, (_clamp(frequencyBuffer[i], -1.f, 1.f) + cut) * 10.f) * freqDiv, 0.f, 1.f);
                
                mfL->q = 1.0f - frequencyBuffer[i];
                mfL->p = frequencyBuffer[i] + 0.8f * frequencyBuffer[i] * mfL->q;
                mfL->f = mfL->p + mfL->p - 1.0f;
                mfL->q = resonance * (1.0f + 0.5f * mfL->q * (1.0f - mfL->q + 5.6f * mfL->q * mfL->q));

                mfR->q = 1.0f - frequencyBuffer[i+1];
                mfR->p = frequencyBuffer[i+1] + 0.8f * frequencyBuffer[i+1] * mfR->q;
                mfR->f = mfR->p + mfR->p - 1.0f;
                mfR->q = resonance * (1.0f + 0.5f * mfR->q * (1.0f - mfR->q + 5.6f * mfR->q * mfR->q));
            /*}*/

            buffer[i] = ProcessSample(mfL, buffer[i]);
            buffer[i + 1] = ProcessSample(mfR, buffer[i + 1]);
        }

        //char buf[20];
        //sprintf_s(buf, "mfL->q = %f", mfL->q);
        //log(0, buf);
        //sprintf_s(buf, "mfL->p = %f", mfL->p);
        //log(0, buf);
        //sprintf_s(buf, "mfL->f = %f", mfL->f);
        //log(0, buf);
        //sprintf_s(buf, "mfL->b0 = %f", mfL->b0);
        //log(0, buf);
        //sprintf_s(buf, "mfL->b1 = %f", mfL->b1);
        //log(0, buf);
        //sprintf_s(buf, "mfL->b2 = %f", mfL->b2);
        //log(0, buf);
        //sprintf_s(buf, "mfL->b3 = %f", mfL->b3);
        //log(0, buf);
        //sprintf_s(buf, "mfL->b4 = %f", mfL->b4);
        //log(0, buf);
    }

}
