using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class NetworkSliders : NetworkBehaviour
{

    public slider[] sliders;
    public readonly SyncList<float> sliderValues = new SyncList<float>(); // synced automatically, but not on first init. changes can and should be handled specifically

    private float[] lastGrabedTimes;
    // user grabs slider
    // PercentChanged is triggered by slider
    // OnPercentChanged is triggered by the event
    // server: directly saves into data model
    // client: calls CmdOnPercentChanged which saves new values into interface and model on server
    // SyncList of the data model is changed and synced to clients automatically
    // OnSlidersUpdated is triggered because it was registered to changes of the SyncList data model

    // onPercentChangedEvent -> PercentChanged
    // UpdateSliderValue -> OnPercentChanged
    // CmdUpdateSliderValue -> CmdOnPercentChanged
    // OnDialsUpdated -> OnSlidersUpdated


    // client and server
    private void Start()
    {
        lastGrabedTimes = new float[sliders.Length];

        //add dials on change callback event
        for (int i = 0; i < sliders.Length; i++)
        {
            int index = i;
            sliders[i].PercentChanged.AddListener(delegate { OnPercentChanged(index); }); // passing the method to be called, as a lambda delegate in order to also pass the index
            sliders[i].onEndGrabEvents.AddListener(delegate { UpdateLastGrabedTime(index); });
        }
    }

    // server
    public override void OnStartServer()
    {
        base.OnStartServer();
        foreach (var slider in sliders)
        {
            sliderValues.Add(slider.percent); // read in the values for the data model from the current state of the interface
        }
    }

    // client
    public override void OnStartClient()
    {
        if (!isServer)
        {
            sliderValues.Callback += OnSlidersUpdated;

            for (int i = 0; i < sliderValues.Count; i++) // Process initial SyncList payload
            {
                OnSlidersUpdated(SyncList<float>.Operation.OP_ADD, i, sliders[i].percent, sliderValues[i]); 
            }
        }
    }

    // client
    void OnSlidersUpdated(SyncList<float>.Operation op, int index, float oldValue, float newValue)
    {
        switch (op)
        {
            case SyncList<float>.Operation.OP_ADD:
                sliders[index].setPercent(newValue);
                break;
            case SyncList<float>.Operation.OP_INSERT:
                break;
            case SyncList<float>.Operation.OP_REMOVEAT:
                break;
            case SyncList<float>.Operation.OP_SET:
                if (sliders[index].curState != manipObject.manipState.grabbed && IsEndGrabCooldownOver(index))
                {
                    sliders[index].setPercent(newValue);
                }
                break;
            case SyncList<float>.Operation.OP_CLEAR:
                break;
        }
    }

    // client and server
    public void OnPercentChanged(int index) // called from the sliders' onPercentChangedEvent
    {
        Debug.Log($"Update dial value of index: {index} to value: {sliders[index].percent}");
        if (isServer)
        {
            sliderValues[index] = sliders[index].percent; // directly write into data model, this triggers a SyncList Update
        }
        else
        {
            CmdOnPercentChanged(index, sliders[index].percent); // call a command on server to update data model
        }
    }

    // client
    [Command(requiresAuthority = false)] // by default only the client with authority over this object may call a server command, but this disables that
    public void CmdOnPercentChanged(int index, float value)
    {
        sliderValues[index] = value;
        sliders[index].setPercent(value);
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