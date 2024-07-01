using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Adrenak.UniVoice;
using System;

public class UniVoiceBusAudioInput : IAudioInput
{
    public event Action<int, float[]> OnSegmentReady;

    public int Frequency => UniVoiceMasterBusRecorder.Instance.Frequency;

    public int ChannelCount => GetNumberOfAudioChannels();

    // this is doubled!
    int GetNumberOfAudioChannels()
    {
        AudioSpeakerMode speakerMode = AudioSettings.speakerMode;
        switch (speakerMode)
        {
            case AudioSpeakerMode.Mono:
                return 1;
            case AudioSpeakerMode.Stereo:
                return 2;
            case AudioSpeakerMode.Quad:
                return 4;
            case AudioSpeakerMode.Surround:
                return 5;
            case AudioSpeakerMode.Mode5point1:
                return 6;
            case AudioSpeakerMode.Mode7point1:
                return 8;
            case AudioSpeakerMode.Prologic:
                return 2; // Prologic usually indicates stereo but encoded with surround information
            default:
                return 2; // Default to stereo if unknown
        }
    }

    public int SegmentRate { get; private set; }

    public UniVoiceBusAudioInput(int segmentRate = 10)
    {
        SegmentRate = segmentRate;
        UniVoiceMasterBusRecorder.Instance.StartRec(SegmentRate);
        Debug.unityLogger.Log("UniVoiceBusAudioInput started recording");
        UniVoiceMasterBusRecorder.Instance.OnSegmentReady += MasterBus_OnSegmentReady;
    }

    void MasterBus_OnSegmentReady(int segmentIndex, float[] samples)
    {
        OnSegmentReady?.Invoke(segmentIndex, samples);
    }

    public void Dispose()
    {
        UniVoiceMasterBusRecorder.Instance.StopRec();
        Debug.unityLogger.Log("UniVoiceBusAudioInput stopped recording");
        UniVoiceMasterBusRecorder.Instance.OnSegmentReady -= MasterBus_OnSegmentReady;
    }
}
