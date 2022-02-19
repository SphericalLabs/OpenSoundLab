/// Created on 07.02. by Hannes
/// MasterBusRecorder is a Unity Native Audio Effect that buffers its input and makes it available for any other modules/threads (for example for writing WAV files to disk).
/// It has an optional limiter feature, which can be completely bypassed.
/// It is intended to use as a singleton, e.g. only one instance should be used. In fact, if you create multiple plugin instances, only the most recent one will be active.
///
/// All functions tagged SOUNDSTAGE_API are thread-safe and can be called from any thread.
/// All functions tagged UNITY_AUDIODSP_CALLBACK are called by Unity's audio Engine and should NEVER be called by the user.
///
/// Please note that the plugin only works if "Load at startup" is checked in the Import settings in Unity for the SoundStageNative library (.so, .dll, .dylib, .a or .bundle) and that you have to set this for each platform individually.
/// For best efficiency, you should check that the target system provides lock-free implementations of std::atomic<bool>, std::atomic<int> and std::atomic<float>. This plugin's main target is the Oculus Quest 2, which provides these lock-free atomics.
/// If your target system does not provide lock-free atomics, it should in most cases still be safe to use this plugin. Performance may suffer a bit. HOWEVER, if your target system does not provide lock-free atomic types AND your audio processing engine operates interrupt-driven, you are strongly advised NOT to use this plugin, as thread-safety can not be guaranteed.

#include "AudioPluginUtil.h"
#include <atomic>
///TODO: Windows include paths are horribly broken. It works for now, but should eventually clean this up.
#ifdef _WIN32
#include "../util.h"
#include "../RingBuffer.h"
#include "../Compressor.h"
#else
#include "util.h"
#include "RingBuffer.h"
#include "Compressor.h"
#endif

///If Unity runs with 60fps and an audio buffer size of 256 with 48kHz sample rate, then the DSP process chain is called roughly 190 times per second, which means between 3 and 4 times per frame.
#define MBR_BUFFERLENGTH 480000 ///10 seconds @ 48kHz this should be enough to handle any common dsp vector size and frame rate as well as some frames without Unity consuming the buffer, which may occasionally happen.

namespace MasterBusRecorder
{
    enum Param
    {
        P_LIMIT,
        P_NUM
    };

    struct EffectData
    {
        struct Data
        {
            float p[P_NUM];
            std::atomic<bool> recording;
            std::atomic<int> newSamples;
            std::atomic<float> level_dB;
            std::atomic<float> level_lin;
            struct RingBuffer *buffer;
            struct CompressorData *limiter;
            int readPtr;
        };
        union
        {
            Data data;
            unsigned char pad[(sizeof(Data) + 15) & ~15]; // This entire structure must be a multiple of 16 bytes (and and instance 16 byte aligned) for PS3 SPU DMA requirements
        };
        //Defining constructors explicitly seems to be necessary when using std::atomic types bc the default constuctor will then be deleted. Was never a problem with Xcode or Android NDK, but would not compile without explicit constructors in Visual Studio.
        EffectData() {}
        ~EffectData() {}
    };

    struct EffectData::Data *instance = NULL;

    int InternalRegisterEffectDefinition(UnityAudioEffectDefinition& definition)
    {
        int numparams = P_NUM;
        definition.paramdefs = new UnityAudioParameterDefinition[numparams];
        AudioPluginUtil::RegisterParameter(definition, "Limiter", "", 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, P_LIMIT, "Limiter on/off.");
        return numparams;
    }

extern "C"
{
    ///Sets the recording flag. After calling this, the AudioMixerThread will start recording the incoming audio to the record buffer. The expected usage in a multi-threaded environment is:
    ///1) Call StartRecording()
    ///2) Call ReadRecordedSample(s) repeatedly to consume the available samples. It is very important that this step is well synchronized with the processing rate of the AudioMxerThread to avoid buffer overflow or underflow. Buffer overflow means that no more samples can be recorded because the buffer is completely filled with unconsumed samples. This happens if your querying rate is too low, and it will cause audible gaps in the recorded audio file. Buffer underflow means that you repeatedly query for new samples when there are none. This happens if your querying rate is too high and is a waste of CPU. As a rule of thumb, you should try to consume the samples at approximately the same rate and in the same block size as the AudioMxerThread uses.
    SOUNDSTAGE_API void MasterBusRecorder_StartRecording()
    {
        if(instance != NULL)
            instance->recording.store(true);
    }

    ///Unsets the recording flag. Note that if you call StopRecording() while the AudioMixerThread is processing an audio buffer, this audio buffer will be completely processed before the AudioMixerThread stops recording. The expected usage in a multi-threaded environment is:
    ///1) Call StopRecording()
    ///2) Either call ReadRecordedSample(s) until there are no more samples available, or call MasterBusRecorder_Clear() to clear the buffer. This is important, because if you skip this step, some samples may remain in the buffer and will be read when you start the next recording.
    ///3) Do any finalizing steps (writing file headers, writing file to disk or whatever)
    SOUNDSTAGE_API void MasterBusRecorder_StopRecording()
    {
        if(instance != NULL)
            instance->recording.store(false);
    }

    /// Clears the recording buffer. Use this if you call MasterBusRecorder_StopRecording() and do NOT want to read query the rest of the available samples.
    SOUNDSTAGE_API void MasterBusRecorder_Clear()
    {
        if(instance != NULL)
            instance->newSamples.store(0);
    }

    ///Returns the average signal energy of the most recent buffer as a linear value in range [0...1].
    SOUNDSTAGE_API float MasterBusRecorder_GetLevel_Lin()
    {
        if(instance != NULL)
            return instance->level_lin.load();
        else
            return 0;
    }

    ///Returns the average signal energy of the most recent buffer in dB in range [-inf...0].
    SOUNDSTAGE_API float MasterBusRecorder_GetLevel_dB()
    {
        if(instance != NULL)
            return instance->level_dB.load();
        else
            return -INFINITY;
    }

    ///Reads 1 sample from the record buffer. Note that this needs an atomic_load operation, which is not guaranteed to be lock-free on all systems. On systems with lock-free implementations of atomic_load, it should be fine to use this function. Otherwise, it is more efficient to use ReadRecordedSamples() to read ALL new samples in one go, as this also only needs 1 atomic_load operation - needs more memory, though.
    SOUNDSTAGE_API bool MasterBusRecorder_ReadRecordedSample(float* sample)
    {
        if(instance != NULL && instance->newSamples.load() > 0)
        {
            *sample = instance->buffer->buf[instance->readPtr]; //We are bypassing the RingBuffer's API here and reading directly from its underlying buffer. This is not recommended... otherwise, calling RingBuffer_Read for one single sample seems overkill.
            instance->newSamples.fetch_sub(1);
            instance->readPtr++;
            if (instance->readPtr >= MBR_BUFFERLENGTH)
                instance->readPtr -= MBR_BUFFERLENGTH;
            return true;
        }
        else
            return false;
    }

    ///Reads all new samples into outBuffer and returns the number of copied samples. Needs 1 atomic_load operation which is not guaranteed to be lock-free on all systems.
    SOUNDSTAGE_API int MasterBusRecorder_ReadRecordedSamples(float* outBuffer)
    {
        if(instance == NULL)
            return 0;
        
        int n = 0;
        int N = 0;
        while((n = instance->newSamples.load() > 0))
        {
            RingBuffer_Read_Absolute(outBuffer, n, instance->readPtr, instance->buffer);
            instance->newSamples.fetch_sub(n);
            ///The readPtr is neither atomic nor protected with a lock bc it is never accessed from the audio thread
            instance->readPtr += n;
            if (instance->readPtr >= MBR_BUFFERLENGTH)
                instance->readPtr -= MBR_BUFFERLENGTH;
            N += n;
        }
        return N;
    }

    ///Writes a reference to the ring buffer and the current readOffset to the given memory addresses. The return value is the maximum number of samples the caller is allowed to access, starting from readOffset. Violating this rule results in undefined behavior, as the other part of the RingBuffer is reserved for the AudioMixerThread for write access.
    ///The expected usage in a multi-threaded environment is as follows:
    ///1) Call GetBufferPointer
    ///2) Do something with some or all of the available samples in the ringbuffer
    ///3) Call AdvanceBufferPointer and pass as argument the number of samples you consumed in 2)
    ///Note that if you do very expensive processing on the samples, it may be better to copy the samples to an intermediate buffer, then call AdvanceBufferPointer, and then to the expensive processing on the intermediate buffer.
    SOUNDSTAGE_API int MasterBusRecorder_GetBufferPointer(void *ringbuffer, int* readOffset)
    {
        if(instance == NULL)
            return 0;
        
        int n = instance->newSamples.load();
        
        ringbuffer = (void*)(instance->buffer);
        
        *readOffset = instance->readPtr;
        
        return n;
    }

    ///Advances the readOffset by n samples. Think of this as marking n samples as "consumed", so they can be overwritten.
    SOUNDSTAGE_API void MasterBusRecorder_AdvanceBufferPointer(int n)
    {
        if(instance == NULL)
            return;
        
        instance->readPtr += n;
        if (instance->readPtr >= MBR_BUFFERLENGTH)
            instance->readPtr -= MBR_BUFFERLENGTH;
        instance->newSamples.fetch_sub(n);
    }

    ///Returns a pointer to the current recorder instance.
    SOUNDSTAGE_API void* MasterBusRecorder_GetRecorderInstance()
    {
        return (void*)instance;
    }
}

    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK CreateCallback(UnityAudioEffectState* state)
    {
        EffectData* effectdata = new EffectData;
        memset(effectdata, 0, sizeof(EffectData));
        AudioPluginUtil::InitParametersFromDefinitions(InternalRegisterEffectDefinition, effectdata->data.p);
        effectdata->data.recording = ATOMIC_VAR_INIT(false);
        effectdata->data.newSamples = ATOMIC_VAR_INIT(0);
        effectdata->data.level_lin = ATOMIC_VAR_INIT(0);
        effectdata->data.level_dB = ATOMIC_VAR_INIT(-INFINITY);
        effectdata->data.buffer = RingBuffer_New(MBR_BUFFERLENGTH);
        effectdata->data.readPtr = 0;
        effectdata->data.recording = false;
        
        CompressorData *limiter = Compressor_New(state->samplerate);
        Compressor_SetParam(0.5f, 0, limiter); //attack
        Compressor_SetParam(2, 1, limiter); //release
        Compressor_SetParam(0, 2, limiter); //thresh
        Compressor_SetParam(INFINITY, 3, limiter); //ratio
        Compressor_SetParam(18, 4, limiter); //knee
        Compressor_SetParam(0, 5, limiter); //makeup
        Compressor_SetParam(3, 6, limiter); //lookahead
        Compressor_SetParam(1, 7, limiter); //limit
        Compressor_SetParam(0, 8, limiter); //bypass
        effectdata->data.limiter = limiter;
        
        state->effectdata = effectdata;
        instance = &effectdata->data;
        
        if(!atomic_is_lock_free(&effectdata->data.recording))
            printv(("Warning: MasterBusRecorder: std::atomic<bool> is not lock-free on this system. Therefore, you are advised to carefully examine the effects of locks in the audio thread on this system before using the MasterBusRecorder.\n"));
        else
            printv(("MasterBusRecorder: std::atomic<bool> is lock-free.\n"));
        if(!atomic_is_lock_free(&effectdata->data.newSamples))
            printv(("Warning: MasterBusRecorder: std::atomic<int> is not lock-free on this system. Therefore, it is highly discouraged to use ReadRecordedSample() on this system. Use ReadRecordedSamples() instead.\n"));
        else
            printv(("MasterBusRecorder: std::atomic<int> is lock-free.\n"));
        if(!atomic_is_lock_free(&effectdata->data.level_dB))
            printv(("Warning: MasterBusRecorder: std::atomic<float> is not lock-free on this system. Therefore, you are advised to carefully examine the effects of locks in the audio thread on this system before using the MasterBusRecorder.\n"));
        else
            printv(("MasterBusRecorder: std::atomic<float> is lock-free.\n"));
        
        return UNITY_AUDIODSP_OK;
    }

    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK ReleaseCallback(UnityAudioEffectState* state)
    {
        EffectData* effectdata = state->GetEffectData<EffectData>();
        EffectData::Data* data = &effectdata->data;
        
        while(data->recording.load() == true)
        {
            //Have to use a spinlock here to wait until mainthread finishes recording. Ideally, StopRecording() is always called before Unity calls ReleaseCallback(). For example, you could call StopRecording in OnDestroy() routine of a MonoBehaviour.
        }
        
        if (instance == &effectdata->data)
            instance = NULL;
        RingBuffer_Free(data->buffer);
        Compressor_Free(data->limiter);
        delete effectdata;
        return UNITY_AUDIODSP_OK;
    }

    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK SetFloatParameterCallback(UnityAudioEffectState* state, int index, float value)
    {
        EffectData::Data* data = &state->GetEffectData<EffectData>()->data;
        if (index >= P_NUM)
            return UNITY_AUDIODSP_ERR_UNSUPPORTED;
        data->p[index] = value;
        return UNITY_AUDIODSP_OK;
    }

    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK GetFloatParameterCallback(UnityAudioEffectState* state, int index, float* value, char *valuestr)
    {
        EffectData::Data* data = &state->GetEffectData<EffectData>()->data;
        if (value != NULL)
            *value = data->p[index];
        if (valuestr != NULL)
            valuestr[0] = 0;
        return UNITY_AUDIODSP_OK;
    }

    int UNITY_AUDIODSP_CALLBACK GetFloatBufferCallback(UnityAudioEffectState* state, const char* name, float* buffer, int numsamples)
    {
        EffectData::Data* data = &state->GetEffectData<EffectData>()->data;
        return UNITY_AUDIODSP_OK;
    }

    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK ProcessCallback(UnityAudioEffectState* state, float* inbuffer, float* outbuffer, unsigned int length, int inchannels, int outchannels)
    {
        EffectData::Data* data = &state->GetEffectData<EffectData>()->data;
        
        ///Limit output if limiter is enabled
        if(data->p[P_LIMIT])
            Compressor_Process(inbuffer, inbuffer, (int)(length*inchannels), inchannels, data->limiter);
        
        ///Pass the audio to the next device in chain
        if(inbuffer != outbuffer)
            _fCopy(inbuffer, outbuffer, (int)(length * outchannels));
        
        ///Calculate average signal energy
        float average = _fAverageSumOfMags(inbuffer, (int)(length * inchannels));
        instance->level_lin.store(average);
        instance->level_dB.store(_atodb(average));
        
        ///Check if we should record
        if(data->recording.load() == false)
            return UNITY_AUDIODSP_OK;
        
        ///Check that we have no buffer overflow
        else if(MBR_BUFFERLENGTH - data->newSamples.load() >= length)
        {
            ///Write samples to buffer
            RingBuffer_Write(inbuffer, length*inchannels, data->buffer);
            ///Update number of available samples
            data->newSamples.fetch_add((int)(length*inchannels));
            
            return UNITY_AUDIODSP_OK;
        }
        
        else
        {
            printv("MasterBusRecorder: buffer overflow!");
            return UNITY_AUDIODSP_OK;
        }
    }
}

