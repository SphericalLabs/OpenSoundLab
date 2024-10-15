using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Adrenak.UniVoice;
using System;

public class SinusAudioInput : IAudioInput
{
    public event Action<int, float[]> OnSegmentReady;

    public int Frequency => SinusAudioSender.Instance.Frequency;

    public int ChannelCount =>
        SinusAudioSender.Instance.AudioClip == null ? 0 : SinusAudioSender.Instance.AudioClip.channels;

    public int SegmentRate => 1000 / SinusAudioSender.Instance.SampleDurationMS;

    public SinusAudioInput(int deviceIndex = 0, int frequency = 16000, int sampleLen = 100)
    {
        SinusAudioSender.Instance.StartRecording(frequency, sampleLen);
        Debug.unityLogger.Log("UniVoiceBusAudioInput start recording");
        SinusAudioSender.Instance.OnSampleReady += Bus_OnSampleReady;
    }

    void Bus_OnSampleReady(int segmentIndex, float[] samples)
    {
        OnSegmentReady?.Invoke(segmentIndex, samples);
    }

    public void Dispose()
    {
        SinusAudioSender.Instance.OnSampleReady -= Bus_OnSampleReady;
    }
}
