using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using Mirror;
public class NetworkClipPlayer : NetworkSyncListener
{
    private clipPlayer clipPlayer;

    protected virtual void Awake()
    {
        clipPlayer = GetComponent<clipPlayer>();
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
            RpcUpdateLastBuffer(clipPlayer.LastBuffer);
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
            RpcUpdateLastBuffer(clipPlayer.LastBuffer);
        }
    }

    [Command(requiresAuthority = false)]
    protected void CmdRequestSync()
    {
        Debug.Log($"{gameObject.name} CmdRequestSync");

        RpcUpdateLastBuffer(clipPlayer.LastBuffer);
    }

    [ClientRpc]
    protected virtual void RpcUpdateLastBuffer(double _lastBuffer)
    {
        if (isClient && !isServer)
        {
            Debug.Log($"{gameObject.name} old _lastBuffer: {clipPlayer.LastBuffer}, new _lastBuffer {_lastBuffer}");

            clipPlayer.LastBuffer = _lastBuffer;
        }
    }
    #endregion
}
