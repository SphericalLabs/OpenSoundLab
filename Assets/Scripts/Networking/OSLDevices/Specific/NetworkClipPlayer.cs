using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Runtime.InteropServices;
public class NetworkClipPlayer : NetworkSignalGenerator
{
    public bool loaded = false;
    public float[] clipSamples;
    public int clipChannels = 2;
    public int[] sampleBounds = new int[] { 0, 0 };
    public double floatingBufferCount = 0;
    public int bufferCount = 0;
    public Vector2 trackBounds = new Vector2(0, 1);

    public void UnloadClip()
    {
        loaded = false;
        toggleWaveDisplay(false);
    }

    public GCHandle m_ClipHandle;

    public void LoadSamples(float[] samples, GCHandle _cliphandle, int channels)
    {

        m_ClipHandle = _cliphandle;
        clipChannels = channels;
        clipSamples = samples;
        sampleBounds[0] = (int)((clipSamples.Length / clipChannels - 1) * (trackBounds.x));
        sampleBounds[1] = (int)((clipSamples.Length / clipChannels - 1) * (trackBounds.y));
        floatingBufferCount = bufferCount = sampleBounds[0];

        toggleWaveDisplay(true);
        DrawClipTex();
        loaded = true;
    }

    public virtual void toggleWaveDisplay(bool on)
    {
    }

    public virtual void DrawClipTex()
    {
    }
}
