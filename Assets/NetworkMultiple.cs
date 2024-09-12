using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkMultiple : NetworkBehaviour
{

    [SyncVar(hook = nameof(OnIsSplitterChanged))]
    public bool isSplitter;

    multipleDeviceInterface multipleDeviceInterface;

    public void Awake()
    {
        multipleDeviceInterface = GetComponent<multipleDeviceInterface>();
    }

    public void Start()
    {
        multipleDeviceInterface.OnIsSplitterChanged.AddListener(IsSplitterUpdated);        
    }

    public void OnIsSplitterChanged(bool oldVar, bool newVar){
        multipleDeviceInterface.isSplitter = newVar;
    }

    void IsSplitterUpdated()
    {
        Debug.Log($"Update IsSplitter: {multipleDeviceInterface.isSplitter}");
        if (isServer)
        {
            isSplitter = multipleDeviceInterface.isSplitter;
        }
        else
        {
            CmdBinauralUpdate(multipleDeviceInterface.isSplitter);
        }
    }

    public override void OnStartClient(){
        OnIsSplitterChanged(multipleDeviceInterface.isSplitter, isSplitter);
    }

    [Command(requiresAuthority = false)]
    public void CmdBinauralUpdate(bool isSplit)
    {
        isSplitter = isSplit;
        multipleDeviceInterface.setFlow(isSplitter, true);
    }
}
