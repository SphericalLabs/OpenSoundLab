using System;
using System.Collections.Generic;
using System.Diagnostics;

using Adrenak.UniVoice;

public class JitterBufferedAudioClient : IAudioClient<int>
{
    static readonly long originUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    static readonly long originTicks = Stopwatch.GetTimestamp();

    readonly IAudioClient<int> innerClient;
    readonly Dictionary<int, PeerJitterBuffer> peerBuffers = new Dictionary<int, PeerJitterBuffer>();
    readonly int startupFrames;
    readonly int maxBufferedFrames;
    readonly long reorderWindowMs;

    public int ID => innerClient.ID;
    public List<int> PeerIDs => innerClient.PeerIDs;
    public VoiceSettings YourVoiceSettings => innerClient.YourVoiceSettings;

    public event Action<int, List<int>> OnJoined;
    public event Action OnLeft;
    public event Action<int> OnPeerJoined;
    public event Action<int> OnPeerLeft;
    public event Action<int, AudioFrame> OnReceivedPeerAudioFrame;

    public JitterBufferedAudioClient(
        IAudioClient<int> innerClient,
        int startupFrames = 2,
        int maxBufferedFrames = 4,
        long reorderWindowMs = 80
    )
    {
        this.innerClient = innerClient;
        this.startupFrames = startupFrames;
        this.maxBufferedFrames = maxBufferedFrames;
        this.reorderWindowMs = reorderWindowMs;

        this.innerClient.OnJoined += onJoined;
        this.innerClient.OnLeft += onLeft;
        this.innerClient.OnPeerJoined += onPeerJoined;
        this.innerClient.OnPeerLeft += onPeerLeft;
        this.innerClient.OnReceivedPeerAudioFrame += onReceivedPeerAudioFrame;
    }

    void onJoined(int id, List<int> peerIds)
    {
        peerBuffers.Clear();
        OnJoined?.Invoke(id, peerIds);
    }

    void onLeft()
    {
        peerBuffers.Clear();
        OnLeft?.Invoke();
    }

    void onPeerJoined(int id)
    {
        if (!peerBuffers.ContainsKey(id))
        {
            peerBuffers.Add(id, new PeerJitterBuffer(startupFrames, maxBufferedFrames, reorderWindowMs));
        }
        OnPeerJoined?.Invoke(id);
    }

    void onPeerLeft(int id)
    {
        if (peerBuffers.TryGetValue(id, out var buffer))
        {
            buffer.Clear();
            peerBuffers.Remove(id);
        }
        OnPeerLeft?.Invoke(id);
    }

    void onReceivedPeerAudioFrame(int id, AudioFrame frame)
    {
        if (!peerBuffers.TryGetValue(id, out var buffer))
        {
            buffer = new PeerJitterBuffer(startupFrames, maxBufferedFrames, reorderWindowMs);
            peerBuffers.Add(id, buffer);
        }

        buffer.Add(frame, nowMs());
    }

    public void Tick()
    {
        long currentMs = nowMs();

        foreach (var kvp in peerBuffers)
        {
            while (kvp.Value.TryPop(currentMs, out var frame))
            {
                OnReceivedPeerAudioFrame?.Invoke(kvp.Key, frame);
            }
        }
    }

    static long nowMs()
    {
        long elapsedTicks = Stopwatch.GetTimestamp() - originTicks;
        return originUnixMs + elapsedTicks * 1000 / Stopwatch.Frequency;
    }

    public void SendAudioFrame(AudioFrame frame)
    {
        innerClient.SendAudioFrame(frame);
    }

    public void SubmitVoiceSettings()
    {
        innerClient.SubmitVoiceSettings();
    }

    public void UpdateVoiceSettings(Action<VoiceSettings> modification)
    {
        innerClient.UpdateVoiceSettings(modification);
    }

    public void Dispose()
    {
        peerBuffers.Clear();
        innerClient.Dispose();
    }
}
