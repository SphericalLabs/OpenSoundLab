using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkKey : NetworkBehaviour
{
    public key[] keys;

    public readonly SyncList<bool> keyValues = new SyncList<bool>();

    private float[] lastKeyHitTimes;

    public override void OnStartServer()
    {
        base.OnStartServer();
        foreach (var button in keys)
        {
            keyValues.Add(button.isHit);
        }
    }

    private void Start()
    {
        lastKeyHitTimes = new float[keys.Length];
        //add dials on change callback event
        for (int i = 0; i < keys.Length; i++)
        {
            int index = i;
            keys[i].onKeyChangedEvent.AddListener(delegate { UpdateKeyIsHit(index); });
            keys[i].onKeyChangedEvent.AddListener(delegate { UpdateLastKeyHitTime(index); });
        }
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            keyValues.Callback += OnKeyUpdated;

            // Process initial SyncList payload
            for (int i = 0; i < keyValues.Count; i++)
            {
                OnKeyUpdated(SyncList<bool>.Operation.OP_ADD, i, keys[i].isHit, keyValues[i]);
            }
        }
    }

    void OnKeyUpdated(SyncList<bool>.Operation op, int index, bool oldValue, bool newValue)
    {
        switch (op)
        {
            case SyncList<bool>.Operation.OP_ADD:
                keys[index].phantomHit(newValue);
                break;
            case SyncList<bool>.Operation.OP_INSERT:
                break;
            case SyncList<bool>.Operation.OP_REMOVEAT:
                break;
            case SyncList<bool>.Operation.OP_SET:
                if (IsKeyHitCooldownOver(index))
                {
                    keys[index].phantomHit(newValue, true);
                }
                break;
            case SyncList<bool>.Operation.OP_CLEAR:
                break;
        }
    }

    public void UpdateKeyIsHit(int index)
    {
        Debug.Log($"Update key hit of index: {index} to value: {keys[index].isHit}");
        if (isServer)
        {
            keyValues[index] = keys[index].isHit;
        }
        else
        {
            CmdKeyButtonIsHit(index, keys[index].isHit);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdKeyButtonIsHit(int index, bool value)
    {
        keyValues[index] = value;
        keys[index].phantomHit(value);
    }


    public void UpdateLastKeyHitTime(int index)
    {
        if (index >= 0 && index < lastKeyHitTimes.Length)
        {
            lastKeyHitTimes[index] = Time.time;
        }
    }

    private bool IsKeyHitCooldownOver(int index)
    {
        if (lastKeyHitTimes[index] + 0.5f < Time.time)
        {
            return true;
        }
        return false;
    }
}

