using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using Mirror;

public class NetworkNoise : NetworkSyncListener
{
    private NoiseSignalGenerator noiseSignalGenerator;
    [SyncVar]//(hook = nameof(OnUpdateSeed))] 
    private int syncSeed = 0; // select a specific noise pattern
    //private bool initialSeedSet = false;
    protected virtual void Awake()
    {
        noiseSignalGenerator = GetComponent<NoiseSignalGenerator>();
        var rateDial = GetComponent<NoiseDeviceInterface>().speedDial;
        rateDial.onPercentChangedEvent.AddListener(OnDragDial);
        rateDial.onEndGrabEvents.AddListener(OnStopDragDial);
    }


    #region Mirror
    public override void OnStartServer()
    {
        Debug.Log($"{gameObject.name} initial noise seed {noiseSignalGenerator.GetSeed()}");
        syncSeed = noiseSignalGenerator.GetSeed();
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isServer)
        {
            CmdRequestSync();
        }
    }
    /* only needed when seed changed during lifetime
    private void OnUpdateSeed(int oldValue, int newValue)
    {
        Debug.Log($"{gameObject.name} update seed {newValue}");
        if (initialSeedSet)
        {
            noiseSignalGenerator.syncNoiseSignalGenerator(newValue, noiseSignalGenerator.NoiseStep);
        }
    }*/

    protected override void OnSync()
    {
        if (isServer)
        {
            RpcUpdateSteps(noiseSignalGenerator.GetStep());
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
            RpcUpdateSteps(noiseSignalGenerator.GetStep());
        }
    }
    [Command(requiresAuthority = false)]
    protected void CmdRequestSync()
    {
        Debug.Log($"{gameObject.name} CmdRequestSync noise step {noiseSignalGenerator.GetStep()}");

        RpcUpdateSteps(noiseSignalGenerator.GetStep());

    }
    [ClientRpc]
    protected virtual void RpcUpdateSteps(int noiseStep)
    {
        if (isClient && !isServer)
        {
            Debug.Log($"{gameObject.name} old noiseStep: {noiseSignalGenerator.GetStep()}, new noiseStep {noiseStep}");
            noiseSignalGenerator.syncNoiseSignalGenerator(syncSeed, noiseStep);
            //initialSeedSet = true;
        }
    }

    #endregion

    #region onDial
    public void OnDragDial()
    {
        /*
        if (Time.frameCount % 8 == 0)
        {
            OnSync();
        }*/
    }

    public void OnStopDragDial()
    {
        OnSync();
    }
    #endregion
}
