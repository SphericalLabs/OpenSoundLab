using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkMasterControl : NetworkSyncListener
{
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
            RpcUpdateMeasurePhase(masterControl.instance.MeasurePhase);
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
            RpcUpdateMeasurePhase(masterControl.instance.MeasurePhase);
        }
    }

    [Command(requiresAuthority = false)]
    protected void CmdRequestSync()
    {
        Debug.Log($"{gameObject.name} CmdRequestSync");

        RpcUpdateMeasurePhase(masterControl.instance.MeasurePhase);
    }

    [ClientRpc]
    protected virtual void RpcUpdateMeasurePhase(double measurePhase)
    {
        if (isClient && !isServer)
        {
            Debug.Log($"{gameObject.name} old _measurePhase: { masterControl.instance.MeasurePhase}, new _measurePhase {measurePhase}");

            masterControl.instance.MeasurePhase = measurePhase;
        }
    }
    #endregion
}
