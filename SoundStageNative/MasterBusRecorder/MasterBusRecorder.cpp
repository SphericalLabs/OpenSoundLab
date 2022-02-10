#include "AudioPluginUtil.h"
#include "util.h"
#include <atomic>
#include "RingBuffer.h"

///Note: The plugin only works if "Load at startup" is checked in the Import settings in Unity for the SoundStageNative library.template

///If Unity runs with 60fps and an audio buffer size of 256 with 48kHz sample rate, then the DSP process chain is called roughly 190 times per second, which means between 3 and 4 times per frame.
#define MBR_BUFFERLENGTH 16384 ///2^14, this should be enough to handle any common dsp vector size and frame rate as well as some frames without Unity consuming the buffer, which may occasionally happen.

namespace MasterBusRecorder
{
    enum Param
    {
        P_NUM
    };

    struct EffectData
    {
        struct Data
        {
            float p[P_NUM];
            std::atomic_bool recording;
            std::atomic_int newSamples;
            std::atomic<float> level_dB;
            std::atomic<float> level_lin;
            struct RingBuffer *buffer;
            int readPtr;
        };
        union
        {
            Data data;
            unsigned char pad[(sizeof(Data) + 15) & ~15]; // This entire structure must be a multiple of 16 bytes (and and instance 16 byte aligned) for PS3 SPU DMA requirements
        };
    };

    struct EffectData::Data *instance = NULL;

    int InternalRegisterEffectDefinition(UnityAudioEffectDefinition& definition)
    {
        int numparams = P_NUM;
        definition.paramdefs = new UnityAudioParameterDefinition[numparams];
        return numparams;
    }

extern "C"
{
    SOUNDSTAGE_API void StartRecording()
    {
        instance->recording.store(true);
    }

    SOUNDSTAGE_API void StopRecording()
    {
        instance->recording.store(false);
    }

    SOUNDSTAGE_API float GetLevel_Lin()
    {
        return instance->level_lin.load();
    }

    SOUNDSTAGE_API float GetLevel_dB()
    {
        return instance->level_dB.load();
    }

    ///Reads 1 sample from the record buffer. Note that this needs an atomic_load operation, which is not guaranteed to be lock-free on all systems. It is more efficient to use ReadRecordedSamples() to read ALL new samples in one go, as this also only needs 1 atomic_load operation - needs more memory, though. On systems with lock-free implementations of atomic_load, it should be fine to use this function.
    SOUNDSTAGE_API bool ReadRecordedSample(float* sample)
    {
        int n = instance->newSamples.load();
        if(n > 0)
        {
            *sample = instance->buffer->buf[instance->readPtr]; //We are bypassing the RingBuffer's API here and reading directly from its underlying buffer. This is not recommended...
            instance->newSamples.fetch_sub(1);
            instance->readPtr++;
            if (instance->readPtr >= MBR_BUFFERLENGTH)
                instance->readPtr -= MBR_BUFFERLENGTH;
            return true;
        }
        else
            return false;
    }

    ///Reads all new samples into outBuffer and returns the number of copied samples. Needs 1 atomic_load no matter of the number of samples to read.
    SOUNDSTAGE_API int ReadRecordedSamples(float* outBuffer)
    {
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

    SOUNDSTAGE_API int GetBufferPointer(void *ringbuffer, int* readOffset)
    {
        int n = instance->newSamples.load();
        
        ringbuffer = (void*)(instance->buffer);
        
        *readOffset = instance->readPtr;
        
        return n;
    }

    SOUNDSTAGE_API void AdvanceBufferPointer(int n)
    {
        instance->readPtr += n;
        if (instance->readPtr >= MBR_BUFFERLENGTH)
            instance->readPtr -= MBR_BUFFERLENGTH;
        instance->newSamples.fetch_sub(n);
    }

    SOUNDSTAGE_API void* GetRecorderInstance()
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
        state->effectdata = effectdata;
        instance = &effectdata->data;
        
        if(!atomic_is_lock_free(&effectdata->data.recording))
            printv(("Warning: MasterBusRecorder: The atomic recording flag is not lock-free on this system. Therefore, you are advised to carefully examine the effects of locks in the audio thread on this system before using the MasterBusRecorder.\n"));
        else
            printv(("MasterBusRecorder: The atomic recording flag is lock-free.\n"));
        if(!atomic_is_lock_free(&effectdata->data.newSamples))
            printv(("Warning: MasterBusRecorder: The atomic newSamples counter is not lock-free on this system. Therefore, it is highly discouraged to use ReadRecordedSample() on this system. Use ReadRecordedSamples() instead.\n"));
        else
            printv(("MasterBusRecorder: The atomic newSamples counter is lock-free.\n"));
        
        return UNITY_AUDIODSP_OK;
    }

    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK ReleaseCallback(UnityAudioEffectState* state)
    {
        EffectData* effectdata = state->GetEffectData<EffectData>();
        EffectData::Data* data = &effectdata->data;
        
        while(data->recording.load() == true)
        {
            //Have to use a spinlock here to wait until mainthead finishes recording. Ideally, StopRecording() is always called before Unity calls ReleaseCallback(). For example, you could call StopRecording in OnDestroy() routine of a MonoBehaviour.
        }
        
        if (instance == &effectdata->data)
            instance = NULL;
        RingBuffer_Free(data->buffer);
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
        
        ///Pass the audio to the next device in chain without modifying it
        _fCopy(inbuffer, outbuffer, length * outchannels);
        float average = _fAverageSumOfMags(inbuffer, length * inchannels);
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
            return UNITY_AUDIODSP_ERR_UNSUPPORTED;
        }
    }
}

