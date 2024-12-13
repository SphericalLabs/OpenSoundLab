using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkOscillator : NetworkSyncListener
{
    protected oscillatorSignalGenerator signalGenerator;
    protected oscillatorDeviceInterface oscillatorDeviceInterface;

    protected virtual void Awake()
    {
        signalGenerator = GetComponent<oscillatorSignalGenerator>();
        oscillatorDeviceInterface = GetComponent<oscillatorDeviceInterface>();
        oscillatorDeviceInterface.lfoSwitch.onSwitchChangedEvent.AddListener(OnLfoChange);

        var freqDial = oscillatorDeviceInterface.freqDial;
        freqDial.onPercentChangedEvent.AddListener(OnDragDial);
        freqDial.onEndGrabEvents.AddListener(OnStopDragDial);
    }
    #region Mirror
    public void OnLfoChange()
    {
        StartCoroutine(WaitForLfoChanged());
    }

    IEnumerator WaitForLfoChanged()
    {
        yield return new WaitForEndOfFrame();
        Debug.Log($"On Change Lfo {!oscillatorDeviceInterface.lfoSwitch.switchVal}");

        if (!oscillatorDeviceInterface.lfoSwitch.switchVal)
        {
            OnSync();
        }
    }


    public override void OnStartClient()
    {
        base.OnStartClient();

        StartCoroutine(WaitForLfoChanged());
    }

    protected override void OnSync()
    {
        base.OnSync();
        if (isServer)
        {
            RpcUpdatePhase(signalGenerator._phase);
        }
        else
        {
            CmdRequestSync();
        }
    }

    protected override void OnIntervalSync()
    {
        if (oscillatorDeviceInterface.lfoSwitch.switchVal)
        {
            return;
        }
        base.OnIntervalSync();
        if (isServer)
        {
            RpcUpdatePhase(signalGenerator._phase);
        }
    }

    [Command(requiresAuthority = false)]
    protected virtual void CmdRequestSync()
    {
        Debug.Log($"{gameObject.name} CmdRequestSync phase {signalGenerator._phase}, lfo {signalGenerator.lfo}, frequency {signalGenerator.frequency}");
        RpcUpdatePhase(signalGenerator._phase);
    }

    [ClientRpc]
    protected virtual void RpcUpdatePhase(double phase)
    {
        if (isClient && !isServer)
        {
            Debug.Log($"{gameObject.name} old phase: {signalGenerator._phase}, new phase {phase}, lfo {signalGenerator.lfo}, frequency {signalGenerator.frequency}");
            signalGenerator._phase = phase;
        }
    }
    #endregion


    #region onDial
    public void OnDragDial()
    {
        if (oscillatorDeviceInterface.lfoSwitch.switchVal)
        {
            return;
        }
        if (Time.frameCount % 8 == 0)
        {
            OnSync();
        }
    }

    public void OnStopDragDial()
    {
        if (oscillatorDeviceInterface.lfoSwitch.switchVal)
        {
            return;
        }
        OnSync();
    }
    #endregion
}
