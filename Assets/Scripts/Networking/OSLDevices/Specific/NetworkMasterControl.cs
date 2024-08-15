using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkMasterControl : NetworkSyncListener
{
    [SerializeField] private button startButton;
    [SerializeField] private button rewindButton;
    [SerializeField] private metronome metro;

    private void Start()
    {
        startButton.onStartGrabEvents.AddListener(OnButtonPress);
        rewindButton.onStartGrabEvents.AddListener(OnButtonPress);
        
        GetComponent<NetworkDials>().dialValues.Callback += OnBpmDialUpdated;
        
    }

    void OnBpmDialUpdated(SyncList<float>.Operation op, int index, float oldValue, float newValue)
    {
        switch (op)
        {
            case SyncList<float>.Operation.OP_ADD:
                break;
            case SyncList<float>.Operation.OP_INSERT:
                break;
            case SyncList<float>.Operation.OP_REMOVEAT:
                break;
            case SyncList<float>.Operation.OP_SET:
                // careful, this is hardwiring index 0.                
                if (index == 0) metro.readBpmDialAndBroadcast();
                break;
            case SyncList<float>.Operation.OP_CLEAR:
                break;
        }
    }

    private void OnDestroy()
    {
        startButton.onToggleChangedEvent.RemoveListener(OnButtonPress);
        rewindButton.onToggleChangedEvent.RemoveListener(OnButtonPress);
    }

    private void OnButtonPress()
    {
        NetworkSyncEventManager.Instance.UpdateSync();
    }


    #region Mirror

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isServer)
        {
            CmdRequestSync();
        }
    }
    protected override void OnSync()
    {
        if (isServer)
        {
            RpcUpdate_measurePhase(masterControl.instance.MeasurePhase);
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
            RpcUpdate_measurePhase(masterControl.instance.MeasurePhase);
        }
    }

    [Command(requiresAuthority = false)]
    protected void CmdRequestSync()
    {
        Debug.Log($"{gameObject.name} CmdRequestSync");

        RpcUpdate_measurePhase(masterControl.instance.MeasurePhase);
    }

    [ClientRpc]
    protected virtual void RpcUpdate_measurePhase(double measurePhase)
    {
        if (isClient && !isServer)
        {
            Debug.Log($"{gameObject.name} old _measurePhase: { masterControl.instance.MeasurePhase}, new _measurePhase {measurePhase}");

            masterControl.instance.MeasurePhase = measurePhase;
        }
    }
    #endregion
}
