using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class NetworkXYHandles : NetworkBehaviour
{
    public xyHandle[] xyHandles;

    public readonly SyncList<Vector2> xyValues = new SyncList<Vector2>();
    private float[] lastGrabedTimes;

    public override void OnStartServer()
    {
        base.OnStartServer();
        foreach (var handle in xyHandles)
        {
            xyValues.Add(new Vector2(handle.transform.localPosition.x, handle.transform.localPosition.y));
        }
    }

    private void Awake()
    {
        lastGrabedTimes = new float[xyHandles.Length];

        //add dials on change callback event
        for (int i = 0; i < xyHandles.Length; i++)
        {
            int index = i;
            xyHandles[i].onHandleChangedEvent.AddListener(delegate { UpdateHandleValue(index); });
            xyHandles[i].onEndGrabEvents.AddListener(delegate { UpdateLastGrabedTime(index); });

        }
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            xyValues.Callback += OnHandleUpdated;

            // Process initial SyncList payload
            for (int i = 0; i < xyValues.Count; i++)
            {
                Vector2 vector = new Vector2(xyHandles[i].transform.localPosition.x, xyHandles[i].transform.localPosition.y);
                OnHandleUpdated(SyncList<Vector2>.Operation.OP_ADD, i, vector, xyValues[i]);
            }
        }
    }

    void OnHandleUpdated(SyncList<Vector2>.Operation op, int index, Vector2 oldValue, Vector2 newValue)
    {
        switch (op)
        {
            case SyncList<Vector2>.Operation.OP_ADD:
                xyHandles[index].updatePos(newValue);
                break;
            case SyncList<Vector2>.Operation.OP_INSERT:
                break;
            case SyncList<Vector2>.Operation.OP_REMOVEAT:
                break;
            case SyncList<Vector2>.Operation.OP_SET:
                if (xyHandles[index].curState != manipObject.manipState.grabbed && IsEndGrabCooldownOver(index))
                {
                    xyHandles[index].updatePos(newValue);
                }
                break;
            case SyncList<Vector2>.Operation.OP_CLEAR:
                break;
        }
    }

    public void UpdateHandleValue(int index)
    {
        Debug.Log($"Update xHandle value of index: {index} to value: {xyHandles[index].transform.localPosition.x}/{xyHandles[index].transform.localPosition.y}");
        if (isServer)
        {
            xyValues[index] = new Vector2(xyHandles[index].transform.localPosition.x, xyHandles[index].transform.localPosition.y);
        }
        else
        {
            CmdUpdateHandleValue(index, new Vector2(xyHandles[index].transform.localPosition.x, xyHandles[index].transform.localPosition.y));
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateHandleValue(int index, Vector2 value)
    {
        xyValues[index] = value;
        xyHandles[index].updatePos(value);
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