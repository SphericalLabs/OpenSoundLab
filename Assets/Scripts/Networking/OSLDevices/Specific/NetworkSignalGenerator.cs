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
        Debug.Log("On Sync");
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
        Debug.Log("On Interval Sync");
        if (isServer)
        {
            RpcUpdatePhase(signalGenerator._phase);
        }
    }

    [Command]
    protected virtual void CmdRequestSync()
    {
        Debug.Log("CmdRequestSync");
        RpcUpdatePhase(signalGenerator._phase);
    }

    [ClientRpc]
    protected virtual void RpcUpdatePhase(double phase)
    {
        Debug.Log("RpcUpdate Phase");
        Debug.Log("New Phase: " + phase);
        if (isClient)
        {
            Debug.Log("old phase: " + signalGenerator._phase);
            signalGenerator._phase = phase;
            Debug.Log("new phase: " + signalGenerator._phase);
        }
    }
    #endregion
}
