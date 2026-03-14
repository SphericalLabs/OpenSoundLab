#pragma once

struct OscillatorProcessor {
    double phase;
    double sampleDuration;
    float prevSyncValue;
    float prevFrequency;
    float prevAmplitude;
    bool hadPreviousBuffer;
};

OscillatorProcessor* oscillatorCreateProcessor(float sampleRate);
void oscillatorDestroyProcessor(OscillatorProcessor* processor);
void oscillatorSetPhase(OscillatorProcessor* processor, double phase);
void oscillatorProcessBuffer(OscillatorProcessor* processor, float buffer[], int length, int channels, double& phaseMirror,
                             float analogWave, bool bLfo, float frequency, float amplitude,
                             float frequencyExpBuffer[], float frequencyLinBuffer[], float amplitudeBuffer[],
                             float syncBuffer[], float pwmBuffer[], bool bFreqExpGen, bool bFreqLinGen, bool bAmpGen,
                             bool bSyncGen, bool bPwmGen, double& dspTime);
