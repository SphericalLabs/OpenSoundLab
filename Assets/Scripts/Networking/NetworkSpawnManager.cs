// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2026 OSLLv1 Sphericals OpenSoundLab
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
            registerRegistrySpawnPrefabs();
        }
    }

    void registerRegistrySpawnPrefabs()
    {
        if (NetworkManager.singleton == null || NetworkManager.singleton.spawnPrefabs == null) return;

        List<GameObject> registryPrefabs = OSLDeviceRegistry.GetNetworkPrefabs();
        for (int i = 0; i < registryPrefabs.Count; ++i)
        {
            GameObject prefab = registryPrefabs[i];
            if (prefab == null || hasSpawnPrefab(prefab)) continue;
            NetworkManager.singleton.spawnPrefabs.Add(prefab);
        }
    }

    bool hasSpawnPrefab(GameObject targetPrefab)
    {
        if (targetPrefab == null) return false;
        for (int i = 0; i < NetworkManager.singleton.spawnPrefabs.Count; ++i)
        {
            GameObject prefab = NetworkManager.singleton.spawnPrefabs[i];
            if (prefab == targetPrefab) return true;
        }

        return false;
    }

    #region Create Item
    public void CreateItem(string deviceIdOrPrefabName, Vector3 position, Quaternion rotation, Vector3 localPositionOffset, Vector3 localRotationOffset)
    {
        GameObject prefab = findSpawnPrefab(deviceIdOrPrefabName);
        if (prefab != null)
        {
            var g = Instantiate(prefab, position, rotation);
            g.transform.Translate(localPositionOffset, Space.Self);
            g.transform.Rotate(localRotationOffset, Space.Self);
            NetworkServer.Spawn(g);
            g.transform.parent = GameObject.Find("PatchAnchor").transform;

            Debug.Log($"{deviceIdOrPrefabName} spawned by the host");
        }
    }

    public void CreateItem(string deviceIdOrPrefabName, Vector3 position, Quaternion rotation, Vector3 localPositionOffset, Vector3 localRotationOffset, manipulator manip)
    {
        GameObject prefab = findSpawnPrefab(deviceIdOrPrefabName);
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

            Debug.Log($"{deviceIdOrPrefabName} spawned by the host into manipulatorObject");
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdCreateItem(string deviceIdOrPrefabName, Vector3 position, Quaternion rotation, Vector3 localPositionOffset, Vector3 localRotationOffset, NetworkIdentity player, bool isLeftHand)
    {
        GameObject prefab = findSpawnPrefab(deviceIdOrPrefabName);
        if (prefab != null)
        {
            var g = Instantiate(prefab, position, rotation);
            g.transform.Translate(localPositionOffset, Space.Self);
            g.transform.Rotate(localRotationOffset, Space.Self);
            NetworkServer.Spawn(g, player.connectionToClient);
            g.transform.parent = GameObject.Find("PatchAnchor").transform;

            //todo send local player the info of the prefab and set grabbed
            Debug.Log($"{player.gameObject} spawned {deviceIdOrPrefabName}");
            if (player.TryGetComponent<VRNetworkPlayer>(out VRNetworkPlayer networkPlayer))
            {
                networkPlayer.GrabNewObjectByHand(g, isLeftHand);
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdCreateItem(string deviceIdOrPrefabName, Vector3 position, Quaternion rotation, Vector3 localPositionOffset, Vector3 localRotationOffset)
    {
        GameObject prefab = findSpawnPrefab(deviceIdOrPrefabName);
        if (prefab != null)
        {
            var g = Instantiate(prefab, position, rotation);
            g.transform.Translate(localPositionOffset, Space.Self);
            g.transform.Rotate(localRotationOffset, Space.Self);
            NetworkServer.Spawn(g);
            g.transform.parent = GameObject.Find("PatchAnchor").transform;

            Debug.Log($"Spawned {deviceIdOrPrefabName}");
        }
    }

    GameObject findSpawnPrefab(string deviceIdOrPrefabName)
    {
        if (NetworkManager.singleton == null || NetworkManager.singleton.spawnPrefabs == null) return null;

        string prefabName = deviceIdOrPrefabName;
        GameObject registeredPrefab = null;
        if (OSLDeviceRegistry.TryGet(deviceIdOrPrefabName, out OSLDeviceRegistration registration))
        {
            if (!registration.IsAvailable)
            {
                Debug.LogWarning("OpenSoundLab: Cannot spawn unavailable device " + registration.canonicalDeviceId + " (" + registration.Access.availability + ").");
                return null;
            }

            registeredPrefab = registration.LoadPrefab();
            if (registeredPrefab != null) prefabName = registeredPrefab.name;
        }

        for (int i = 0; i < NetworkManager.singleton.spawnPrefabs.Count; ++i)
        {
            GameObject prefab = NetworkManager.singleton.spawnPrefabs[i];
            if (prefab == null) continue;
            if (registeredPrefab != null && prefab == registeredPrefab) return prefab;
            if (registeredPrefab == null && prefab.name == prefabName) return prefab;
        }

        Debug.LogWarning("OpenSoundLab: Missing network spawn prefab for " + deviceIdOrPrefabName + ".");
        return null;
    }

    #endregion

    #region Duplicate Object

    public void DuplicateItem(GameObject obj, manipulator manip)
    {
        var g = SaveLoadInterface.instance.Copy(obj, manip);
        if (g == null) return;
        NetworkServer.Spawn(g);
        g.transform.parent = GameObject.Find("PatchAnchor").transform;
        Debug.Log($"{g} duplicated by the host");
    }

    [Command(requiresAuthority = false)]
    public void CmdDuplicateItem(NetworkIdentity obj, NetworkIdentity player, bool isLeftHand)
    {
        var g = SaveLoadInterface.instance.Copy(obj.gameObject, null);
        if (g == null) return;
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
