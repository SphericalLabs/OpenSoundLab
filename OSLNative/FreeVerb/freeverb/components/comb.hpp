// comb filter class declaration
//
// Written by Jezar at Dreampoint, June 2000
// http://www.dreampoint.co.uk
// This code is public domain


#pragma once


#include <stddef.h>
#include <vector>


namespace freeverb
{


class CombFilter
{
public:
                    CombFilter(double timeInSeconds, double sampleRate);

    void            clear();
    inline float    process(float inputAudioSample, float feedback, float damping);

private:
    float   mFilterHistory = 0.0f;
    std::vector<float> mBuffer;
    size_t  mBufferIndex = 0;
};


inline float CombFilter::process(float inputAudioSample, float feedback, float damping)
{
    //read previous sample from delay line
    const auto output = mBuffer[mBufferIndex];

    //filter previous sample. Seems to be a simple 0 pole IIR filter of the form y0 = ax0 + (1-a)y1 (exponential moving average filter)
    mFilterHistory = (output * (1.0f - damping)) + (mFilterHistory * damping);

    //apply feedback gain to previous sample and accumulate with current sample.
    mBuffer[mBufferIndex] = inputAudioSample + (mFilterHistory * feedback);

    //advance buffer pointer and wrap around if necessary.
    mBufferIndex++;
    if (mBufferIndex >= mBuffer.size())
    {
        mBufferIndex = 0;
    }

    return output;
}


}
