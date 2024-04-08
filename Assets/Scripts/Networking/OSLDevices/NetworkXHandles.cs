using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkXHandles : NetworkBehaviour
{
    public xHandle[] xHandles;

    public readonly SyncList<float> xValues = new SyncList<float>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        foreach (var handle in xHandles)
        {
            xValues.Add(handle.transform.localPosition.x);
        }
    }

    private void Start()
    {
        //add dials on change callback event
        for (int i = 0; i < xHandles.Length; i++)
        {
            int index = i;
            xHandles[i].onHandleChangedEvent.AddListener(delegate { UpdateHandleValue(index); });
        }
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            xValues.Callback += OnDialsUpdated;

            // Process initial SyncList payload
            for (int i = 0; i < xValues.Count; i++)
            {
                OnDialsUpdated(SyncList<float>.Operation.OP_ADD, i, xHandles[i].transform.localPosition.x, xValues[i]);
            }
        }
    }

    void OnDialsUpdated(SyncList<float>.Operation op, int index, float oldValue, float newValue)
    {
        switch (op)
        {
            case SyncList<float>.Operation.OP_ADD:
                xHandles[index].updatePos(newValue);
                break;
            case SyncList<float>.Operation.OP_INSERT:
                break;
            case SyncList<float>.Operation.OP_REMOVEAT:
                break;
            case SyncList<float>.Operation.OP_SET:
                if (xHandles[index].curState != manipObject.manipState.grabbed)
                {
                    xHandles[index].updatePos(newValue);
                }
                break;
            case SyncList<float>.Operation.OP_CLEAR:
                break;
        }
    }

    public void UpdateHandleValue(int index)
    {
        Debug.Log($"Update xHandle value of index: {index} to value: {xHandles[index].transform.localPosition.x}");
        if (isServer)
        {
            xValues[index] = xHandles[index].transform.localPosition.x;
        }
        else
        {
            CmdUpdateHandleValue(index, xHandles[index].transform.localPosition.x);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateHandleValue(int index, float value)
    {
        xValues[index] = value;
        xHandles[index].updatePos(value);
    }
}
