using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkXHandles : NetworkBehaviour
{
    public xHandle[] xHandles;

    public readonly SyncList<float> xValues = new SyncList<float>();
    private float[] lastGrabedTimes;

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
        lastGrabedTimes = new float[xHandles.Length];

        //add dials on change callback event
        for (int i = 0; i < xHandles.Length; i++)
        {
            int index = i;
            xHandles[i].onHandleChangedEvent.AddListener(delegate { UpdateHandleValue(index); });
            xHandles[i].onEndGrabEvents.AddListener(delegate { UpdateLastGrabedTime(index); });

        }
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            xValues.Callback += OnHandleUpdated;

            // Process initial SyncList payload
            for (int i = 0; i < xValues.Count; i++)
            {
                OnHandleUpdated(SyncList<float>.Operation.OP_ADD, i, xHandles[i].transform.localPosition.x, xValues[i]);
            }
        }
    }

    void OnHandleUpdated(SyncList<float>.Operation op, int index, float oldValue, float newValue)
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
                if (xHandles[index].curState != manipObject.manipState.grabbed && IsEndGrabCooldownOver(index))
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

    public void UpdateLastGrabedTime(int index)
    {
        if (index >= 0 && index < lastGrabedTimes.Length)
        {
            lastGrabedTimes[index] = Time.time;
        }
    }

    private bool IsEndGrabCooldownOver(int index)
    {
        if (lastGrabedTimes[index] + 0.5f < Time.time)
        {
            return true;
        }
        return false;
    }
}
