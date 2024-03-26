using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkSpawnManager : NetworkBehaviour
{
    public static NetworkSpawnManager Instance;

    private void Start()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }


    public void CreatItem(string prefabName, Vector3 position, Quaternion rotation, Vector3 localPositionOffset, Vector3 localRotationOffset)
    {
        var prefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == prefabName);
        if (prefab != null)
        {
            var g = Instantiate(prefab, position, rotation);
            g.transform.Translate(localPositionOffset, Space.Self);
            g.transform.Rotate(localRotationOffset, Space.Self);
            NetworkServer.Spawn(g);

            Debug.Log($"{prefabName} spawned by the host");
        }
    }

    public void CreatItem(string prefabName, Vector3 position, Quaternion rotation, Vector3 localPositionOffset, Vector3 localRotationOffset, manipulator manip)
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

    [Command (requiresAuthority = false)]
    public void CmdCreatItem(string prefabName, Vector3 position, Quaternion rotation, Vector3 localPositionOffset, Vector3 localRotationOffset, NetworkIdentity player, bool isLeftHand)
    {
        var prefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == prefabName);
        if (prefab != null)
        {
            var g = Instantiate(prefab, position, rotation);
            g.transform.Translate(localPositionOffset, Space.Self);
            g.transform.Rotate(localRotationOffset, Space.Self);
            NetworkServer.Spawn(g, player.connectionToClient);

            //todo send local player the info of the prefab and set grabed
            Debug.Log($"{player.gameObject} spawned {prefabName}");
            if (player.TryGetComponent<VRNetworkPlayer>(out VRNetworkPlayer networkPlayer))
            {
                networkPlayer.GrabNewObjectByHand(g, isLeftHand);
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdCreatItem(string prefabName, Vector3 position, Quaternion rotation, Vector3 localPositionOffset, Vector3 localRotationOffset)
    {
        var prefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == prefabName);
        if (prefab != null)
        {
            var g = Instantiate(prefab, position, rotation);
            g.transform.Translate(localPositionOffset, Space.Self);
            g.transform.Rotate(localRotationOffset, Space.Self);
            NetworkServer.Spawn(g);

            Debug.Log($"Spawned {prefabName}");
        }
    }
}
