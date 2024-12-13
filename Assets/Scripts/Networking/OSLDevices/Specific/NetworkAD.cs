using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkAD : NetworkSyncListener
{
    protected ADSignalGenerator adSignalGenerator;
    protected ADDeviceInterface adDeviceInterface;
    protected virtual void Awake()
    {
        adSignalGenerator = GetComponent<ADSignalGenerator>();
        adDeviceInterface = GetComponent<ADDeviceInterface>();
        adDeviceInterface.attackDial.onEndGrabEvents.AddListener(OnStopDragDial);
        adDeviceInterface.releaseDial.onEndGrabEvents.AddListener(OnStopDragDial);
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
            RpcUpdateGlidedVal(adSignalGenerator.IsRunning, adSignalGenerator.Stage, adSignalGenerator.Counter, adSignalGenerator.GlidedVal);
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
            RpcUpdateGlidedVal(adSignalGenerator.IsRunning, adSignalGenerator.Stage, adSignalGenerator.Counter, adSignalGenerator.GlidedVal);
        }
    }

    [Command(requiresAuthority = false)]
    protected virtual void CmdRequestSync()
    {
        Debug.Log($"{gameObject.name} CmdRequestSync glidedVal {adSignalGenerator.GlidedVal}");
        RpcUpdateGlidedVal(adSignalGenerator.IsRunning, adSignalGenerator.Stage, adSignalGenerator.Counter, adSignalGenerator.GlidedVal);
    }

    [ClientRpc]
    protected virtual void RpcUpdateGlidedVal(bool isRunning, int stage, int counter, float glidedVal)
    {
        if (isClient && !isServer)
        {
            Debug.Log($"{gameObject.name} old glidedVal: {adSignalGenerator.GlidedVal}, new phase {glidedVal}");
            adSignalGenerator.IsRunning = isRunning;
            adSignalGenerator.Stage = stage;
            adSignalGenerator.Counter = counter;
            adSignalGenerator.GlidedVal = glidedVal;
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
