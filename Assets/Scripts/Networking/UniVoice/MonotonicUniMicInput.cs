using System;
using System.Diagnostics;

using Adrenak.UniMic;
using Adrenak.UniVoice;

using UnityEngine;

public class MonotonicUniMicInput : IAudioInput
{
    static readonly long originUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    static readonly long originTicks = Stopwatch.GetTimestamp();

    const string tag = "MonotonicUniMicInput";

    public event Action<AudioFrame> OnFrameReady;

    Mic.Device device;
    public Mic.Device Device
    {
        get => device;
        set
        {
            if (device == value) return;
            if (device != null) device.OnFrameCollected -= onFrameCollected;
            device = value;
            if (device != null) device.OnFrameCollected += onFrameCollected;
        }
    }

    public MonotonicUniMicInput(Mic.Device device)
    {
        Device = device;
    }

    void onFrameCollected(int frequency, int channels, float[] samples)
    {
        var frame = new AudioFrame
        {
            timestamp = nowMs(),
            frequency = frequency,
            channelCount = channels,
            samples = Adrenak.UniVoice.Utils.Bytes.FloatsToBytes(samples)
        };
        OnFrameReady?.Invoke(frame);
    }

    static long nowMs()
    {
        long elapsedTicks = Stopwatch.GetTimestamp() - originTicks;
        return originUnixMs + elapsedTicks * 1000 / Stopwatch.Frequency;
    }

    public void Dispose()
    {
        if (Device != null) Device.OnFrameCollected -= onFrameCollected;
        UnityEngine.Debug.unityLogger.Log(tag, "Disposed");
    }
}
