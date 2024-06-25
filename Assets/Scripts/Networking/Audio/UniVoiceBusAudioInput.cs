using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Adrenak.UniVoice;
using System;

public class UniVoiceBusAudioInput : IAudioInput
{
    public event Action<int, float[]> OnSegmentReady;

    public int Frequency => UniVoiceMasterBusRecorder.Instance.Frequency;

    public int ChannelCount => 2;

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
