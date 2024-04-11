using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class NetworkYHandles : NetworkBehaviour
{
    public yHandle[] yHandles;

    public readonly SyncList<float> yValues = new SyncList<float>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        foreach (var handle in yHandles)
        {
            yValues.Add(handle.transform.localPosition.x);
        }
    }

    private void Start()
    {
        //add dials on change callback event
        for (int i = 0; i < yHandles.Length; i++)
        {
            int index = i;
            yHandles[i].onHandleChangedEvent.AddListener(delegate { UpdateHandleValue(index); });
        }
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            yValues.Callback += OnHandleUpdated;

            // Process initial SyncList payload
            for (int i = 0; i < yValues.Count; i++)
            {
                OnHandleUpdated(SyncList<float>.Operation.OP_ADD, i, yHandles[i].transform.localPosition.x, yValues[i]);
            }
        }
    }

    void OnHandleUpdated(SyncList<float>.Operation op, int index, float oldValue, float newValue)
    {
        switch (op)
        {
            case SyncList<float>.Operation.OP_ADD:
                yHandles[index].updatePos(newValue);
                break;
            case SyncList<float>.Operation.OP_INSERT:
                break;
            case SyncList<float>.Operation.OP_REMOVEAT:
                break;
            case SyncList<float>.Operation.OP_SET:
                if (yHandles[index].curState != manipObject.manipState.grabbed)
                {
                    yHandles[index].updatePos(newValue);
                }
                break;
            case SyncList<float>.Operation.OP_CLEAR:
                break;
        }
    }

    public void UpdateHandleValue(int index)
    {
        Debug.Log($"Update xHandle value of index: {index} to value: {yHandles[index].transform.localPosition.x}");
        if (isServer)
        {
            yValues[index] = yHandles[index].transform.localPosition.x;
        }
        else
        {
            CmdUpdateHandleValue(index, yHandles[index].transform.localPosition.x);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateHandleValue(int index, float value)
    {
        yValues[index] = value;
        yHandles[index].updatePos(value);
    }
}
