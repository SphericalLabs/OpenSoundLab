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

#if _MSC_VER // this is defined when compiling with Visual Studio
#define OSL_API __declspec(dllexport) // Visual Studio needs annotating exported functions with this
#else
#define OSL_API // Android NDK does not need annotating exported functions, so define is empty
#endif

extern "C" {
    OSL_API void SetArrayToFixedValue(float buf[], int length, float value);
    OSL_API int DrumSignalGenerator(float buffer[], int length, int channels, bool signalOn, int counter);
    OSL_API void SetArrayToSingleValue(float a[], int length, float val);
    OSL_API void MultiplyArrayBySingleValue(float buffer[], int length, float val);
    OSL_API void AddArrays(float a[], float b[], int length);
    OSL_API void CopyArray(float from[], float to[], int length);
    OSL_API void DuplicateArrayAndReset(float from[], float to[], int length, float val);
    OSL_API int CountPulses(float buffer[], int length, int channels, float lastSig[]);
    OSL_API void MaracaProcessBuffer(float buffer[], int length, int channels, float amp, double& _phase, double _sampleDuration);
    OSL_API void MaracaProcessAudioBuffer(float buffer[], float controlBuffer[], int length, int channels, double& _phase, double _sampleDuration);
    OSL_API void processFader(float buffer[], int length, int channels, float bufferB[], int lengthB, bool aSig, bool bSig, bool samePercent, float lastpercent, float sliderPercent);
    OSL_API float LoganTest();
    OSL_API bool GetBinaryState(float buffer[], int length, int channels, float &lastBuf);
    OSL_API bool IsPulse(float buffer[], int length);
    OSL_API void NormalizeClip(float buffer[], int length);
    OSL_API void MicFunction(float a[], float b[], int length, float val);
    OSL_API void ColorTest(char a[]);
        
    struct NoiseProcessor;
    OSL_API NoiseProcessor* CreateNoiseProcessor(int seed);
    OSL_API void DestroyNoiseProcessor(NoiseProcessor* processor);
    OSL_API void NoiseProcessBuffer(NoiseProcessor* processor, float* buffer, int length, int channels, float sampleRatePercent, float* lastSample, int* counter, int speedFrames, bool* updated);
    OSL_API void SyncNoiseProcessor(NoiseProcessor* processor, int seed, int steps);
    OSL_API int GetCurrentSeed(NoiseProcessor* processor);
    OSL_API int GetCurrentStep(NoiseProcessor* processor);

    OSL_API void GateProcessBuffer(float buffer[], int length, int channels, bool incoming, float controlBuffer[], bool bControlSig, float amp);
    OSL_API double ClipSignalGenerator(float buffer[], float freqExpBuffer[], float freqLinBuffer[], float ampBuffer[], float seqBuffer[], int length, float lastSeqGen[2], int channels, bool freqExpGen, bool freqLinGen, bool ampGen, bool seqGen, double floatingBufferCount
		, int sampleBounds[2], float playbackSpeed, float lastPlaybackSpeed, void* clip, int clipChannels, float amplitude, float lastAmplitude, bool playdirection, bool looping, double _sampleDuration, int bufferCount, bool& active, int windowLength);
    OSL_API void ADSRSignalGenerator(float buffer[], int length, int channels, int frames[], int& frameCount, bool active, float &ADSRvolume,
		float volumes[], float startVal, int& curFrame, bool sustaining);
    OSL_API void KeyFrequencySignalGenerator(float buffer[], int length, int channels, int semitone, float keyMultConst, float& filteredVal );
    OSL_API void XylorollMergeSignalsWithOsc(float buf[], int length, float buf1[], float buf2[]);
    OSL_API void XylorollMergeSignalsWithoutOsc(float buf[], int length, float buf1[], float buf2[]);
    OSL_API void OscillatorSignalGenerator(float buffer[], int length, int channels, double& _phase, float analogWave, float frequency, float prevFrequency, float amplitude, float prevAmplitude, float& prevSyncValue,
        float frequencyExpBuffer[], float frequencyLinBuffer[], float amplitudeBuffer[], float syncBuffer[], float pwmBuffer[], bool bFreqExpGen, bool bFreqLinGen, bool bAmpGen, bool bSyncGen, bool bPwmGen, double _sampleDuration, double &dspTime);
    OSL_API void addCombFilterSignal(float inputbuffer[], float addbuffer[], int length, float delayBufferL[], float delayBufferR[], int delaylength, float gain, int& inPoint, int& outPoint);
    OSL_API void processCombFilterSignal(float buffer[], int length, float delayBufferL[], float delayBufferR[], int delaylength, float gain, int& inPoint, int& outPoint);
    OSL_API void lowpassSignal(float buffer[], int length, float& lowpassL, float& lowpassR);
    OSL_API void combineArrays(float buffer[], float bufferB[], int length, float levelA, float levelB);
    OSL_API void ProcessWaveTexture(float buffer[], int length, void* pixels, unsigned char Ra, unsigned char Ga, unsigned char Ba, unsigned char Rb, unsigned char Gb, unsigned char Bb,
		int period, int waveheight, int wavewidth, int& lastWaveH, int& curWaveW);
    float lerp(float a, float b, float f);
}
