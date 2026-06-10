// all-pass filter declaration
//
// Written by Jezar at Dreampoint, June 2000
// http://www.dreampoint.co.uk
// This code is public domain


#pragma once


#include <stddef.h>
#include <vector>

#include "tuning.h"


namespace freeverb
{


class AllPassFilter
{
public:
                    AllPassFilter(double timeInSeconds, double sampleRate);

    void            clear();
    inline float    process(float inputAudioSample);

private:
    std::vector<float> mBuffer;
    size_t mBufferIndex = 0;
};


inline float AllPassFilter::process(float inputAudioSample)
{
    //read output sample from delay line
    const auto bufferedValue = mBuffer[mBufferIndex];

    //apply feedback gain to previous sample and accumulate new input sample. kAllPassFeedback is defined in tuning.h as seems it has been figured out via extensive testing rather than math or theory based.
    mBuffer[mBufferIndex] = inputAudioSample + (bufferedValue * kAllPassFeedback);

    //advance buffer pointer and wrap around if needed.
    mBufferIndex++;
    if (mBufferIndex >= mBuffer.size())
    {
        mBufferIndex = 0;
    }

    //why do we return the DIFFERENCE between previous and current sample? This should give some insight: https://www.dsprelated.com/freebooks/pasp/Allpass_Filters.html 
    return bufferedValue - inputAudioSample;
}


}
