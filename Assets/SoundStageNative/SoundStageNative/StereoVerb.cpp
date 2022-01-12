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
    //This calls a modified FreeVerb function that operates on interleaved audio buffers:
    x->processInterleaved(buffer, length, channels);
    
    //If instead you prefer to de-interleave the buffers first and use FreeVerb's original function, you can do so like this:
    //_fDeinterleave(buffer, buffer, length, channels);
    //x->process(buffer, buffer+length/2, buffer, buffer+length/2, length/2);
    //_fInterleave(buffer, buffer, length, channels);
}
