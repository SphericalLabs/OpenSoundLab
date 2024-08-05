using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using Mirror;
public class NetworkClipPlayerComplex : NetworkSignalGenerator
{
    private clipPlayerComplex clipPlayerComplex;

    protected override void Awake()
    {
        clipPlayerComplex = GetComponent<clipPlayerComplex>();
    }
    #region Mirror

    protected override void OnSync()
    {
        if (isServer)
        {
            RpcUpdatePhase(clipPlayerComplex._phase);
            RpcUpdateLastBuffer(clipPlayerComplex.LastBuffer);
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
            RpcUpdateLastBuffer(clipPlayerComplex.LastBuffer);
        }
    }

    [Command]
    protected override void CmdRequestSync()
    {
        base.CmdRequestSync();
        RpcUpdateLastBuffer(clipPlayerComplex.LastBuffer);
    }

    [ClientRpc]
    protected virtual void RpcUpdateLastBuffer(double _lastBuffer)
    {
        if (isClient)
        {
            clipPlayerComplex.LastBuffer = _lastBuffer;
        }
    }

    #endregion
}
