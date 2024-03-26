using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkAuthorityHandle : NetworkBehaviour
{
    private handle _handle;
    public TextMesh debugtextMesh;

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
            debugtextMesh.text = (isServer ? "Server: client connection" + connectionToClient : "Client: ") + "\nhas authority: " + authority;
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

    [Command(requiresAuthority = false)]
    public void CmdRemoveObjectAuthority()
    {
        netIdentity.RemoveClientAuthority();
    }

    #region IGrabable

    //invoked by vrcontroller
    public virtual void StartAuthorityGrabing()
    {
        Debug.Log($"Get Authority of {gameObject.name}");
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

        Debug.Log($"Remove Authority of {gameObject.name}");
        if (authority)
            CmdRemoveObjectAuthority();
    }

    #endregion


    #region Destroy
    public void NetworkDestroy()
    {
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
        NetworkServer.Destroy(gameObject);
        Debug.Log($"Network Destroy {gameObject.name}");
    }

    [Server]
    public void ServerNetworkDestroy()
    {
        NetworkServer.Destroy(gameObject);
        Debug.Log($"Network Destroy {gameObject.name}");
    }
    #endregion
}
