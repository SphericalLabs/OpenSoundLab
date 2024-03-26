using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkAuthorityHandle : NetworkBehaviour
{
    private handle _handle;
    public TMPro.TMP_Text debugtextMesh;

    public virtual void Awake()
    {
        UpdateDebugText();
        _handle = GetComponentInChildren<handle>();
        if (_handle != null)
        {
            _handle.onTrashEvents.AddListener(NetworkDestroy);
            _handle.onStartGrabEvents.AddListener(StartAuthorityGrabing);
            _handle.onEndGrabEvents.AddListener(EndGrabing);
        }
    }

    public void UpdateDebugText()
    {
        if (debugtextMesh != null)
        {
            debugtextMesh.text = (isServer ? "Server: client connection" + connectionToClient : "Client: ") + "\nhas authority: " + isOwned;
        }
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        UpdateDebugText();
        Debug.Log($"Start Authority of {gameObject.name}");
    }

    public override void OnStopAuthority()
    {
        base.OnStopAuthority();
        UpdateDebugText();
        Debug.Log($"Stop Authority of {gameObject.name}");
    }

    [Command(requiresAuthority = false)]
    public void CmdRemoveObjectAuthority()
    {
        netIdentity.RemoveClientAuthority();
    }

    #region IGrabable

    //invoked by vrcontroller
    public virtual void StartAuthorityGrabing()
    {
        Debug.Log($"Start Grab of {gameObject.name}");
        if (!isServer && !authority)
            NetworkMenuManager.Instance.localPlayer.CmdGetObjectAuthority(netIdentity);
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

        Debug.Log($"End Grab of {gameObject.name}");
        if (!isServer)
            CmdRemoveObjectAuthority();
    }

    #endregion


    #region Destroy
    public void NetworkDestroy()
    {
        Debug.Log($"Trigger Destroy");
        if (isServer)
        {
            ServerNetworkDestroy();
        }
        else
        {
            CmdNetworkDestroy();
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdNetworkDestroy()
    {
        Debug.Log($"Network Destroy {gameObject.name}");
        NetworkServer.Destroy(gameObject);
        Destroy(gameObject);
    }

    [Server]
    public void ServerNetworkDestroy()
    {
        Debug.Log($"Network Destroy {gameObject.name}");
        NetworkServer.Destroy(gameObject);
        Destroy(gameObject);
    }
    #endregion
}
