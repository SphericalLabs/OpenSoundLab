using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Adrenak.UniVoice;
using System;

public class UniVoiceBusAudioInput : IAudioInput
{
    public event Action<int, float[]> OnSegmentReady;

    public int Frequency => UniVoiceBusRecorder.Instance.Frequency;

    public int ChannelCount =>
        UniVoiceBusRecorder.Instance.AudioClip == null ? 0 : UniVoiceBusRecorder.Instance.AudioClip.channels;

    public int SegmentRate => 1000 / UniVoiceBusRecorder.Instance.SampleDurationMS;

    public UniVoiceBusAudioInput(int deviceIndex = 0, int frequency = 16000, int sampleLen = 100)
    {
        UniVoiceBusRecorder.Instance.StartRecording(frequency, sampleLen);
        Debug.unityLogger.Log("UniVoiceUniMicInput start playing");
        UniVoiceBusRecorder.Instance.OnSampleReady += Bus_OnSampleReady;
    }

    void Bus_OnSampleReady(int segmentIndex, float[] samples)
    {
        OnSegmentReady?.Invoke(segmentIndex, samples);
    }

    public void Dispose()
    {
        UniVoiceBusRecorder.Instance.OnSampleReady -= Bus_OnSampleReady;
    }
}
