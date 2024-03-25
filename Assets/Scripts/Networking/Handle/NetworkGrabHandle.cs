using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkGrabHandle : NetworkBehaviour
{
    private handle _handle;

    public TextMesh debugtextMesh;

    private NetworkTransformReliable _networkTransformBase;

    public bool isMovingByClient = false;

    public virtual void Awake()
    {
        UpdateDebugText();
        _handle = GetComponentInChildren<handle>();
    }

    public void UpdateDebugText()
    {
        if (debugtextMesh != null)
        {
            debugtextMesh.text = (isServer ? "Server: client connection" + connectionToClient : "Client: ") + "\nhas authority: " + authority + "\nClient Transform authority: ";

        }
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        UpdateDebugText();
    }

    public override void OnStopAuthority()
    {
        base.OnStopAuthority();
        UpdateDebugText();
    }


    #region IGrabable

    //invoked by vrcontroller
    public virtual void StartAuthorityGrabing()
    {
        if (!isServer && !authority)
            NetworkSpawnManager.Instance.CmdGetObjectAuthority(netIdentity);
        else if (isServer)
        {
            netIdentity.RemoveClientAuthority();
        }

        UpdateDebugText();
    }

    //invoked by vrcontroller
    public virtual void EndGrabing()
    {
        this.UpdateDebugText();

        if (authority)
            NetworkSpawnManager.Instance.CmdRemoveObjectAuthority(netIdentity);
    }

    #endregion
}
