using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkSyncListener : NetworkBehaviour
{
    private void Start()
    {
        SubscribeToNetworkEvents();
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
        Debug.Log($"{gameObject.name} On Sync");
    }

    protected virtual void OnIntervalSync()
    {
        Debug.Log($"{gameObject.name} On Interval Sync");
    }
    #endregion
}
