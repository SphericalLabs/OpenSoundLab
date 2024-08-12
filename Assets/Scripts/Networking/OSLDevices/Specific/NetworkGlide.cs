using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkGlide : NetworkSyncListener
{
    protected glideSignalGenerator glideSignalGenerator;
    protected glideDeviceInterface glideDeviceInterface;

    protected virtual void Awake()
    {
        glideSignalGenerator = GetComponent<glideSignalGenerator>();
        glideDeviceInterface = GetComponent<glideDeviceInterface>();
        glideDeviceInterface.ValueDial.onEndGrabEvents.AddListener(OnStopDragDial);
    }
    #region Mirror


    public override void OnStartClient()
    {
        if (!isServer)
        {
            CmdRequestSync();
        }
    }

    protected override void OnSync()
    {
        base.OnSync();
        if (isServer)
        {
            RpcUpdateGlidedVal(glideSignalGenerator.GlidedVal);
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
            RpcUpdateGlidedVal(glideSignalGenerator.GlidedVal);
        }
    }

    [Command(requiresAuthority = false)]
    protected virtual void CmdRequestSync()
    {
        Debug.Log($"{gameObject.name} CmdRequestSync glidedVal {glideSignalGenerator.GlidedVal}");
        RpcUpdateGlidedVal(glideSignalGenerator.GlidedVal);
    }

    [ClientRpc]
    protected virtual void RpcUpdateGlidedVal(float glidedVal)
    {
        if (isClient && !isServer)
        {
            Debug.Log($"{gameObject.name} old glidedVal: {glideSignalGenerator.GlidedVal}, new phase {glidedVal}");
            glideSignalGenerator.GlidedVal = glidedVal;
        }
    }
    #endregion

    #region onDial
    public void OnStopDragDial()
    {
        OnSync();
    }
    #endregion
}
