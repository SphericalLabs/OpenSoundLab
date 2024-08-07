using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using Mirror;

public class NetworkNoise : NetworkSyncListener
{
    private NoiseSignalGenerator noiseSignalGenerator;
    protected virtual void Awake()
    {
        noiseSignalGenerator = GetComponent<NoiseSignalGenerator>();
        var rateDial = GetComponent<NoiseDeviceInterface>().speedDial;
        rateDial.onPercentChangedEvent.AddListener(OnDragDial);
        rateDial.onEndGrabEvents.AddListener(OnStopDragDial);
    }
    [SyncVar(hook = nameof(OnUpdateSeed))] 
    private int seed = 0; // select a specific noise pattern
    private bool initialSeedSet = false;

    #region Mirror
    public override void OnStartServer()
    {
        Debug.Log($"{gameObject.name} initial noise seed {noiseSignalGenerator.Seed}");
        seed = noiseSignalGenerator.Seed;
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isServer)
        {
            CmdRequestSync();
        }
    }
    private void OnUpdateSeed(int oldValue, int newValue)
    {
        Debug.Log($"{gameObject.name} update seed {newValue}");
        if (initialSeedSet)
        {
            noiseSignalGenerator.syncNoiseSignalGenerator(newValue, noiseSignalGenerator.NoiseStep);
        }
    }

    protected override void OnSync()
    {
        if (isServer)
        {
            RpcUpdateSteps(noiseSignalGenerator.NoiseStep);
        }
        else
        {
            CmdRequestSync();
        }
    }

    protected override void OnIntervalSync()
    {
        base.OnIntervalSync();
        if (isServer)
        {
            RpcUpdateSteps(noiseSignalGenerator.NoiseStep);
        }
    }
    [Command(requiresAuthority = false)]
    protected void CmdRequestSync()
    {
        Debug.Log($"{gameObject.name} CmdRequestSync");

        RpcUpdateSteps(noiseSignalGenerator.NoiseStep);

    }
    [ClientRpc]
    protected virtual void RpcUpdateSteps(int noiseStep)
    {
        if (isClient)
        {
            Debug.Log($"{gameObject.name} old noiseStep: {noiseSignalGenerator.NoiseStep}, new noiseStep {noiseStep}");
            noiseSignalGenerator.syncNoiseSignalGenerator(noiseSignalGenerator.Seed, noiseStep);
            initialSeedSet = true;
        }
    }

    #endregion

    public void OnDragDial()
    {
        if (Time.frameCount % 4 == 0)
        {
            OnSync();
        }
    }

    public void OnStopDragDial()
    {
        OnSync();
    }
}
