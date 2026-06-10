// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2026 OSLLv1 Sphericals OpenSoundLab
//
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
//
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
//
//
// Copyright © 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright © 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright © 2017 Apache 2.0 Google LLC SoundStage VR
//
// Licensed under the Apache License, Version 2.0 (the "License");
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#include "GateApi.h"

extern "C" {

int CountPulses(float buffer[], int length, int channels, float lastSig[]) {
    int hits = 0;
    for (int i = 0; i < length; i += channels) {
        if (buffer[i] > lastSig[1] && lastSig[1] <= lastSig[0]) {
            hits++;
        }

        lastSig[0] = lastSig[1];
        lastSig[1] = buffer[i];
    }
    return hits;
}

bool GetBinaryState(float buffer[], int length, int channels, float& lastBuf) {
    bool on = false;
    for (int i = 0; i < length; i += channels) {
        if (lastBuf == buffer[i] && buffer[i] < 0)
            on = false;
        else
            on = true;
        lastBuf = buffer[i];
    }

    return on;
}

bool IsPulse(float buffer[], int length) {
    if (buffer[0] == -1 && buffer[1] == -1) {
        for (int i = 2; i < length; ++i) {
            if (buffer[i] == -1)
                return false;
        }
        return true;
    } else
        return false;
}

void GateProcessBuffer(float buffer[], int length, int channels, bool incoming, float controlBuffer[], bool bControlSig,
                       float amp) {
    if (!incoming) {
        // float endAmp = 4 * amp - 2;//(amp - .5f) * 2;
        if (!bControlSig) // act as CV generator (but beware non-linear, assymetrical behaviour of dial.cs... 0 at 9'
                          // clock, -2/+2 at 6' clock)
        {
            float endAmp = 4 * amp - 2;
            for (int i = 0; i < length; i++)
                buffer[i] = endAmp; // endAmp;
        } else                      // act as attenverter for cv input?
        {
            for (int i = 0; i < length; i++) {
                buffer[i] = amp * 2 * (controlBuffer[i] + 1) - 1.0f;
            }
        }
    } else {
        float endAmp = amp * 2;
        if (!bControlSig) // act as attenuator/gain for audio
        {

            for (int i = 0; i < length; i++) {
                buffer[i] *= endAmp;
            }
        } else // act as a VCA
        {
            for (int i = 0; i < length; i++) {
                // buffer[i] = ((controlBuffer[i] + 1) / 2.0f) * ((buffer[i] + 1) / 2.0f) * endAmp;
                // buffer[i] = .25f * (controlBuffer[i] + 1) * (buffer[i] + 1) * endAmp;
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
                buffer[i] = (buffer[i] > 1.0f) ? 1.0f : ((buffer[i] < -1.0f) ? -1.0f : buffer[i]); //clamp function made
    of ternary operators
            }
        }
    }
    */
}
}
