using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkAuthorityHandle : NetworkBehaviour
{
    private handle[] _handles;
    public TMPro.TMP_Text debugtextMesh;

    private NetworkTransformBase _netTransform;
    private bool _destroyIsTriggerd;

    public virtual void Start()
    {
        UpdateDebugText();
        _netTransform = GetComponent<NetworkTransformBase>();
        _handles = GetComponentsInChildren<handle>();
        if (_handles != null && _handles.Length > 0)
        {
            foreach (handle handle in _handles)
            {
                if (handle.onTrashEvents == null)
                {
                    handle.onTrashEvents = new UnityEngine.Events.UnityEvent();
                }
                if (handle.onStartGrabEvents == null)
                {
                    handle.onStartGrabEvents = new UnityEngine.Events.UnityEvent();
                }
                if (handle.onEndGrabEvents == null)
                {
                    handle.onEndGrabEvents = new UnityEngine.Events.UnityEvent();
                }
                handle.onTrashEvents.AddListener(NetworkDestroy);
                handle.onStartGrabEvents.AddListener(StartAuthorityGrabing);
                handle.onEndGrabEvents.AddListener(EndGrabing);
            }
        }
    }

    public void UpdateDebugText()
    {
        if (debugtextMesh != null)
        {
            Debug.Log($"Update Text {isServer}");
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
        //force release on manipulatorObject
        foreach (var handle in _handles)
        {
            if (handle.curState == manipObject.manipState.grabbed && handle.manipulatorObjScript != null)
            {
                handle.manipulatorObjScript.ForceRelease();
            }
        }
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
        Debug.Log($"End Grab of {gameObject.name}");
        if (!isServer && isOwned)
        {
            if (_netTransform != null)
            {
                _netTransform.CmdTeleport(transform.position, transform.rotation);
            }
            CmdRemoveObjectAuthority();
        }
        this.UpdateDebugText();
    }

    #endregion


    #region Destroy
    public void NetworkDestroy()
    {
        if (_destroyIsTriggerd)
        {
            return;
        }
        _destroyIsTriggerd = true;
        Debug.Log($"Trigger Destroy");
        if (isServer)
        {
            ServerNetworkDestroy();
        }
        else
        {
            if (isOwned)
            {
                Debug.Log($"Network Destroy on client {gameObject.name}");
                NetworkServer.Destroy(gameObject);
            }
            else
            {
                CmdNetworkDestroy();
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdNetworkDestroy()
    {
        Debug.Log($"Network Destroy {gameObject.name}");
        NetworkServer.Destroy(gameObject);
        //Destroy(gameObject);
    }

    [Server]
    public void ServerNetworkDestroy()
    {
        Debug.Log($"Network Destroy {gameObject.name}");
        NetworkServer.Destroy(gameObject);
        //Destroy(gameObject);
    }
    #endregion
}
