/*
 * Copyright 2017 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

extern "C" {
	 void SetArrayToFixedValue(float buf[], int length, float value);
	 int DrumSignalGenerator(float buffer[], int length, int channels, bool signalOn, int counter);
	 void SetArrayToSingleValue(float a[], int length, float val);
	 void MultiplyArrayBySingleValue(float buffer[], int length, float val);
	 void AddArrays(float a[], float b[], int length);
	 void CopyArray(float from[], float to[], int length);
	 void DuplicateArrayAndReset(float from[], float to[], int length, float val);
	 int CountPulses(float buffer[], int length, int channels, float lastSig[]);
	 void MaracaProcessBuffer(float buffer[], int length, int channels, float amp, double& _phase, double _sampleDuration);
	 void MaracaProcessAudioBuffer(float buffer[], float controlBuffer[], int length, int channels, double& _phase, double _sampleDuration);
	 void processFader(float buffer[], int length, int channels, float bufferB[], int lengthB, bool aSig, bool bSig, bool samePercent, float lastpercent, float sliderPercent);
	 float LoganTest();
	 bool GetBinaryState(float buffer[], int length, int channels, float &lastBuf);
	 bool IsPulse(float buffer[], int length);
	 void CompressClip(float buffer[], int length);
	 void MicFunction(float a[], float b[], int length, float val);
	 void ColorTest(char a[]);
	 int NoiseProcessBuffer(float buffer[], float& sample, int length, int channels, float frequency, int counter, int speedFrames, bool& updated);
	 void GateProcessBuffer(float buffer[], int length, int channels, bool incoming, float controlBuffer[], bool bControlSig, float amp);
	 float ClipSignalGenerator(float buffer[], float speedBuffer[], float ampBuffer[], float seqBuffer[], int length, float lastSeqGen[2], int channels, bool speedGen, bool ampGen, bool seqGen, float floatingBufferCount
		, int sampleBounds[2], float playbackSpeed, void* clip, int clipChannels, float amplitude, bool playdirection, bool looping, double _sampleDuration, int bufferCount, bool& active);
	 void ADSRSignalGenerator(float buffer[], int length, int channels, int frames[], int& frameCount, bool active, float &ADSRvolume,
		float volumes[], float startVal, int& curFrame, bool sustaining);
	 void KeyFrequencySignalGenerator(float buffer[], int length, int channels, int semitone, float keyMultConst, float& filteredVal );
	 void XylorollMergeSignalsWithOsc(float buf[], int length, float buf1[], float buf2[]);
	 void XylorollMergeSignalsWithoutOsc(float buf[], int length, float buf1[], float buf2[]);
	 void OscillatorSignalGenerator(float buffer[], int length, int channels, double &_phase, float analogWave, float frequency, float amplitude, float prevAmplitude
		, float frequencyBuffer[], float amplitudeBuffer[], bool bFreqGen, bool bAmpGen, double _sampleDuration, double &dspTime);
	 void addCombFilterSignal(float inputbuffer[], float addbuffer[], int length, float delayBufferL[], float delayBufferR[], int delaylength, float gain, int& inPoint, int& outPoint);
	 void processCombFilterSignal(float buffer[], int length, float delayBufferL[], float delayBufferR[], int delaylength, float gain, int& inPoint, int& outPoint);
	 void lowpassSignal(float buffer[], int length, float& lowpassL, float& lowpassR);
	 void combineArrays(float buffer[], float bufferB[], int length, float levelA, float levelB);
	 void ProcessWaveTexture(float buffer[], int length, void* pixels, unsigned char Ra, unsigned char Ga, unsigned char Ba, unsigned char Rb, unsigned char Gb, unsigned char Bb,
		int period, int waveheight, int wavewidth, int& lastWaveH, int& curWaveW);
}