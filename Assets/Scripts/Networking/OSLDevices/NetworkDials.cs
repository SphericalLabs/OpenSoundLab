using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkDials : NetworkBehaviour
{
    public dial[] dials;

    public readonly SyncList<float> dialValues = new SyncList<float>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        foreach (var dial in dials)
        {
            dialValues.Add(dial.percent);
        }
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        dialValues.Clear();
    }

    private void Start()
    {
        //add dials on change callback event
        for (int i = 0; i < dials.Length; i++)
        {
            int index = i;
            dials[i].onPercentChangedEvent.AddListener(delegate { UpdateDialValue(index); });
            if (dials[i].DialFeedback == null)
            {
                dials[i].DialFeedback = dials[i].transform.parent.Find("glowDisk").GetComponent<glowDisk>();
            }
        }
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            dialValues.Callback += OnDialsUpdated;

            // Process initial SyncList payload
            for (int i = 0; i < dialValues.Count; i++)
            {
                OnDialsUpdated(SyncList<float>.Operation.OP_ADD, i, dials[i].percent, dialValues[i]);
            }
        }
    }

    void OnDialsUpdated(SyncList<float>.Operation op, int index, float oldValue, float newValue)
    {
        switch (op)
        {
            case SyncList<float>.Operation.OP_ADD:
                dials[index].setPercent(newValue);
                break;
            case SyncList<float>.Operation.OP_INSERT:
                break;
            case SyncList<float>.Operation.OP_REMOVEAT:
                break;
            case SyncList<float>.Operation.OP_SET:
                if (dials[index].curState != manipObject.manipState.grabbed)
                {
                    dials[index].setPercent(newValue);
                }
                break;
            case SyncList<float>.Operation.OP_CLEAR:
                break;
        }
    }

    public void UpdateDialValue(int index)
    {
        //Debug.Log($"Update dial value of index: {index} to value: {dials[index].percent}");
        if (isServer)
        {
            dialValues[index] = dials[index].percent;
        }
        else
        {
            CmdUpdateDialValue(index, dials[index].percent);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateDialValue(int index, float value)
    {
        dialValues[index] = value;
        dials[index].setPercent(value);
    }
}
