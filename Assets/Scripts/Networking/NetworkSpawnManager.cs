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

public class NetworkSpawnManager : NetworkBehaviour
{
    public static NetworkSpawnManager Instance;

    public List<omniJack> networkedJacks;

    private void Start()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            networkedJacks = new List<omniJack>();
        }
    }

    #region Create Item
    public void CreateItem(string prefabName, Vector3 position, Quaternion rotation, Vector3 localPositionOffset, Vector3 localRotationOffset)
    {
        var prefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == prefabName);
        if (prefab != null)
        {
            var g = Instantiate(prefab, position, rotation);
            g.transform.Translate(localPositionOffset, Space.Self);
            g.transform.Rotate(localRotationOffset, Space.Self);
            NetworkServer.Spawn(g);
            g.transform.parent = GameObject.Find("PatchAnchor").transform;

            Debug.Log($"{prefabName} spawned by the host");
        }
    }

    public void CreateItem(string prefabName, Vector3 position, Quaternion rotation, Vector3 localPositionOffset, Vector3 localRotationOffset, manipulator manip)
    {
        var prefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == prefabName);
        if (prefab != null)
        {
            var g = Instantiate(prefab, position, rotation);

            // spawn directly into the hand if selecting by gaze
            if (manip != null && manip.wasGazeBased)
            {
                g.transform.position = manip.transform.position + manip.transform.forward * 0.12f;
                manip.wasGazeBased = false; // treat that interaction as a physical one from now on, otherwise it would be handled in fine mode by default
            }

            g.transform.Translate(localPositionOffset, Space.Self);
            g.transform.Rotate(localRotationOffset, Space.Self);
            NetworkServer.Spawn(g);
            manip.ForceGrab(g.GetComponentInChildren<handle>());

            Debug.Log($"{prefabName} spawned by the host into manipulatorObject");
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdCreateItem(string prefabName, Vector3 position, Quaternion rotation, Vector3 localPositionOffset, Vector3 localRotationOffset, NetworkIdentity player, bool isLeftHand)
    {
        var prefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == prefabName);
        if (prefab != null)
        {
            var g = Instantiate(prefab, position, rotation);
            g.transform.Translate(localPositionOffset, Space.Self);
            g.transform.Rotate(localRotationOffset, Space.Self);
            NetworkServer.Spawn(g, player.connectionToClient);
            g.transform.parent = GameObject.Find("PatchAnchor").transform;

            //todo send local player the info of the prefab and set grabbed
            Debug.Log($"{player.gameObject} spawned {prefabName}");
            if (player.TryGetComponent<VRNetworkPlayer>(out VRNetworkPlayer networkPlayer))
            {
                networkPlayer.GrabNewObjectByHand(g, isLeftHand);
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdCreateItem(string prefabName, Vector3 position, Quaternion rotation, Vector3 localPositionOffset, Vector3 localRotationOffset)
    {
        var prefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == prefabName);
        if (prefab != null)
        {
            var g = Instantiate(prefab, position, rotation);
            g.transform.Translate(localPositionOffset, Space.Self);
            g.transform.Rotate(localRotationOffset, Space.Self);
            NetworkServer.Spawn(g);
            g.transform.parent = GameObject.Find("PatchAnchor").transform;

            Debug.Log($"Spawned {prefabName}");
        }
    }

    #endregion

    #region Duplicate Object

    public void DuplicateItem(GameObject obj, manipulator manip)
    {
        var g = SaveLoadInterface.instance.Copy(obj, manip);
        NetworkServer.Spawn(g);
        g.transform.parent = GameObject.Find("PatchAnchor").transform;
        Debug.Log($"{g} duplicated by the host");
    }

    [Command(requiresAuthority = false)]
    public void CmdDuplicateItem(NetworkIdentity obj, NetworkIdentity player, bool isLeftHand)
    {
        var g = SaveLoadInterface.instance.Copy(obj.gameObject, null);
        Debug.Log($"{g} duplicated by the client");

        NetworkServer.Spawn(g, player.connectionToClient);
        g.transform.parent = GameObject.Find("PatchAnchor").transform;

        if (player.TryGetComponent<VRNetworkPlayer>(out VRNetworkPlayer networkPlayer))
        {
            networkPlayer.GrabNewObjectByHand(g, isLeftHand);
        }
    }


    #endregion

    #region Networked Jacks
    public void AddJack(omniJack omniJack)
    {
        if (!networkedJacks.Contains(omniJack))
        {
            networkedJacks.Add(omniJack);
        }
    }
    public void RemoveJack(omniJack omniJack)
    {
        if (networkedJacks.Contains(omniJack))
        {
            networkedJacks.Remove(omniJack);
        }
    }

    public omniJack GetJackById(int id)
    {
        var target = networkedJacks.Find((x) => x.ID == id);
        if (target != null)
        {
            return target;
        }
        return null;
    }
    #endregion
}
