using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class OSLDeviceRegistry
{
    public const string CoreVendorId = OSLDeviceIdentityCompatibility.CoreVendorId;
    public const string CoreDomain = OSLDeviceIdentityCompatibility.CoreDomain;

    static readonly OSLDeviceRegistryState state = new OSLDeviceRegistryState();

    public static void EnsureLoaded()
    {
        state.EnsureLoaded();
    }

    public static void Refresh()
    {
        state.Refresh();
    }

#if UNITY_EDITOR
    [MenuItem("OpenSoundLab/Devices/Refresh Registry")]
    public static void RefreshFromEditor()
    {
        Refresh();
        Debug.Log("OpenSoundLab: Device registry refreshed. Collections: " + state.CollectionCount + ", devices: " + state.RegistrationCount + ".");
    }
#endif

    public static List<OSLDeviceCollectionRegistration> GetCollections()
    {
        return state.GetCollections();
    }

    public static bool TryGetCollection(string collectionId, out OSLDeviceCollectionRegistration collection)
    {
        return state.TryGetCollection(collectionId, out collection);
    }

    public static List<OSLDeviceRegistration> GetAll(bool sortAlphabetically = false)
    {
        return state.GetAll(sortAlphabetically);
    }

    public static List<OSLDeviceRegistration> GetAllByCategory(DeviceCategory category, bool sortAlphabetically = false)
    {
        return state.GetAllByCategory(category, sortAlphabetically);
    }

    public static bool TryGet(DeviceType deviceType, out OSLDeviceRegistration registration)
    {
        if (deviceType == null)
        {
            registration = null;
            return false;
        }

        return TryGet(deviceType.Name, out registration);
    }

    public static bool TryGet(string deviceId, out OSLDeviceRegistration registration)
    {
        return state.TryGet(deviceId, out registration);
    }

    public static OSLDeviceAccess GetAccess(string deviceId)
    {
        if (!TryGet(deviceId, out OSLDeviceRegistration registration))
        {
            return OSLDeviceAccess.Missing("Device is not registered.");
        }

        return registration.Access;
    }

    public static bool TryGetAvailable(string deviceId, out OSLDeviceRegistration registration)
    {
        if (!TryGet(deviceId, out registration)) return false;
        return registration.IsAvailable;
    }

    public static bool TryGetByCanonicalDeviceId(string canonicalDeviceId, out OSLDeviceRegistration registration)
    {
        return state.TryGetByCanonicalDeviceId(canonicalDeviceId, out registration);
    }

    public static bool TryGetByDataTypeName(string dataTypeName, out OSLDeviceRegistration registration)
    {
        return state.TryGetByDataTypeName(dataTypeName, out registration);
    }

    public static bool TryGetByLegacyDataTypeName(string dataTypeName, out OSLDeviceRegistration registration)
    {
        return state.TryGetByLegacyDataTypeName(dataTypeName, out registration);
    }

    public static bool TryGetByLegacyXmlRootName(string xmlRootName, out OSLDeviceRegistration registration)
    {
        return state.TryGetByLegacyXmlRootName(xmlRootName, out registration);
    }

    public static bool TryGetByPrefabResourcePath(string prefabResourcePath, out OSLDeviceRegistration registration)
    {
        return state.TryGetByPrefabResourcePath(prefabResourcePath, out registration);
    }

    public static bool TryGetByPrefabName(string prefabName, out OSLDeviceRegistration registration)
    {
        return state.TryGetByPrefabName(prefabName, out registration);
    }

    public static bool TryGetByStoreSku(string storeSku, out OSLDeviceRegistration registration)
    {
        return state.TryGetByStoreSku(storeSku, out registration);
    }

    public static List<OSLDeviceRegistration> GetByStoreSku(string storeSku)
    {
        return state.GetByStoreSku(storeSku);
    }

    public static bool TryGetCanonicalDeviceIdForDataType(Type dataType, out string canonicalDeviceId)
    {
        return state.TryGetCanonicalDeviceIdForDataType(dataType, out canonicalDeviceId);
    }

    public static bool TryNormalizeDeviceId(string deviceId, out string normalizedDeviceId)
    {
        normalizedDeviceId = deviceId;
        if (!TryGet(deviceId, out OSLDeviceRegistration registration)) return false;

        normalizedDeviceId = registration.deviceId;
        return true;
    }

    public static bool TryGetCanonicalDeviceId(string deviceId, out string canonicalDeviceId)
    {
        canonicalDeviceId = deviceId;
        if (!TryGet(deviceId, out OSLDeviceRegistration registration)) return false;

        canonicalDeviceId = registration.canonicalDeviceId;
        return !string.IsNullOrWhiteSpace(canonicalDeviceId);
    }

    public static bool ShouldShowInMenu(DeviceType deviceType)
    {
        if (!TryGet(deviceType, out OSLDeviceRegistration registration)) return false;
        return registration.showInMenu && registration.IsAvailable;
    }

    public static string GetDisplayName(DeviceType deviceType)
    {
        if (!TryGet(deviceType, out OSLDeviceRegistration registration)) return "";
        return registration.displayName;
    }

    public static GameObject GetPrefab(DeviceType deviceType)
    {
        if (!TryGet(deviceType, out OSLDeviceRegistration registration)) return null;
        if (!registration.IsAvailable) return null;
        return registration.LoadPrefab();
    }

    public static GameObject GetPrefab(string deviceId)
    {
        if (!TryGet(deviceId, out OSLDeviceRegistration registration)) return null;
        if (!registration.IsAvailable) return null;
        return registration.LoadPrefab();
    }

    public static GameObject GetMenuPrefab(DeviceType deviceType)
    {
        if (!TryGet(deviceType, out OSLDeviceRegistration registration)) return null;
        if (!registration.IsAvailable) return null;
        return registration.LoadMenuPrefab();
    }

    public static Texture GetSymbol(DeviceType deviceType)
    {
        if (!TryGet(deviceType, out OSLDeviceRegistration registration)) return null;
        return registration.LoadSymbol();
    }

    public static bool TryGetSpawnOffsets(string deviceIdOrPrefabName, out Vector3 localPositionOffset, out Vector3 localRotationOffset)
    {
        localPositionOffset = Vector3.zero;
        localRotationOffset = Vector3.zero;

        if (!TryGet(deviceIdOrPrefabName, out OSLDeviceRegistration registration)) return false;
        if (!registration.IsAvailable) return false;
        return registration.TryGetSpawnOffsets(out localPositionOffset, out localRotationOffset);
    }

    public static List<GameObject> GetNetworkPrefabs()
    {
        return state.GetNetworkPrefabs();
    }

    public static Type[] GetInstrumentDataTypes()
    {
        return state.GetInstrumentDataTypes();
    }
}
