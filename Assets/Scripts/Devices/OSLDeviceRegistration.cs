using System;
using System.Collections.Generic;
using UnityEngine;

public enum OSLDeviceSource
{
    Core,
    Addon
}

public class OSLDeviceCollectionRegistration
{
    public string vendorId;
    public string collectionId;
    public string resourceSlug;
    public string displayName;
    public string description;
    public OSLCollectionSource source;
    public bool enabled = true;
    public string[] nativeSymbols = new string[0];
    public OSLDeviceCollectionManifest collectionManifest;
    public List<OSLDeviceRegistration> devices = new List<OSLDeviceRegistration>();
}

public class OSLDeviceRegistration
{
    public DeviceType deviceType;
    public string vendorId;
    public string collectionId;
    public string resourceSlug;
    public string canonicalDeviceId;
    public string deviceLocalId;
    public string deviceId;
    public string displayName;
    public DeviceCategory category;
    public string[] tags = new string[0];
    public int order;
    public OSLDeviceSource source;
    public string providerId;
    public bool enabled = true;
    public bool showInMenu = true;
    public bool registerForNetworkSpawn = true;
    public OSLDeviceProductInfo productInfo = new OSLDeviceProductInfo();
    public string[] nativeSymbols = new string[0];
    public string prefabResourcePath;
    public string menuPrefabResourcePath;
    public string symbolResourcePath;
    public string dataTypeName;
    public string[] legacyDeviceIds = new string[0];
    public string[] legacyDataTypeNames = new string[0];
    public string[] legacyXmlRootNames = new string[0];
    public Type dataType;
    public Type[] dataTypes = new Type[0];
    public Vector3 previewPositionOffset;
    public Vector3 previewRotationOffset;
    public Vector3 previewScale = Vector3.one;
    public Vector3 spawnPositionOffset;
    public Vector3 spawnRotationOffset;
    public bool hasSpawnOffsets;
    public OSLDeviceCollectionRegistration collection;
    public OSLDeviceCollectionManifest collectionManifest;
    public OSLDeviceDefinition definition;

    public DeviceCategory Category => category;
    public int Order => order;
    public bool IsAddon => source == OSLDeviceSource.Addon;
    public bool IsCore => source == OSLDeviceSource.Core;
    public bool RequiresEntitlement => productInfo != null && productInfo.requiresEntitlement;
    public bool IsPurchasable => productInfo != null && productInfo.isPurchasable;
    public string StoreSku => productInfo != null ? productInfo.storeSku : "";
    public string[] StoreSkus => BuildStoreSkus(productInfo);
    public OSLDeviceAccess Access => OSLDeviceAccessResolver.Resolve(this);
    public bool IsAvailable => Access.IsAvailable;

    static string[] BuildStoreSkus(OSLDeviceProductInfo info)
    {
        string[] skus = new string[0];
        if (info == null) return skus;

        skus = OSLDeviceIdentityCompatibility.AddUnique(skus, info.storeSku);
        if (info.storeSkus == null) return skus;

        for (int i = 0; i < info.storeSkus.Length; ++i)
        {
            skus = OSLDeviceIdentityCompatibility.AddUnique(skus, info.storeSkus[i]);
        }

        return skus;
    }

    public GameObject LoadPrefab()
    {
        if (!string.IsNullOrWhiteSpace(prefabResourcePath)) return Resources.Load<GameObject>(prefabResourcePath);
        if (string.IsNullOrWhiteSpace(deviceLocalId)) return null;
        return Resources.Load<GameObject>("Prefabs/" + deviceLocalId);
    }

    public GameObject LoadMenuPrefab()
    {
        if (!string.IsNullOrWhiteSpace(menuPrefabResourcePath)) return Resources.Load<GameObject>(menuPrefabResourcePath);
        if (string.IsNullOrWhiteSpace(deviceLocalId)) return null;
        return Resources.Load<GameObject>("MenuPrefabs/" + deviceLocalId + "_Menu");
    }

    public Texture LoadSymbol()
    {
        if (!string.IsNullOrWhiteSpace(symbolResourcePath)) return Resources.Load<Texture>(symbolResourcePath);
        if (string.IsNullOrWhiteSpace(deviceLocalId)) return null;
        return Resources.Load<Texture>("Textures/" + deviceLocalId + "Symbol");
    }

    public bool TryGetSpawnOffsets(out Vector3 localPositionOffset, out Vector3 localRotationOffset)
    {
        localPositionOffset = spawnPositionOffset;
        localRotationOffset = spawnRotationOffset;
        return hasSpawnOffsets;
    }
}
