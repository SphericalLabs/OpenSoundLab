// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
//
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
//
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
//
//
// Copyright © 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright © 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright © 2017 Apache 2.0 Google LLC SoundStage VR
//
// Licensed under the Apache License, Version 2.0 (the "License");
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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

    [SyncVar(hook = nameof(OnControlledByClient))]
    public bool controlledByClient = false;

    public virtual void Start()
    {
        // Mirror network spawns to root level. Since every device has a NetworkAuthorityHandle, this is a good place to inject the resorting into PatchAnchor
        Transform pAnchor = GameObject.Find("PatchAnchor").transform;
        if (pAnchor != null && transform.parent != pAnchor) transform.parent = pAnchor;

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
        UpdateDebugText();
    }

    [Command(requiresAuthority = false)]
    public void CmdOnControlledByClient(bool b)
    {
        controlledByClient = b;
        UpdateDebugText();
    }

    public void OnControlledByClient(bool old, bool newBool)
    {
        _netTransform.syncDirection = controlledByClient ? SyncDirection.ClientToServer : SyncDirection.ServerToClient;
        if (!_netTransform.enabled)
        {
            _netTransform.enabled = true;
        }
    }

    public void UpdateDebugText()
    {
        if (debugtextMesh != null && _netTransform != null)
        {
            //Debug.Log($"Update Text {isServer}");
            debugtextMesh.text = (isServer ? "Server: client connection" + connectionToClient : "Client: ") + "\nhas authority: " + isOwned + "\nSync Direction: " + _netTransform.syncDirection;
        }
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        UpdateDebugText();
        //Debug.Log($"Start Authority of {gameObject.name}");
        CmdOnControlledByClient(true);
    }

    public override void OnStopAuthority()
    {
        base.OnStopAuthority();
        UpdateDebugText();
        //Debug.Log($"Stop Authority of {gameObject.name}");
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
        Debug.Log($"Start authority grab of {gameObject.name}");
        //todo don't take authority if already has authority and all handles are grabbed
        if (!isServer)
        {
            if (!authority && !AllHandlesGrabbed())
            {
                NetworkMenuManager.Instance.localPlayer.CmdGetObjectAuthority(netIdentity);
                //todo already allow to be moved by client, try to switch _netTransform direction
                //_netTransform.syncDirection = SyncDirection.ClientToServer;
                _netTransform.enabled = false;
            }
        }
        else if (isServer)
        {
            netIdentity.RemoveClientAuthority();
            controlledByClient = false;
        }

        UpdateDebugText();
    }

    //invoked by vrcontroller
    public virtual void EndGrabing()
    {
        Debug.Log($"End authority grab of {gameObject.name}");
        if (!isServer && isOwned)
        {
            if (_netTransform != null)
            {
                if (!_netTransform.enabled)
                {
                    _netTransform.enabled = true;
                }
                _netTransform.CmdTeleport(transform.position, transform.rotation);
            }
            CmdOnControlledByClient(false);
            CmdRemoveObjectAuthority();
        }
        UpdateDebugText();
    }

    bool AllHandlesGrabbed()
    {
        foreach (handle h in _handles)
        {
            if (h.curState != manipObject.manipState.grabbed)
            {
                return false;
            }
        }
        return true;
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
            CmdNetworkDestroy();
            /*if (isOwned)
            {
                Debug.Log($"Network Destroy on client {gameObject.name}");
                NetworkServer.Destroy(gameObject);
            }
            else
            {
                CmdNetworkDestroy();
            }*/
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdNetworkDestroy()
    {
        Debug.Log($"Network Destroy {gameObject.name}");
        netIdentity.RemoveClientAuthority();
        StartCoroutine(WaitDestroyTime());
        //Destroy(gameObject);
    }
    IEnumerator WaitDestroyTime()
    {
        yield return new WaitForSeconds(0.1f);

        NetworkServer.Destroy(gameObject);
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
