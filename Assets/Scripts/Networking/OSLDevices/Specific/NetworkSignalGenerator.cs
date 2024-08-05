using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkSignalGenerator : NetworkBehaviour
{
    protected signalGenerator signalGenerator;
    protected virtual void Awake()
    {
        signalGenerator = GetComponent<signalGenerator>();
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
            RpcUpdatePhase(signalGenerator._phase);
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
            RpcUpdatePhase(signalGenerator._phase);
        }
    }

    [Command]
    protected virtual void CmdRequestSync()
    {
        RpcUpdatePhase(signalGenerator._phase);
    }

    [ClientRpc]
    protected virtual void RpcUpdatePhase(double phase)
    {
        if (isClient)
        {
            signalGenerator._phase = phase;
        }
    }
    #endregion
}
