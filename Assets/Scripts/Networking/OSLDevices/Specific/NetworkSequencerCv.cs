using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class NetworkSequencerCv : NetworkSyncListener
{
    protected sequencerCVDeviceInterface sequencerCvDeviceInterface;

    protected virtual void Awake()
    {
        sequencerCvDeviceInterface = GetComponent<sequencerCVDeviceInterface>();
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
            RpcUpdateCurStep(sequencerCvDeviceInterface.CurStep);
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
            RpcUpdateCurStep(sequencerCvDeviceInterface.CurStep);
        }
    }

    [Command(requiresAuthority = false)]
    protected virtual void CmdRequestSync()
    {
        Debug.Log($"{gameObject.name} CmdRequestSync glidedVal {sequencerCvDeviceInterface.CurStep}");
        RpcUpdateCurStep(sequencerCvDeviceInterface.CurStep);
    }

    [ClientRpc]
    protected virtual void RpcUpdateCurStep(int curStep)
    {
        if (isClient && !isServer)
        {
            Debug.Log($"{gameObject.name} old curStep: {sequencerCvDeviceInterface.CurStep}, new curStep {curStep}");
            sequencerCvDeviceInterface.CurStep = curStep;
        }
    }
    #endregion
}
