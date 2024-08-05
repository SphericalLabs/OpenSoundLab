using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using Mirror;
public class NetworkClipPlayerComplex : NetworkSyncListener
{
    private clipPlayerComplex clipPlayerComplex;

    protected virtual void Awake()
    {
        clipPlayerComplex = GetComponent<clipPlayerComplex>();
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

    [Command(requiresAuthority = false)]
    protected void CmdRequestSync()
    {
        Debug.Log($"{gameObject.name} CmdRequestSync");

        RpcUpdateLastBuffer(clipPlayerComplex.LastBuffer);
    }

    [ClientRpc]
    protected virtual void RpcUpdateLastBuffer(double _lastBuffer)
    {
        if (isClient)
        {
            Debug.Log($"{gameObject.name} old _lastBuffer: {clipPlayerComplex.LastBuffer}, new _lastBuffer {_lastBuffer}");

            clipPlayerComplex.LastBuffer = _lastBuffer;
        }
    }
    #endregion
}
