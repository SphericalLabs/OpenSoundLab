using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Network
{
    public static class OSLNetworkSpawnPrefabRegistrar
    {
        public static void RegisterManifestPrefabs(NetworkManager networkManager)
        {
            if (networkManager == null || networkManager.spawnPrefabs == null) return;

            List<GameObject> registryPrefabs = OSLDeviceRegistry.GetNetworkPrefabs();
            for (int i = 0; i < registryPrefabs.Count; ++i)
            {
                GameObject prefab = registryPrefabs[i];
                if (prefab == null || hasSpawnPrefab(networkManager, prefab)) continue;

                networkManager.spawnPrefabs.Add(prefab);
            }
        }

        static bool hasSpawnPrefab(NetworkManager networkManager, GameObject targetPrefab)
        {
            if (networkManager == null || targetPrefab == null) return false;

            NetworkIdentity targetIdentity = targetPrefab.GetComponent<NetworkIdentity>();
            uint targetAssetId = targetIdentity != null ? targetIdentity.assetId : 0;

            for (int i = 0; i < networkManager.spawnPrefabs.Count; ++i)
            {
                GameObject prefab = networkManager.spawnPrefabs[i];
                if (prefab == null) continue;
                if (prefab == targetPrefab) return true;

                if (targetAssetId == 0) continue;
                NetworkIdentity identity = prefab.GetComponent<NetworkIdentity>();
                if (identity != null && identity.assetId == targetAssetId) return true;
            }

            return false;
        }
    }
}
