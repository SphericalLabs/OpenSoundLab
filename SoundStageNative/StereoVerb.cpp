// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of SoundStage Lab.
//
// SoundStage Lab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// SoundStage Lab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SoundStage Lab.  If not, see <http://www.gnu.org/licenses/>.

#include "StereoVerb.h"

SOUNDSTAGE_API freeverb::ReverbModel* StereoVerb_New(int sampleRate) {
    return new freeverb::ReverbModel((double)sampleRate);
}
SOUNDSTAGE_API void StereoVerb_Free(freeverb::ReverbModel* x) { delete x; }
SOUNDSTAGE_API void StereoVerb_SetParam(int param, float value, freeverb::ReverbModel *x)
{
    switch (param) {
        case 0:
            x->setRoomSize(value);
            break;
        case 1:
            x->setDamping(value);
            break;
        case 2:
            x->setDryLevel(value);
            break;
        case 3:
            x->setWetLevel(value);
            break;
        case 4:
            x->setWidth(value);
            break;
        case 5:
            x->setFreezeMode(value);
            break;
        default:
            break;
    }
}
SOUNDSTAGE_API float StereoVerb_GetParam(int param, freeverb::ReverbModel *x)
{
    switch (param) {
        case 0:
            return x->getRoomSize();
            break;
        case 1:
            return x->getDamping();
            break;
        case 2:
            return x->getDryLevel();
            break;
        case 3:
            return x->getWetLevel();
            break;
        case 4:
            return x->getWidth();
            break;
        case 5:
            return x->getFreezeMode();
            break;
        default:
            return -1;
            break;
    }
    return 0;
}
SOUNDSTAGE_API void StereoVerb_Clear(freeverb::ReverbModel *x) { x->clear(); }
SOUNDSTAGE_API void StereoVerb_Process(float buffer[], int length, int channels, freeverb::ReverbModel *x)
{
    //This calls a modified FreeVerb function that operates on interleaved audio buffers and also considers the modulation buffers:
    x->processInterleaved(buffer, length, channels);
}
