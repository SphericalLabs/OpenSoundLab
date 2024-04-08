using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkSliders : NetworkBehaviour
{
    public sliderNotched[] sliders;

    public readonly SyncList<float> sliderValues = new SyncList<float>();

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
        //add dials on change callback event
        for (int i = 0; i < sliders.Length; i++)
        {
            int index = i;
            sliders[i].onPercentChangedEvent.AddListener(delegate { UpdateSliderValue(index); });
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
                //sliders[index].setPercent(newValue);
                break;
            case SyncList<float>.Operation.OP_INSERT:
                break;
            case SyncList<float>.Operation.OP_REMOVEAT:
                break;
            case SyncList<float>.Operation.OP_SET:
                //sliders[index].setPercent(newValue);
                break;
            case SyncList<float>.Operation.OP_CLEAR:
                break;
        }
    }

    public void UpdateSliderValue(int index)
    {
        Debug.Log($"Update dial value of index: {index} to value: {sliders[index].percent}");
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
        //sliders[index].setPercent(value);
    }
}
