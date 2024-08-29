using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class NetworkButtons : NetworkBehaviour
{
    public button[] buttons;

    public readonly SyncList<bool> buttonValues = new SyncList<bool>();

    private float[] lastToggeldTimes;

    public override void OnStartServer()
    {
        base.OnStartServer();
        buttonValues.Clear();
        foreach (var button in buttons)
        {
            buttonValues.Add(button.isHit);
        }
    }

    private void Start()
    {
        lastToggeldTimes = new float[buttons.Length];
        //add dials on change callback event
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].onToggleChangedEvent.AddListener(delegate { UpdateButtonIsHit(index); });
            buttons[i].onToggleChangedEvent.AddListener(delegate { UpdateLastToggledTime(index); });
        }
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            buttonValues.Callback += OnButtonUpdated;

            // Process initial SyncList payload
            for (int i = 0; i < buttonValues.Count; i++)
            {
                OnButtonUpdated(SyncList<bool>.Operation.OP_ADD, i, buttons[i].isHit, buttonValues[i]);
            }
        }
    }

    void OnButtonUpdated(SyncList<bool>.Operation op, int index, bool oldValue, bool newValue)
    {
        switch (op)
        {
            case SyncList<bool>.Operation.OP_ADD:
                buttons[index].keyHit(newValue, false);
                break;
            case SyncList<bool>.Operation.OP_INSERT:
                break;
            case SyncList<bool>.Operation.OP_REMOVEAT:
                break;
            case SyncList<bool>.Operation.OP_SET:
                if (IsToggleCooldownOver(index))
                {
                    buttons[index].keyHit(newValue, false);
                }
                break;
            case SyncList<bool>.Operation.OP_CLEAR:
                break;
        }
    }

    public void UpdateButtonIsHit(int index)
    {
        Debug.Log($"Update button hit of index: {index} to value: {buttons[index].isHit}");
        if (isServer)
        {
            buttonValues[index] = buttons[index].isHit;
        }
        else
        {
            CmdUpdateButtonIsHit(index, buttons[index].isHit);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateButtonIsHit(int index, bool value)
    {
        buttonValues[index] = value;
        buttons[index].keyHit(value, false);
    }

    public void UpdateLastToggledTime(int index)
    {
        if (index >= 0 && index < lastToggeldTimes.Length)
        {
            lastToggeldTimes[index] = Time.time;
        }
    }

    private bool IsToggleCooldownOver(int index)
    {
        if (lastToggeldTimes[index] + 0.5f < Time.time)
        {
            return true;
        }
        return false;
    }
}
