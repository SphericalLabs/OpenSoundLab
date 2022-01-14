// Copyright 2017 Google LLC
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
#include <math.h>
#include "Filter.h"

extern "C" {

	float ProcessSample(FilterData* fd, float sample)
	{
		float input = sample - fd->q * fd->b4;//feedback

		float t1 = fd->b1;
		fd->b1 = (input + fd->b0) * fd->p - fd->b1 * fd->f;
		float t2 = fd->b2; fd->b2 = (fd->b1 + t1) * fd->p - fd->b2 * fd->f;
		t1 = fd->b3;
		fd->b3 = (fd->b2 + t2) * fd->p - fd->b3 * fd->f;
		fd->b4 = (fd->b3 + t1) * fd->p - fd->b4 * fd->f;
		fd->b4 = fd->b4 - fd->b4 * fd->b4 * fd->b4 * 0.166667f; //clipping
		fd->b0 = input;

		if (fd->LP) return fd->b4;
		else return input - fd->b4;
	}

    void processStereoFilter(float buffer[], int length, FilterData* mfL, FilterData* mfR, float cutoffFrequency, float frequencyBuffer[], float resonance)
    {

        resonance = clamp(resonance, 0.f, 1.f);

        for (int i = 0; i < length; i += 2)
        {
            // exponential 1/Oct
            //frequencyBuffer[i] = clamp((261.6256f * powf(2, (frequencyBuffer[i] + cutoffFrequency) * 10.f)) / 24000.f, 0.f, 1.f); // 24kHz, Nyquist 48kHz
            //frequencyBuffer[i+1] = clamp((261.6256f * powf(2, (frequencyBuffer[i+1] + cutoffFrequency) * 10.f)) / 24000.f, 0.f, 1.f);

            frequencyBuffer[i] = clamp(cutoffFrequency + frequencyBuffer[i], 0.f, 1.f); 
            frequencyBuffer[i + 1] = clamp(cutoffFrequency + frequencyBuffer[i + 1], 0.f, 1.f);

            mfL->q = 1.0f - frequencyBuffer[i];
            mfL->p = frequencyBuffer[i] + 0.8f * frequencyBuffer[i] * mfL->q;
            mfL->f = mfL->p + mfL->p - 1.0f;
            mfL->q = resonance * (1.0f + 0.5f * mfL->q * (1.0f - mfL->q + 5.6f * mfL->q * mfL->q));

            mfR->q = 1.0f - frequencyBuffer[i+1];
            mfR->p = frequencyBuffer[i+1] + 0.8f * frequencyBuffer[i+1] * mfR->q;
            mfR->f = mfR->p + mfR->p - 1.0f;
            mfR->q = resonance * (1.0f + 0.5f * mfR->q * (1.0f - mfR->q + 5.6f * mfR->q * mfR->q));

            buffer[i] = ProcessSample(mfL, buffer[i]);
            buffer[i + 1] = ProcessSample(mfR, buffer[i + 1]);
        }
    }

    float clamp(float d, float min, float max) {
        const float t = d < min ? min : d;
        return t > max ? max : t;
    }
}