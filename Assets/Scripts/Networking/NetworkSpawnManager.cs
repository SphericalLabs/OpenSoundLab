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

    [Command (requiresAuthority = false)]
    public void CmdCreatItem(string prefabName, Vector3 position, Quaternion rotation, Vector3 localPositionOffset, Vector3 localRotationOffset, NetworkIdentity player)
    {
        var prefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == "prefabName");
        if (prefab != null)
        {
            var g = Instantiate(prefab, position, rotation);
            g.transform.Translate(localPositionOffset, Space.Self);
            g.transform.Rotate(localRotationOffset, Space.Self);
            NetworkServer.Spawn(g);

            //todo send local player the info of the prefab and set grabed
            Debug.Log($"{player.gameObject} spawned this item");
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdCreatItem(string prefabName, Vector3 position, Quaternion rotation, Vector3 localPositionOffset, Vector3 localRotationOffset)
    {
        var prefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.name == "prefabName");
        if (prefab != null)
        {
            var g = Instantiate(prefab, position, rotation);
            g.transform.Translate(localPositionOffset, Space.Self);
            g.transform.Rotate(localRotationOffset, Space.Self);
            NetworkServer.Spawn(g);
        }
    }


    //general networking actions
    [Command]
    public void CmdGetObjectAuthority(NetworkIdentity item)
    {
        if (item.connectionToClient != null)
            item.RemoveClientAuthority();
        item.AssignClientAuthority(connectionToClient);
    }

    [Command]
    public void CmdRemoveObjectAuthority(NetworkIdentity item)
    {
        item.RemoveClientAuthority();
    }

    [Command]
    public void CmdDestroyObjectOnServer(GameObject target)
    {
        NetworkServer.Destroy(target);
    }
}
