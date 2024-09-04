using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkSyncListener : NetworkBehaviour
{
    private void Start()
    {
        SubscribeToNetworkEvents();
        SubscribeToJacks();
    }
    #region Mirror
    protected virtual void SubscribeToNetworkEvents()
    {
        NetworkSyncEventManager.Instance.OsSyncEvent += OnSync;
        if (isServer)
        {
            NetworkSyncEventManager.Instance.IntervalSyncEvent += OnIntervalSync;
        }
    }

    protected virtual void SubscribeToJacks()
    {
        var jacks = GetComponentsInChildren<omniJack>();
        if (jacks != null)
        {
            foreach(omniJack jack in jacks)
            {
                jack.onBeginnConnectionEvent.AddListener(OnSync);
                jack.onEndConnectionEvent.AddListener(OnSync);
            }
        }
    }

    private void OnDestroy()
    {
        UnSubscribeToNetworkEvents();
    }

    protected virtual void UnSubscribeToNetworkEvents()
    {
        NetworkSyncEventManager.Instance.OsSyncEvent -= OnSync;
        if (isServer)
        {
            NetworkSyncEventManager.Instance.IntervalSyncEvent -= OnIntervalSync;
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
