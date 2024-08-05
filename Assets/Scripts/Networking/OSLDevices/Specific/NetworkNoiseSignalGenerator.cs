using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using Mirror;

public class NetworkNoiseSignalGenerator : NetworkSignalGenerator
{
    private NoiseSignalGenerator noiseSignalGenerator;
    protected override void Awake()
    {
        noiseSignalGenerator = GetComponent<NoiseSignalGenerator>();
    }
    [SyncVar] int seed = 0; // select a specific noise pattern
    

    #region Mirror
    protected override void OnSync()
    {
        if (isServer)
        {
            RpcUpdatePhase(noiseSignalGenerator._phase);
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
    [Command]
    protected override void CmdRequestSync()
    {
        base.CmdRequestSync();
        RpcUpdateSteps(noiseSignalGenerator.NoiseStep);

    }
    [ClientRpc]
    protected virtual void RpcUpdateSteps(int noiseStep)
    {
        if (isClient)
        {
            noiseSignalGenerator.NoiseStep = noiseStep;
        }
    }

    #endregion
}
