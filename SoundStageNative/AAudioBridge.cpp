///https://developer.android.com/ndk/guides/audio/aaudio/aaudio

///https://github.com/google/oboe

#include <aaudio/AAudio.h>
#include "main.h"
#include "AAudioBridge.h"
#include "util.h"

AAudioStream *stream = NULL;

SOUNDSTAGE_API struct AAudioBridge *AAudioBridge_StartInput()
{

    
}

SOUNDSTAGE_API AAudioBridge_StartInput(AAudioBridge *x)
{
    if(stream == NULL)
    {
        struct AAudioBridge *x = (AAudioBridge*)_malloc(sizeof(AAudioBridge));
        
        AAudioStreamBuilder *builder;
        aaudio_result_t result = AAudio_createStreamBuilder(&builder);
        
        AAudioStreamBuilder_setDeviceId(builder, deviceId);
        AAudioStreamBuilder_setDirection(builder, direction);
        AAudioStreamBuilder_setSharingMode(builder, mode);
        AAudioStreamBuilder_setSampleRate(builder, sampleRate);
        AAudioStreamBuilder_setChannelCount(builder, channelCount);
        AAudioStreamBuilder_setFormat(builder, format);
        AAudioStreamBuilder_setBufferCapacityInFrames(builder, frames);
        
        result = AAudioStreamBuilder_openStream(builder, x->stream);
        
        AAudioStreamBuilder_delete(builder);
    }
    
    aaudio_result_t result;
    result = AAudioStream_requestStart(x->stream);
}

SOUNDSTAGE_API AAudioBridge_PauseInput(AAudioBridge *x)
{
    if(stream)
    {
        aaudio_result_t result;
        result = AAudioStream_requestStop(x->stream);
    }
}
