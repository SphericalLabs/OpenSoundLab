using UnityEngine;

public static class NetworkSendThrottle
{
    public const float sendRate = 36f;
    public const float sendInterval = 1f / sendRate;

    public static void Initialize(float[] lastSendTimes)
    {
        if (lastSendTimes == null) return;
        for (int i = 0; i < lastSendTimes.Length; i++)
        {
            lastSendTimes[i] = -1f;
        }
    }

    public static bool ShouldSend(float[] lastSendTimes, int index)
    {
        if (!IsValid(lastSendTimes, index)) return true;
        if (lastSendTimes[index] + sendInterval > Time.time) return false;
        lastSendTimes[index] = Time.time;
        return true;
    }

    public static void MarkSent(float[] lastSendTimes, int index)
    {
        if (!IsValid(lastSendTimes, index)) return;
        lastSendTimes[index] = Time.time;
    }

    public static void Reset(float[] lastSendTimes, int index)
    {
        if (!IsValid(lastSendTimes, index)) return;
        lastSendTimes[index] = -1f;
    }

    public static bool ShouldSend(ref float lastSendTime)
    {
        if (lastSendTime + sendInterval > Time.time) return false;
        lastSendTime = Time.time;
        return true;
    }

    public static void MarkSent(ref float lastSendTime)
    {
        lastSendTime = Time.time;
    }

    public static void Reset(ref float lastSendTime)
    {
        lastSendTime = -1f;
    }

    static bool IsValid(float[] lastSendTimes, int index)
    {
        return lastSendTimes != null && index >= 0 && index < lastSendTimes.Length;
    }
}
