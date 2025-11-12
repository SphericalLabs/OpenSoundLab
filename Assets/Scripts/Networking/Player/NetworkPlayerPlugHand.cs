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
using Mirror;
using UnityEngine;

public class NetworkPlayerPlugHand : NetworkBehaviour
{
    public GameObject handParent;

    [SyncVar(hook = nameof(OnSetConnectedJackIndex))]
    public int connectedJackIndex;
    [SyncVar(hook = nameof(OnChangeOffset))]
    private Vector3 offset;
    [SyncVar(hook = nameof(OnChangeRotationOffset))]
    private Quaternion rotationOffset;
    private NetworkPlugCopy networkedPlugInHand;
    private omniPlug plugInHand;

    public GameObject plugPrefab;

    public omniPlug PlugInHand { get => plugInHand; set => plugInHand = value; }

    //check if it delets plug if like that
    private void Update()
    {
        if (isLocalPlayer && connectedJackIndex != 0 && plugInHand == null)
        {
            SetHandJackIndex(0, Vector3.zero, Quaternion.identity);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isServer && connectedJackIndex != 0)
        {
            SetSamplePlug(offset, rotationOffset);
        }
    }

    public void OnSetConnectedJackIndex(int old, int newValue)
    {
        if (old != newValue && !isServer)
        {
            Debug.Log($"{gameObject.name} On set connected Jack Index: {newValue}");

            SetSamplePlug(offset, rotationOffset);
        }
    }


    public void OnChangeOffset(Vector3 old, Vector3 newValue)
    {
        if (networkedPlugInHand != null && !isLocalPlayer)
        {
            networkedPlugInHand.transform.localPosition = newValue;
        }
    }
    public void OnChangeRotationOffset(Quaternion old, Quaternion newValue)
    {
        if (networkedPlugInHand != null && !isLocalPlayer)
        {
            networkedPlugInHand.transform.localRotation = newValue;
        }
    }


    public void SetHandJackIndex(int index, Vector3 position, Quaternion rotation)
    {
        Debug.Log($"{gameObject.name} Set hand jack: {index}, {position}, {rotation}");
        if (isServer)
        {
            offset = position;
            rotationOffset = rotation;
            connectedJackIndex = index;
            SetSamplePlug(position, rotation);
        }
        else
        {
            CmdSetHandJackIndex(index, position, rotation);
        }
    }

    [Command]
    public void CmdSetHandJackIndex(int index, Vector3 position, Quaternion rotation)
    {
        connectedJackIndex = index;
        rotationOffset = rotation;
        SetSamplePlug(position, rotation);
    }

    private void SetSamplePlug(Vector3 position, Quaternion rotation)
    {
        if (networkedPlugInHand != null)
        {
            Destroy(networkedPlugInHand.gameObject);
            Debug.Log($"Destroy grabbed plug of {handParent}");
        }
        if (!isLocalPlayer && connectedJackIndex != 0)
        {
            //check if target jack of ID existst
            var targetJack = NetworkSpawnManager.Instance.GetJackById(connectedJackIndex);
            if (targetJack != null)
            {
                Debug.Log($"Change plug of {handParent} to {connectedJackIndex}, {position}, {rotation}");

                GameObject g = Instantiate(plugPrefab, handParent.transform);
                g.transform.localPosition = position;
                g.transform.localRotation = rotation;
                networkedPlugInHand = g.GetComponent<NetworkPlugCopy>();
                networkedPlugInHand.Initialize(this, targetJack);
            }
        }
    }



    public void PassToOtherHand()
    {
        Debug.Log($"{gameObject.name} plug passed to other hand");
        if (isServer)
        {
            connectedJackIndex = 0;
            networkedPlugInHand = null;
            RpcDeleteGrabbedPlugInHand();

        }
        else
        {
            networkedPlugInHand = null;
            CmdPassToOtherPlayer();
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdPassToOtherPlayer()
    {
        Debug.Log($"Cmd {gameObject.name} plug passed to other hand");

        connectedJackIndex = 0;
        if (networkedPlugInHand != null)
        {
            Destroy(networkedPlugInHand.gameObject);
        }
        RpcDeleteGrabbedPlugInHand();
    }

    [ClientRpc]
    public void RpcDeleteGrabbedPlugInHand()
    {
        Debug.Log($"RPC {gameObject.name} delet grabbed plug");
        if (plugInHand != null)
        {
            Destroy(plugInHand.gameObject);
        }
    }
}
