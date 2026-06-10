using System;
using System.Runtime.InteropServices;

public struct LinkPhaseGeneratorState
{
    public bool linkAvailable;
    public bool linkEnabled;
    public bool isPlaying;
    public float phase;
    public float bpm;
    public int peers;
}

public static class linkPhaseGeneratorService
{
    static IntPtr link = IntPtr.Zero;
    static bool nativeAvailable = true;
    static bool linkEnabled = true;
    static float fallbackBpm = 120f;
    static bool fallbackPlaying = true;
    static int lastPeerCount = 0;

    [DllImport("OSLNative")]
    static extern IntPtr LinkPhaseGenerator_Create(double bpm);

    [DllImport("OSLNative")]
    static extern void LinkPhaseGenerator_Destroy(IntPtr x);

    [DllImport("OSLNative")]
    static extern void LinkPhaseGenerator_SetEnabled(IntPtr x, int enabled);

    [DllImport("OSLNative")]
    static extern int LinkPhaseGenerator_IsEnabled(IntPtr x);

    [DllImport("OSLNative")]
    static extern int LinkPhaseGenerator_NumPeers(IntPtr x);

    [DllImport("OSLNative")]
    static extern double LinkPhaseGenerator_GetTempo(IntPtr x);

    [DllImport("OSLNative")]
    static extern void LinkPhaseGenerator_SetTempo(IntPtr x, double bpm);

    [DllImport("OSLNative")]
    static extern int LinkPhaseGenerator_IsPlaying(IntPtr x);

    [DllImport("OSLNative")]
    static extern void LinkPhaseGenerator_SetIsPlaying(IntPtr x, int isPlaying);

    [DllImport("OSLNative")]
    static extern void LinkPhaseGenerator_SetIsPlayingQuantized(IntPtr x, int isPlaying, double quantum);

    [DllImport("OSLNative")]
    static extern void LinkPhaseGenerator_RequestBeatZero(IntPtr x, double quantum);

    [DllImport("OSLNative")]
    static extern void LinkPhaseGenerator_GetAudioState(IntPtr x, double quantum, out double phase, out double bpm,
                                                        out int isPlaying, out int peers);

    public static bool LinkAvailable { get { ensureLink(); return nativeAvailable && link != IntPtr.Zero; } }
    public static bool LinkEnabled { get { return linkEnabled && nativeAvailable && link != IntPtr.Zero; } }

    public static void SetEnabled(bool enabled, float bpm)
    {
        linkEnabled = enabled;
        fallbackBpm = bpm;

        if (!ensureLink())
            return;

        callNative(delegate { LinkPhaseGenerator_SetEnabled(link, enabled ? 1 : 0); });
    }

    public static void SetTempo(float bpm)
    {
        fallbackBpm = bpm;
        if (!LinkEnabled)
            return;

        callNative(delegate { LinkPhaseGenerator_SetTempo(link, bpm); });
    }

    public static float GetTempo()
    {
        if (!LinkEnabled)
            return fallbackBpm;

        double bpm = fallbackBpm;
        if (callNative(delegate { bpm = LinkPhaseGenerator_GetTempo(link); }) && bpm > 0.0)
            fallbackBpm = (float)bpm;

        return fallbackBpm;
    }

    public static void SetPlaying(bool playing)
    {
        fallbackPlaying = playing;
        if (!LinkEnabled)
            return;

        callNative(delegate { LinkPhaseGenerator_SetIsPlaying(link, playing ? 1 : 0); });
    }

    public static void SetPlayingQuantized(bool playing, float quantum)
    {
        fallbackPlaying = playing;
        if (!LinkEnabled)
            return;

        callNative(delegate { LinkPhaseGenerator_SetIsPlayingQuantized(link, playing ? 1 : 0, quantum); });
    }

    public static bool IsPlaying()
    {
        if (!LinkEnabled)
            return fallbackPlaying;

        int isPlaying = fallbackPlaying ? 1 : 0;
        if (callNative(delegate { isPlaying = LinkPhaseGenerator_IsPlaying(link); }))
            fallbackPlaying = isPlaying != 0;

        return fallbackPlaying;
    }

    public static int PeerCount()
    {
        if (!LinkEnabled)
            return 0;

        int peers = lastPeerCount;
        if (callNative(delegate { peers = LinkPhaseGenerator_NumPeers(link); }))
            lastPeerCount = peers;

        return lastPeerCount;
    }

    public static float PhaseAtTime(float quantum)
    {
        return CaptureAudioState(quantum).phase;
    }

    public static void RequestBeatZero(float quantum)
    {
        if (!LinkEnabled)
            return;

        callNative(delegate { LinkPhaseGenerator_RequestBeatZero(link, quantum); });
    }

    public static LinkPhaseGeneratorState CaptureAudioState(float quantum)
    {
        LinkPhaseGeneratorState state = new LinkPhaseGeneratorState();
        state.linkAvailable = nativeAvailable && link != IntPtr.Zero;
        state.linkEnabled = linkEnabled && state.linkAvailable;
        state.isPlaying = fallbackPlaying;
        state.phase = 0f;
        state.bpm = fallbackBpm;
        state.peers = lastPeerCount;

        if (!state.linkEnabled)
            return state;

        double phase = 0.0;
        double bpm = fallbackBpm;
        int isPlaying = fallbackPlaying ? 1 : 0;
        int peers = lastPeerCount;

        if (callNative(delegate { LinkPhaseGenerator_GetAudioState(link, quantum, out phase, out bpm, out isPlaying, out peers); }))
        {
            state.phase = (float)phase;
            state.bpm = (float)bpm;
            state.isPlaying = isPlaying != 0;
            state.peers = peers;
            fallbackBpm = state.bpm;
            fallbackPlaying = state.isPlaying;
            lastPeerCount = state.peers;
        }

        return state;
    }

    static bool ensureLink()
    {
        if (!nativeAvailable)
            return false;
        if (link != IntPtr.Zero)
            return true;

        return callNative(delegate
        {
            IntPtr createdLink = LinkPhaseGenerator_Create(fallbackBpm);
            if (createdLink != IntPtr.Zero)
            {
                LinkPhaseGenerator_SetEnabled(createdLink, linkEnabled ? 1 : 0);
                link = createdLink;
            }
        }) && link != IntPtr.Zero;
    }

    static bool callNative(Action action)
    {
        if (!nativeAvailable)
            return false;

        try
        {
            action();
            return true;
        }
        catch (DllNotFoundException)
        {
            nativeAvailable = false;
            link = IntPtr.Zero;
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            nativeAvailable = false;
            link = IntPtr.Zero;
            return false;
        }
    }
}
