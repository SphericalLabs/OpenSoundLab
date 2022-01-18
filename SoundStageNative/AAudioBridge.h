
#pragma once

#ifdef __cplusplus
extern "C" {
#endif
    ///Starts an input audio stream and collects audio data from a microphone.
    SOUNDSTAGE_API void AAudioBridge_StartInput();
    ///Pauses the input audio stream witohut deleting it.
    SOUNDSTAGE_API void AAudioBridge_PauseInput();
    //Stops and destroys the input audio stream.
    SOUNDSTAGE_API void AAudioBridge_StopInput();
#ifdef __cplusplus
}
#endif
