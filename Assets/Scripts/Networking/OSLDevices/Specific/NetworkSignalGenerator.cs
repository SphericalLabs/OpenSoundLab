using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkSignalGenerator : NetworkBehaviour
{
    protected bool hasAlreadyBeenCalledInThisBufferRun = false;

    public int index = 0;

    protected double _sampleRate;
    protected double _sampleDuration;
    public double _phase;

    public bool useNativeIfAvailable = true;

    float levelVal = 1;

    protected const int MAX_BUFFER_LENGTH = 2048; // Very important to enforce this 

    public virtual void Awake()
    {
        _phase = 0;
        _sampleRate = AudioSettings.outputSampleRate;
        _sampleDuration = 1.0 / AudioSettings.outputSampleRate;

        SubscribeToNetworkEvents();
    }

    public virtual void updateTape(string s)
    {

    }

    public virtual void trigger(int c)
    {

    }

    public virtual void trigger(int c, float f)
    {

    }

    public virtual void modBuffer(double dspTime, int channels, float[] buffer)
    {

    }

    public virtual void processBuffer(float[] buffer, double dspTime, int channels)
    {

    }

    protected bool recursionCheckPre()
    {
        if (hasAlreadyBeenCalledInThisBufferRun)
        {
            hasAlreadyBeenCalledInThisBufferRun = false;
            return false;
        }
        hasAlreadyBeenCalledInThisBufferRun = true;
        return true;
    }

    protected void recursionCheckPost()
    {
        hasAlreadyBeenCalledInThisBufferRun = false;
    }

    public virtual float[] getBuffer(double dspTime, int channels, int bufferLength, bool modFreq = false, float requestedFreq = 440f, float detuneAmount = 0)
    {
        float[] buffer = new float[bufferLength];

        for (int i = 0; i < buffer.Length; i += channels)
        {
            double sample = Mathf.Sin((float)_phase * 2 * Mathf.PI);

            float frequency = 440;
            float amplitude = 0.5f;

            _phase += frequency * _sampleDuration;

            if (_phase > 1.0) _phase -= 1.0;

            buffer[i] = (float)sample * amplitude;
            buffer[i + 1] = (float)sample * amplitude;

            dspTime += _sampleDuration;
        }

        return buffer;
    }

    #region Mirror
    protected virtual void SubscribeToNetworkEvents()
    {
        NetworkSyncEventManager.Instance.SyncEvent += OnSync;
        if (isServer)
        {
            NetworkSyncEventManager.Instance.IntervalSyncEvent += OnIntervalSync;
        }
    }

    protected virtual void OnSync()
    {
        if (isServer)
        {
            RpcUpdatePhase(_phase);
        }
        else
        {
            CmdRequestSync();
        }
    }

    protected virtual void OnIntervalSync()
    {
        if (isServer)
        {
            RpcUpdatePhase(_phase);
        }
    }

    [Command]
    protected virtual void CmdRequestSync()
    {
        RpcUpdatePhase(_phase);
    }

    [ClientRpc]
    protected virtual void RpcUpdatePhase(double phase)
    {
        if (isClient)
        {
            this._phase = phase;
        }
    }
    #endregion
}
