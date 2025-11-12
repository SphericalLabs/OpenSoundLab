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
