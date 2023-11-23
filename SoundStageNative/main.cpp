// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 
// 
// Copyright © 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright © 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright © 2017 Apache 2.0 Google LLC SoundStage VR
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
#include <stdlib.h>

#define PI 3.14159265

extern "C" {

    float lerp(float a, float b, float f)
    {
        return (a * (1.0f - f)) + (b * f);
    }

    void SetArrayToFixedValue(float buf[], int length, float value)
    {
        for (int i = 0; i < length; ++i) // how pre-increment? clicks?
            buf[i] = value;
    }

    void SetArrayToSingleValue(float a[], int length, float val) {
        for (int i = 0; i < length; ++i)
            a[i] = val;
    }

    void MultiplyArrayBySingleValue(float buffer[], int length, float val)
    {
        for (int i = 0; i < length; ++i) buffer[i] *= val;
    }

    void AddArrays(float a[], float b[], int length)
    {
        for (int i = 0; i < length; ++i)
        {
            a[i] += b[i];
        }
    }

    void CopyArray(float from[], float to[], int length)
    {
        for (int i = 0; i < length; ++i)
        {
            to[i] = from[i];
        }
    }

    void DuplicateArrayAndReset(float from[], float to[], int length, float val)
    {
        for (int i = 0; i < length; ++i)
        {
            to[i] = from[i] * val;
            from[i] = 0;
        }
    }

    int CountPulses(float buffer[], int length, int channels, float lastSig[])
    {
        int hits = 0;
        for (int i = 0; i < length; i += channels)
        {
            if (buffer[i] > lastSig[1] && lastSig[1] <= lastSig[0])
            {
                hits++;
            }

            lastSig[0] = lastSig[1];
            lastSig[1] = buffer[i];
        }
        return hits;
    }

    void MaracaProcessBuffer(float buffer[], int length, int channels, float amp, double& _phase, double _sampleDuration)
    {
        for (int i = 0; i < length; i += channels)
        {
            buffer[i] = buffer[i + 1] = ((amp * sin(_phase * 2 * PI)) - .5f) * 2; ;
            _phase += amp * 16 * _sampleDuration;
        }
    }

    void MaracaProcessAudioBuffer(float buffer[], float controlBuffer[], int length, int channels, double& _phase, double _sampleDuration)
    {
        for (int i = 0; i < length; i += channels)
        {
            float sample = sin(_phase * 2 * PI);
            buffer[i] = buffer[i + 1] = ((controlBuffer[i] + 1) / 2.0f) * sample;

            float endFrequency = 200.0f + ((controlBuffer[i] + 1) / 2.0f) * 300.0f;
            _phase += endFrequency * _sampleDuration;
            if (_phase > 1.0) _phase -= 1.0;
        }
    }

    void processFader(float buffer[], int length, int channels, float bufferB[], int lengthB, bool aSig, bool bSig, bool samePercent, float lastpercent, float sliderPercent)
    {
        float p = sliderPercent;
        if (aSig && bSig)
        {
            for (int i = 0; i < length; i += channels)
            {
                if (!samePercent) p = lerp(lastpercent, sliderPercent, (float)i / length); // slope limiting

                buffer[i] = buffer[i] * powf(1 - p, 2) + bufferB[i] * powf(p, 2);
                buffer[i+1] = buffer[i + 1] * powf(1 - p, 2) + bufferB[i + 1] * powf(p, 2);

            }
        }
        else
        {
            float modA = 0;
            float modB = 1;

            if (aSig)
            {

                p = 1 - p;
                modA = 1;
                modB = -1;

            }

            for (int i = 0; i < length; i += channels)
            {
                if (!samePercent) p = lerp(lastpercent, sliderPercent, (float)i / length) * modB + modA; // slope limiting
                buffer[i] *= powf(p, 2);
                buffer[i + 1] *= powf(p, 2);
                
            }
        }
    }

    

    bool GetBinaryState(float buffer[], int length, int channels, float &lastBuf)
    {
        bool on = false;
        for (int i = 0; i < length; i += channels)
        {
            if (lastBuf == buffer[i] && buffer[i] < 0) on = false;
            else on = true;
            lastBuf = buffer[i];
        }

        return on;
    }

    bool IsPulse(float buffer[], int length)
    {
        if (buffer[0] == -1 && buffer[1] == -1)
        {
            for (int i = 2; i < length; ++i)
            {
                if (buffer[i] == -1) return false;
            }
            return true;
        }
        else return false;
    }

    void NormalizeClip(float buffer[], int length)
    {
        float maxVal = 0;
        for (int i = 0; i < length; i += 2)
        {
            if (buffer[i] > maxVal) maxVal = buffer[i];
        }

        if (maxVal == 1) return;

        float mod = 1.0f / maxVal;
        for (int i = 0; i < length; ++i)
        {
            buffer[i] *= mod;
        }
    }

    void MicFunction(float a[], float b[], int length, float val)
    {
        for (int i = 0; i < length; i += 2)
        {
            a[i] = a[i + 1] = b[i] * powf(val, 2);
        }
    }

    float LoganTest()
    {
        return 1.31f;
    }

    void ColorTest(char a[])
    {
        a[5] = 128;
    }

    void GateProcessBuffer(float buffer[], int length, int channels, bool incoming, float controlBuffer[], bool bControlSig, float amp)
    {
        if (!incoming)
        {
            //float endAmp = 4 * amp - 2;//(amp - .5f) * 2;
            if (!bControlSig) // act as CV generator (but beware non-linear, assymetrical behaviour of dial.cs... 0 at 9' clock, -2/+2 at 6' clock)
            {
                float endAmp = 4 * amp - 2;
                for (int i = 0; i < length; i++) buffer[i] = endAmp;// endAmp;
            }
            else // act as attenverter for cv input? 
            {
                for (int i = 0; i < length; i++)
                {
                    buffer[i] = amp * 2 * (controlBuffer[i] + 1) - 1.0f;
                }
            }
        }
        else
        {
            float endAmp = amp * 2;
            if (!bControlSig) // act as attenuator/gain for audio 
            {

                for (int i = 0; i < length; i++)
                {
                    buffer[i] *= endAmp;
                }
            }
            else // act as a VCA
            {
                for (int i = 0; i < length; i++)
                {
                    //buffer[i] = ((controlBuffer[i] + 1) / 2.0f) * ((buffer[i] + 1) / 2.0f) * endAmp;
                    //buffer[i] = .25f * (controlBuffer[i] + 1) * (buffer[i] + 1) * endAmp;
                    buffer[i] = .5f * (controlBuffer[i] + 1) * (buffer[i] + 1) * endAmp - 1.0f;

                }

            }
        }
        /*
        if (incoming)
        {
            if (!bControlSig)
            {
                float endAmp = amp * 2;
                for (int i = 0; i < length; i += channels)
                {
                    buffer[i] *= endAmp;
                    buffer[i + 1] *= endAmp;
                }
            }
            else
            {
                float endAmp;
                for (int i = 0; i < length; i += channels)
                {
                    endAmp = amp * 2 * ((controlBuffer[i] + 1) / 2.0f);
                    buffer[i] *= endAmp;
                    buffer[i + 1] *= endAmp;
                }

            }
        }
        else
        {
            if (!bControlSig)
            {
                float endAmp = (amp - .5f) * 2;
                for (int i = 0; i < length; i++)buffer[i] = endAmp;
            }
            else
            {
                float endAmp = (amp - .5f) * 2;
                for (int i = 0; i < length; i++)
                {
                    buffer[i] = endAmp + controlBuffer[i];
                    buffer[i] = (buffer[i] > 1.0f) ? 1.0f : ((buffer[i] < -1.0f) ? -1.0f : buffer[i]); //clamp function made of ternary operators
                }
            }
        }
        */
    }

    int NoiseProcessBuffer(float buffer[], float& sample, int length, int channels, float frequency, int counter, int speedFrames, bool& updated) {

        if (frequency > .95f)
        {
            updated = true;
            for (int i = 0; i < length; i += channels)
            {
                sample = buffer[i] = buffer[i + 1] = -1 + 2 * ((float)rand()) / RAND_MAX;
            }
        }

        else
        {
            for (int i = 0; i < length; i += channels)
            {
                counter++;
                if (counter > speedFrames)
                {
                    updated = true;
                    counter = 0;
                    sample = -1 + 2 * ((float)rand()) / RAND_MAX;
                }
                buffer[i] = buffer[i + 1] = sample;
            }
        }
        return counter;
    }

    int DrumSignalGenerator(float buffer[], int length, int channels, bool signalOn, int counter)
    {
        float val = signalOn ? 1.f : -1.f;

        int endSignal = length;

        if (signalOn)
            endSignal = counter;

        for (int i = 0; i < length; i += channels)
        {
            val = (signalOn && (i < endSignal)) ? 1.f : -1.f;
            buffer[i] = buffer[i + 1] = val;
        }
        counter -= length;

        return counter;

    }

    float getADSR(int curFrame, float startVal, int frameCount, int frames[], float volumes[])
    {
        switch (curFrame)
        {
        case 0:
            return startVal + (volumes[0] - startVal) * frameCount / frames[0];
        case 1:
            return volumes[0] + (volumes[1] - volumes[0]) * (float)frameCount / (float)frames[1];
        case 2:
            return volumes[1];
        case 3:
            return volumes[1] * (1.f - (float)frameCount / (float)frames[3]);
        case 4:
            return 0;
        default:
            break;
        }
        return 0;
    }



    void ADSRSignalGenerator(float buffer[], int length, int channels, int frames[], int& frameCount, bool active, float &ADSRvolume,
        float volumes[], float startVal, int& curFrame, bool sustaining)
    {
        if (!active)
        {
            for (int i = 0; i < length; i += channels)
            {
                buffer[i] = buffer[i + 1] = 0.f;
            }
            return;
        }

        for (int i = 0; i < length; i += channels)
        {
            buffer[i + 1] = buffer[i] = ADSRvolume = lerp(getADSR(curFrame, startVal, frameCount, frames, volumes), ADSRvolume, .98f);

            if (curFrame != 2) frameCount++;
            else if (curFrame == 2 && !sustaining) frameCount++;

            if (curFrame < 4)
            {
                if (frameCount >= frames[curFrame])
                {
                    curFrame++;
                    frameCount = 0;
                }
            }
        }
    }

    void KeyFrequencySignalGenerator(float buffer[], int length, int channels, int semitone, float keyMultConst, float& filteredVal)
    {
        float val = (float)semitone / 12.f * 0.1f;
        for (int i = 0; i < length; i += channels)
        {
            buffer[i] = buffer[i + 1] = filteredVal = lerp(val, filteredVal, .9f); // lerp as eased follower, downscale 1V/Oct to 0.1V/Oct
        }
    }

    double ClipSignalGenerator(float buffer[], float freqExpBuffer[], float freqLinBuffer[], float ampBuffer[], float seqBuffer[], int length, float lastSeqGen[2], int channels, bool freqExpGen, bool freqLinGen, bool ampGen, bool seqGen, double floatingBufferCount
      , int sampleBounds[2], float playbackSpeed, float lastPlayBackSpeed, void* clip, int clipChannels, float amplitude, float lastAmplitude, bool playdirection, bool looping, double _sampleDuration, int bufferCount, bool& active, int windowLength = 0)
    {
      // clip not yet or not available anymore, but wouldn't check for segmentation fault due to outdated pointer
      if (!clip) {
        return floatingBufferCount;
      }

      float* clipdata = reinterpret_cast<float*>(clip);
      float sampleBoundsCenter = (sampleBounds[0] + sampleBounds[1]) * 0.5f;

      for (int i = 0; i < length; i += channels)
      {

        float endAmplitude = amplitude;
        if (lastAmplitude != amplitude) endAmplitude = lerp(lastAmplitude, amplitude, (float)i / length); // slope limiting
        if (ampGen) endAmplitude = endAmplitude * ampBuffer[i]; // -1,1, allows for ring modulation

        float endPlaybackSpeed = playbackSpeed;
        if (lastPlayBackSpeed != playbackSpeed) endPlaybackSpeed = lerp(lastPlayBackSpeed, playbackSpeed, (float)i / length); // slope limiting

        if (active) {
          if (freqExpGen) floatingBufferCount += endPlaybackSpeed * pow(2, _clamp(freqExpBuffer[i], -1.f, 1.f) * 10.f); // exp fm, upscale 0.1V/Oct to 1V/Oct
          else floatingBufferCount += endPlaybackSpeed;
          if (freqLinGen) floatingBufferCount += freqLinBuffer[i] * 20.f; // lin fm 
        }

        bool endOfSample = false;
        if (floatingBufferCount > sampleBounds[1])
        {
          endOfSample = true;
          floatingBufferCount = fmod(floatingBufferCount - sampleBounds[0], sampleBounds[1] - sampleBounds[0]) + sampleBounds[0] + 1; // wrap over playhead offset
        }
        else if (floatingBufferCount < sampleBounds[0] + 1)
        {
          endOfSample = true;
          floatingBufferCount = sampleBounds[1] - fmod(floatingBufferCount - sampleBounds[0], sampleBounds[1] - sampleBounds[0]); // wrap over playhead offset
        }

        if (endOfSample)
        {
          if (!looping) active = false;
        }

        if (seqGen)
        {
          if (seqBuffer[i] > 0.f && lastSeqGen[0] <= 0.f)
          {
            if (playbackSpeed >= 0) floatingBufferCount = bufferCount = sampleBounds[0] + 1;
            else floatingBufferCount = bufferCount = sampleBounds[1];
            active = true;
          }
          lastSeqGen[0] = seqBuffer[i];
        }

        float windowing = 1.f;
        if (windowLength != 0) {
          if (floatingBufferCount < sampleBounds[0] + windowLength && floatingBufferCount <= sampleBoundsCenter) {
            windowing = _map(floatingBufferCount, sampleBounds[0], sampleBounds[0] + windowLength, 0.f, 1.f, 0.5f);
          }
          else if (floatingBufferCount > sampleBounds[1] - windowLength) {
            windowing = _map(floatingBufferCount, sampleBounds[1] - windowLength, sampleBounds[1], 1.f, 0.f, 0.5f);
          }
          else {
            windowing = 1.f;
          }
        }

        if (active)
        {
          // linear interpolation
          buffer[i] =
            lerp(
              clipdata[(int)floor(floatingBufferCount) * clipChannels],
              clipdata[(int)ceil(floatingBufferCount) * clipChannels],
              fmod(floatingBufferCount, floor(floatingBufferCount)))
            * endAmplitude * windowing; ;

          if (clipChannels == 2) {
            buffer[i + 1] =
              lerp(
                clipdata[(int)floor(floatingBufferCount) * clipChannels + 1],
                clipdata[(int)ceil(floatingBufferCount) * clipChannels + 1],
                fmod(floatingBufferCount, floor(floatingBufferCount)))
              * endAmplitude * windowing;
          }
          else {
            buffer[i + 1] = buffer[i];
          }

        }
      }
      return floatingBufferCount;
    }


    void XylorollMergeSignalsWithOsc(float buf[], int length, float buf1[], float buf2[])
    {
        for (int i = 0; i < length; ++i)
        {
            buf[i] += (buf1[i] + buf2[i]) * .3f;

        }
    }

    void XylorollMergeSignalsWithoutOsc(float buf[], int length, float buf1[], float buf2[])
    {
        for (int i = 0; i < length; ++i)
        {
            buf[i] += buf1[i] * ((buf2[i] + 1) / 2.f);
        }
    }

    void OscillatorSignalGenerator(float buffer[], int length, int channels, double& _phase, float analogWave, float frequency, float prevFrequency, float amplitude, float prevAmplitude, float& prevSyncValue,
        float frequencyExpBuffer[], float frequencyLinBuffer[], float amplitudeBuffer[], float syncBuffer[], float pwmBuffer[], bool bFreqExpGen, bool bFreqLinGen, bool bAmpGen, bool bSyncGen, bool bPwmGen, double _sampleDuration, double &dspTime)
    {
        int waveMode = (int)roundf(analogWave * 3);

        for (int i = 0; i < length; i += channels)
        {

            // manage phase reset/sync
            if (bSyncGen) {
                if (syncBuffer[i] > 0.f && prevSyncValue <= 0.f)
                    _phase = 0.;

                prevSyncValue = syncBuffer[i];
            }

            if (waveMode == 0) { // sine
              buffer[i] = sin(_phase * 2 * PI);
            }
            else if (waveMode == 1) { // square
              if (bPwmGen) {
                buffer[i] = _phase >= (pwmBuffer[i] + 1) / 2.f ? 1.f : -1.f; // expects value range -1,1f
              }
              else
              {
                buffer[i] = _phase >= 0.5f ? 1.f : -1.f;
              }
            }
            else if (waveMode == 2) { // saw
              buffer[i] = _phase * 2 - 1;
            }
            else { // tri
              if (_phase <= 0.5f) {
                buffer[i] = _phase * 2;
              }
              else if (_phase > 0.5f)
              {
                buffer[i] = 1 - (_phase - 0.5f) * 2;
              }
              buffer[i] = buffer[i] * 2 - 1; // [0,1]->[-1,1]
            }

            //frequency compute
            float endFrequency = frequency;
            if (prevFrequency != frequency) endFrequency = lerp(prevFrequency, frequency, (float)i / length); // slope limiting

            //amp compute
            float endAmplitude = amplitude;
            if (prevAmplitude != amplitude) endAmplitude = lerp(prevAmplitude, amplitude, (float)i / length); // slope limiting

            //calc control inputs
            if (bFreqExpGen)
            {
                endFrequency = endFrequency * powf(2, _clamp(frequencyExpBuffer[i], -1.f, 1.f) * 10.f); // convert 0.1V/Oct to 1V/Oct; this has to be clamped, could crash the system otherwise, think 2^320
            }
            if (bFreqLinGen) {
                endFrequency = endFrequency + frequencyLinBuffer[i] * 8000.f; // add lin fm, thru zero, 1V / 100Hz
            }
            if (bAmpGen)
            {
                endAmplitude = endAmplitude * amplitudeBuffer[i]; // expects 0,1 for fading, but allows for negative inputs, will invert phase then
            }
            
            //update phase for next sample
            _phase += _clamp(endFrequency, -24000.f, 24000.f) * _sampleDuration; // clamp to +/- 24kHz

            // wrap around, also for negative thru-zero phases and frequencies higher than nyquist
            if (_phase >= 0.0) { 
              _phase = fmod(_phase, 1);
            }
            else {
              _phase = 1.0 - fmod(_phase, 1);
            }

            //final buffer
            buffer[i] = buffer[i + 1] = buffer[i] * endAmplitude;

            //dsptime update
            dspTime += _sampleDuration;
        }
    }

    ///*
    //https://dsp.stackexchange.com/a/36778
    //returns a float array with two indexes representing the volumes of the left (index 0) and right (index 1) channels
    //*/

    //float xFade(float t, int index) {
    //    
    //    if (index == 0) {
    //        return sqrt(t);
    //    } else if (index == 1) {
    //        return sqrt(1.f-t);
    //    }
    //}

    void addCombFilterSignal(float inputbuffer[], float addbuffer[], int length, float delayBufferL[], float delayBufferR[], int delaylength, float gain, int& inPoint, int& outPoint)
    {
        float inputL = 0;
        float inputR = 0;

        for (int i = 0; i < length; i += 2)
        {
            inputL = inputbuffer[i];
            inputR = inputbuffer[i + 1];

            delayBufferL[inPoint] = inputbuffer[i] + delayBufferL[outPoint] * gain;
            delayBufferR[inPoint] = inputbuffer[i + 1] + delayBufferR[outPoint] * gain;

            inPoint++;
            if (inPoint == delaylength) inPoint = 0;

            addbuffer[i] += delayBufferL[outPoint];
            addbuffer[i + 1] += delayBufferR[outPoint];

            outPoint++;
            if (outPoint == delaylength) outPoint = 0;
        }
    }

    void processCombFilterSignal(float buffer[], int length, float delayBufferL[], float delayBufferR[], int delaylength, float gain, int& inPoint, int& outPoint)
    {
        float inputL = 0;
        float inputR = 0;

        for (int i = 0; i < length; i += 2)
        {
            delayBufferL[inPoint] = inputL = buffer[i] + delayBufferL[outPoint] * gain;
            delayBufferR[inPoint] = inputR = buffer[i + 1] + delayBufferR[outPoint] * gain;

            inPoint++;
            if (inPoint == delaylength)inPoint = 0;

            buffer[i] = delayBufferL[outPoint] - gain* inputL;
            buffer[i + 1] = delayBufferR[outPoint] - gain * inputR;

            outPoint++;
            if (outPoint == delaylength) outPoint = 0;
        }
    }

    void lowpassSignal(float buffer[], int length, float& lowpassL, float& lowpassR)
    {
        for (int i = 0; i < length; i += 2)
        {
            buffer[i] = lowpassL = 0.7f * lowpassL + 0.3f * buffer[i];
            buffer[i + 1] = lowpassR = 0.7f * lowpassR + 0.3f * buffer[i + 1];
        }
    }

    void combineArrays(float buffer[], float bufferB[], int length, float levelA, float levelB)
    {
        for (int i = 0; i < length; ++i)
        {
            buffer[i] = buffer[i] * levelA + bufferB[i] * levelB;
        }
    }


    void ProcessWaveTexture(float buffer[], int length, void* pixels, unsigned char Ra, unsigned char Ga, unsigned char Ba, unsigned char Rb, unsigned char Gb, unsigned char Bb,
        int period, int waveheight, int wavewidth, int& lastWaveH, int& curWaveW)
    {
        unsigned char* data = reinterpret_cast<unsigned char*>(pixels);

        for (int i = 0; i < length / period; ++i)
        {
            float temp = (buffer[i * period] + 1.0f) *.5f;
            temp = (temp > 1.0f) ? 1.0f : ((temp < 0.0f) ? 0.0f : temp);
            int curH = (int)((waveheight - 1) * temp);
            for (int i2 = 0; i2 < waveheight; i2++)
            {
                unsigned char* pixel = data + 4 * (i2 * wavewidth + curWaveW);
                if (lastWaveH >= i2 && i2 >= curH)
                {
                    pixel[0] = Ra;
                    pixel[1] = Ga;
                    pixel[2] = Ba;
                    pixel[3] = 255;
                }
                else if (lastWaveH <= i2 && i2 <= curH) {
                    pixel[0] = Ra;
                    pixel[1] = Ga;
                    pixel[2] = Ba;
                    pixel[3] = 255;
                }
                else
                {
                    pixel[0] = Rb;
                    pixel[1] = Gb;
                    pixel[2] = Bb;
                    pixel[3] = 255;
                }
            }

            lastWaveH = curH;
            curWaveW = (curWaveW + 1) % wavewidth;
        }
    }

}
