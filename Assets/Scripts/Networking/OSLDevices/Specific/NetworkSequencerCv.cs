using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class NetworkSequencerCv : NetworkSyncListener
{
    protected sequencerCVDeviceInterface sequencerCvDeviceInterface;

    NetworkButtons networkButtons;
    NetworkDials networkDials;
    NetworkSwitchs networkSwitchs;
    NetworkJacks networkJacks;


    protected virtual void Awake()
    {
        sequencerCvDeviceInterface = GetComponent<sequencerCVDeviceInterface>();
        sequencerCvDeviceInterface.beatSlider.onEndGrabEvents.AddListener(OnSync);
        sequencerCvDeviceInterface.stepSelect.onEndGrabEvents.AddListener(OnSync);
        sequencerCvDeviceInterface.xyHandle.onEndGrabEvents.AddListener(OnSync);

        networkButtons = GetComponent<NetworkButtons>();
        networkDials = GetComponent<NetworkDials>();
        networkSwitchs = GetComponent<NetworkSwitchs>();
        networkJacks = GetComponent<NetworkJacks>();

        networkButtons.buttons = Utils.AddElementsToArray(sequencerCvDeviceInterface.GetComponentsInChildren<button>(true), networkButtons.buttons);
        networkDials.dials = Utils.AddElementsToArray(sequencerCvDeviceInterface.GetComponentsInChildren<dial>(true), networkDials.dials);
        networkSwitchs.switchs = Utils.AddElementsToArray(sequencerCvDeviceInterface.GetComponentsInChildren<basicSwitch>(true), networkSwitchs.switchs);
        networkJacks.omniJacks = Utils.AddElementsToArray(sequencerCvDeviceInterface.GetComponentsInChildren<omniJack>(true), networkJacks.omniJacks);

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
        Debug.Log($"{gameObject.name} CmdRequestSync curStep {sequencerCvDeviceInterface.CurStep}");
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
