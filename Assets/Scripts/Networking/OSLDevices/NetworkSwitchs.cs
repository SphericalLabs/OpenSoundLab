using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkSwitchs : NetworkBehaviour
{
    public basicSwitch[] switchs;

    public readonly SyncList<bool> switchValues = new SyncList<bool>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        foreach (var s in switchs)
        {
            switchValues.Add(s.switchVal);
        }
    }

    private void Start()
    {
        //add dials on change callback event
        for (int i = 0; i < switchs.Length; i++)
        {
            int index = i;
            switchs[i].onSwitchChangedEvent.AddListener(delegate { UpdateSwitchValue(index); });
        }
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            switchValues.Callback += OnSwitchUpdated;

            // Process initial SyncList payload
            for (int i = 0; i < switchValues.Count; i++)
            {
                OnSwitchUpdated(SyncList<bool>.Operation.OP_ADD, i, switchs[i].switchVal, switchValues[i]);
            }
        }
    }

    void OnSwitchUpdated(SyncList<bool>.Operation op, int index, bool oldValue, bool newValue)
    {
        switch (op)
        {
            case SyncList<bool>.Operation.OP_ADD:
                switchs[index].setSwitch(newValue, true, true);
                break;
            case SyncList<bool>.Operation.OP_INSERT:
                break;
            case SyncList<bool>.Operation.OP_REMOVEAT:
                break;
            case SyncList<bool>.Operation.OP_SET:
                switchs[index].setSwitch(newValue, true, true);
                break;
            case SyncList<bool>.Operation.OP_CLEAR:
                break;
        }
    }

    public void UpdateSwitchValue(int index)
    {
        Debug.Log($"Update button hit of index: {index} to value: {switchs[index].switchVal}");
        if (isServer)
        {
            switchValues[index] = switchs[index].switchVal;
        }
        else
        {
            CmdUpdateSwitchIsChanged(index, switchs[index].switchVal);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateSwitchIsChanged(int index, bool value)
    {
        switchValues[index] = value;
        switchs[index].setSwitch(value, false, true);
    }
}

