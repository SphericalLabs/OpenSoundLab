using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class NetworkSliders : NetworkBehaviour
{

    public slider[] sliders;
    public readonly SyncList<float> sliderValues = new SyncList<float>(); // synced automatically, but not on first init. changes can and should be handled specifically

    // user grabs slider
    // onPercentChangedEvent is triggered by slider
    // UpdateSliderValue is triggered by the event
        // server: directly saves into data model
        // client: calls CmdUpdateSliderValue which saves new values into interface and model on server
    // SyncList of the data model is changed and synced to clients automatically
    // OnDialsUpdated is triggered because it was registered to changes of the SyncList data model

    // onPercentChangedEvent -> OnPercentChanged
    // UpdateSliderValue -> OnPercentChangedHandler
    // CmdUpdateSliderValue -> CmdOnPercentChangedHandler
    // OnDialsUpdated -> sliderValuesChangedHandler, sliderValuesCallback, sliderValuesHandler, sliderValuesCallbackHandler, sliderValuesUpdated
    

    // client and server
    private void Start()
    {
        //add dials on change callback event
        for (int i = 0; i < sliders.Length; i++)
        {
            int index = i;
            sliders[i].onPercentChangedEvent.AddListener(delegate { UpdateSliderValue(index); }); // passing the method to be called, as a lambda delegate in order to also pass the index
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
            sliderValues.Callback += OnDialsUpdated;

            for (int i = 0; i < sliderValues.Count; i++) // Process initial SyncList payload
            {
                OnDialsUpdated(SyncList<float>.Operation.OP_ADD, i, sliders[i].percent, sliderValues[i]); 
            }
        }
    }

    // client
    void OnDialsUpdated(SyncList<float>.Operation op, int index, float oldValue, float newValue)
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
                if (sliders[index].curState != manipObject.manipState.grabbed)
                {
                    sliders[index].setPercent(newValue);
                }
                break;
            case SyncList<float>.Operation.OP_CLEAR:
                break;
        }
    }

    // client and server
    public void UpdateSliderValue(int index) // called from the sliders' onPercentChangedEvent
    {
        Debug.Log($"Update dial value of index: {index} to value: {sliders[index].percent}");
        if (isServer)
        {
            sliderValues[index] = sliders[index].percent; // directly write into data model, this triggers a SyncList Update
        }
        else
        {
            CmdUpdateSliderValue(index, sliders[index].percent); // call a command on server to update data model
        }
    }

    // client
    [Command(requiresAuthority = false)] // by default only the client with authority over this object may call a server command, but this disables that
    public void CmdUpdateSliderValue(int index, float value)
    {
        sliderValues[index] = value;
        sliders[index].setPercent(value);
    }
}