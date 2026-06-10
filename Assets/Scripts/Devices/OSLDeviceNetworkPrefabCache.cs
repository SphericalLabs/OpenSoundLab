using System.Collections.Generic;
using UnityEngine;

internal sealed class OSLDeviceNetworkPrefabCache
{
    readonly List<GameObject> prefabs = new List<GameObject>();
    bool loaded;

    public void Invalidate()
    {
        loaded = false;
        prefabs.Clear();
    }

    public List<GameObject> GetNetworkPrefabs(List<OSLDeviceRegistration> registrations)
    {
        if (!loaded) Build(registrations);
        return new List<GameObject>(prefabs);
    }

    void Build(List<OSLDeviceRegistration> registrations)
    {
        loaded = true;
        prefabs.Clear();
        if (registrations == null) return;

        for (int i = 0; i < registrations.Count; ++i)
        {
            OSLDeviceRegistration registration = registrations[i];
            if (registration == null || !registration.registerForNetworkSpawn || !registration.IsAvailable) continue;

            GameObject prefab = registration.LoadPrefab();
            if (prefab != null && prefab.GetComponent("NetworkIdentity") != null) prefabs.Add(prefab);
        }
    }
}
