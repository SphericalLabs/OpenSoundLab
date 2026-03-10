using System.Collections.Generic;

using Adrenak.UniVoice;

class PeerJitterBuffer
{
    struct PendingFrame
    {
        public long arrivalMs;
        public AudioFrame frame;
    }

    readonly List<PendingFrame> pendingFrames = new List<PendingFrame>();

    readonly int startupFrames;
    readonly int maxBufferedFrames;
    readonly long reorderWindowMs;

    long lastReleasedTimestamp = long.MinValue;

    public PeerJitterBuffer(int startupFrames, int maxBufferedFrames, long reorderWindowMs)
    {
        this.startupFrames = startupFrames;
        this.maxBufferedFrames = maxBufferedFrames;
        this.reorderWindowMs = reorderWindowMs;
    }

    public void Clear()
    {
        pendingFrames.Clear();
        lastReleasedTimestamp = long.MinValue;
    }

    public void Add(AudioFrame frame, long arrivalMs)
    {
        if (frame.samples == null || frame.samples.Length == 0) return;
        if (frame.timestamp <= lastReleasedTimestamp) return;

        int insertIndex = pendingFrames.Count;

        for (int i = 0; i < pendingFrames.Count; i++)
        {
            long pendingTimestamp = pendingFrames[i].frame.timestamp;
            if (frame.timestamp == pendingTimestamp) return;
            if (frame.timestamp < pendingTimestamp)
            {
                insertIndex = i;
                break;
            }
        }

        pendingFrames.Insert(insertIndex, new PendingFrame
        {
            arrivalMs = arrivalMs,
            frame = frame
        });
    }

    public bool TryPop(long nowMs, out AudioFrame frame)
    {
        frame = default;
        dropLateFrames();
        if (pendingFrames.Count == 0) return false;

        bool primed = lastReleasedTimestamp == long.MinValue
            ? pendingFrames.Count >= startupFrames
            : pendingFrames.Count >= maxBufferedFrames;

        bool waitedLongEnough = nowMs - pendingFrames[0].arrivalMs >= reorderWindowMs;
        if (!primed && !waitedLongEnough) return false;

        frame = pendingFrames[0].frame;
        pendingFrames.RemoveAt(0);
        lastReleasedTimestamp = frame.timestamp;
        dropLateFrames();
        return true;
    }

    void dropLateFrames()
    {
        while (pendingFrames.Count > 0 && pendingFrames[0].frame.timestamp <= lastReleasedTimestamp)
        {
            pendingFrames.RemoveAt(0);
        }
    }
}
