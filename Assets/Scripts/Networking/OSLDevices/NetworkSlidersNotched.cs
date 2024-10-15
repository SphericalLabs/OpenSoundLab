using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkSlidersNotched : NetworkBehaviour
{
    public sliderNotched[] sliders;

    public readonly SyncList<float> sliderValues = new SyncList<float>();

    private float[] lastGrabedTimes;

    public override void OnStartServer()
    {
        base.OnStartServer();
        foreach (var slider in sliders)
        {
            sliderValues.Add(slider.percent);
        }
    }

    private void Start()
    {
        lastGrabedTimes = new float[sliders.Length];

        //add dials on change callback event
        for (int i = 0; i < sliders.Length; i++)
        {
            int index = i;
            sliders[i].onPercentChangedEvent.AddListener(delegate { UpdateSliderValue(index); });
            sliders[i].onEndGrabEvents.AddListener(delegate { UpdateLastGrabedTime(index); });
        }
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            sliderValues.Callback += OnDialsUpdated;

            // Process initial SyncList payload
            for (int i = 0; i < sliderValues.Count; i++)
            {
                OnDialsUpdated(SyncList<float>.Operation.OP_ADD, i, sliders[i].percent, sliderValues[i]);
            }
        }
    }

    void OnDialsUpdated(SyncList<float>.Operation op, int index, float oldValue, float newValue)
    {
        switch (op)
        {
            case SyncList<float>.Operation.OP_ADD:
                sliders[index].setValByPercent(newValue);
                break;
            case SyncList<float>.Operation.OP_INSERT:
                break;
            case SyncList<float>.Operation.OP_REMOVEAT:
                break;
            case SyncList<float>.Operation.OP_SET:
                if (sliders[index].curState != manipObject.manipState.grabbed && IsEndGrabCooldownOver(index))
                {
                    sliders[index].setValByPercent(newValue);
                }
                break;
            case SyncList<float>.Operation.OP_CLEAR:
                break;
        }
    }

    public void UpdateSliderValue(int index)
    {
        if (isServer)
        {
            sliderValues[index] = sliders[index].percent;
        }
        else
        {
            CmdUpdateSliderValue(index, sliders[index].percent);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateSliderValue(int index, float value)
    {
        sliderValues[index] = value;
        sliders[index].setValByPercent(value);
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
