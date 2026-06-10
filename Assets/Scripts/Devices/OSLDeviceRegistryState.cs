using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class OSLDeviceRegistryState
{
    readonly List<OSLDeviceCollectionRegistration> collections = new List<OSLDeviceCollectionRegistration>();
    readonly List<OSLDeviceRegistration> registrations = new List<OSLDeviceRegistration>();
    readonly OSLDeviceLookupCache lookupCache = new OSLDeviceLookupCache();
    readonly OSLDeviceManifestLoader manifestLoader = new OSLDeviceManifestLoader();
    readonly OSLDeviceNetworkPrefabCache networkPrefabCache = new OSLDeviceNetworkPrefabCache();
    bool loaded;

    public int CollectionCount => collections.Count;
    public int RegistrationCount => registrations.Count;

    public void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        InvalidateAllCaches();
        manifestLoader.LoadCollectionManifests(RegisterCollectionManifest);
    }

    public void Refresh()
    {
        loaded = false;
        InvalidateAllCaches();
        EnsureLoaded();
    }

    public List<OSLDeviceCollectionRegistration> GetCollections()
    {
        EnsureLoaded();
        return new List<OSLDeviceCollectionRegistration>(collections);
    }

    public List<OSLDeviceRegistration> GetAll(bool sortAlphabetically)
    {
        EnsureLoaded();
        List<OSLDeviceRegistration> result = new List<OSLDeviceRegistration>(registrations);
        SortRegistrations(result, sortAlphabetically);
        return result;
    }

    public List<OSLDeviceRegistration> GetAllByCategory(DeviceCategory category, bool sortAlphabetically)
    {
        EnsureLoaded();
        List<OSLDeviceRegistration> filteredRegistrations = new List<OSLDeviceRegistration>();

        for (int i = 0; i < registrations.Count; ++i)
        {
            OSLDeviceRegistration registration = registrations[i];
            if (registration != null && registration.Category == category) filteredRegistrations.Add(registration);
        }

        SortRegistrations(filteredRegistrations, sortAlphabetically);
        return filteredRegistrations;
    }

    public bool TryGetCollection(string collectionId, out OSLDeviceCollectionRegistration collection)
    {
        EnsureLoaded();
        return lookupCache.TryGetCollection(collectionId, out collection);
    }

    public bool TryGet(string deviceId, out OSLDeviceRegistration registration)
    {
        EnsureLoaded();
        return lookupCache.TryGet(deviceId, out registration);
    }

    public bool TryGetByCanonicalDeviceId(string canonicalDeviceId, out OSLDeviceRegistration registration)
    {
        EnsureLoaded();
        return lookupCache.TryGetByCanonicalDeviceId(canonicalDeviceId, out registration);
    }

    public bool TryGetByDataTypeName(string dataTypeName, out OSLDeviceRegistration registration)
    {
        EnsureLoaded();
        return lookupCache.TryGetByDataTypeName(dataTypeName, out registration);
    }

    public bool TryGetByLegacyDataTypeName(string dataTypeName, out OSLDeviceRegistration registration)
    {
        EnsureLoaded();
        return lookupCache.TryGetByLegacyDataTypeName(dataTypeName, out registration);
    }

    public bool TryGetByLegacyXmlRootName(string xmlRootName, out OSLDeviceRegistration registration)
    {
        EnsureLoaded();
        return lookupCache.TryGetByLegacyXmlRootName(xmlRootName, out registration);
    }

    public bool TryGetByPrefabResourcePath(string prefabResourcePath, out OSLDeviceRegistration registration)
    {
        EnsureLoaded();
        return lookupCache.TryGetByPrefabResourcePath(prefabResourcePath, out registration);
    }

    public bool TryGetByPrefabName(string prefabName, out OSLDeviceRegistration registration)
    {
        EnsureLoaded();
        return lookupCache.TryGetByPrefabName(prefabName, out registration);
    }

    public bool TryGetByStoreSku(string storeSku, out OSLDeviceRegistration registration)
    {
        EnsureLoaded();
        return lookupCache.TryGetByStoreSku(storeSku, out registration);
    }

    public List<OSLDeviceRegistration> GetByStoreSku(string storeSku)
    {
        EnsureLoaded();
        return lookupCache.GetByStoreSku(storeSku);
    }

    public bool TryGetCanonicalDeviceIdForDataType(Type dataType, out string canonicalDeviceId)
    {
        EnsureLoaded();
        return lookupCache.TryGetCanonicalDeviceIdForDataType(dataType, out canonicalDeviceId);
    }

    public List<GameObject> GetNetworkPrefabs()
    {
        EnsureLoaded();
        return networkPrefabCache.GetNetworkPrefabs(registrations);
    }

    public Type[] GetInstrumentDataTypes()
    {
        EnsureLoaded();
        return lookupCache.GetInstrumentDataTypes(registrations);
    }

    void InvalidateAllCaches()
    {
        collections.Clear();
        registrations.Clear();
        lookupCache.Clear();
        networkPrefabCache.Invalidate();
    }

    void RegisterCollectionManifest(OSLDeviceCollectionManifest manifest)
    {
        if (manifest == null || !manifest.enabled) return;
        if (string.IsNullOrWhiteSpace(manifest.collectionId))
        {
            Debug.LogWarning("OpenSoundLab: Skipping device collection manifest without collectionId.");
            return;
        }

        OSLDeviceCollectionRegistration collection = new OSLDeviceCollectionRegistration
        {
            vendorId = manifest.vendorId,
            collectionId = manifest.collectionId,
            resourceSlug = !string.IsNullOrWhiteSpace(manifest.resourceSlug) ? manifest.resourceSlug : manifest.collectionId,
            displayName = manifest.displayName,
            description = manifest.description,
            source = manifest.source,
            enabled = manifest.enabled,
            nativeSymbols = manifest.nativeSymbols != null ? manifest.nativeSymbols : new string[0],
            collectionManifest = manifest
        };

        if (!RegisterCollection(collection)) return;
        if (manifest.devices == null) return;

        for (int i = 0; i < manifest.devices.Length; ++i)
        {
            OSLDeviceRegistration registration = CreateCollectionRegistration(collection, manifest.devices[i]);
            RegisterDevice(collection, registration);
        }
    }

    bool RegisterCollection(OSLDeviceCollectionRegistration collection)
    {
        if (!lookupCache.RegisterCollection(collection)) return false;
        collections.Add(collection);
        return true;
    }

    void RegisterDevice(OSLDeviceCollectionRegistration collection, OSLDeviceRegistration registration)
    {
        if (collection == null || registration == null || !registration.enabled) return;

        registrations.Add(registration);
        collection.devices.Add(registration);
        lookupCache.RegisterDevice(registration);
        networkPrefabCache.Invalidate();
    }

    OSLDeviceRegistration CreateCollectionRegistration(OSLDeviceCollectionRegistration collection, OSLDeviceDefinition definition)
    {
        if (collection == null || definition == null || !definition.enabled) return null;

        string deviceLocalId = definition.deviceLocalId;
        if (string.IsNullOrWhiteSpace(deviceLocalId)) deviceLocalId = OSLDeviceIdentityCompatibility.GetLocalIdFromCanonicalId(definition.canonicalDeviceId);
        if (string.IsNullOrWhiteSpace(deviceLocalId))
        {
            Debug.LogWarning("OpenSoundLab: Skipping device definition without deviceLocalId in collection " + collection.collectionId + ".");
            return null;
        }

        string canonicalDeviceId = definition.canonicalDeviceId;
        if (string.IsNullOrWhiteSpace(canonicalDeviceId)) canonicalDeviceId = OSLDeviceIdentityCompatibility.BuildCanonicalDeviceId(collection.collectionId, deviceLocalId);
        string deviceId = collection.source == OSLCollectionSource.Core ? deviceLocalId : canonicalDeviceId;
        string deviceTypeName = collection.source == OSLCollectionSource.Core ? deviceLocalId : canonicalDeviceId;

        DeviceType deviceType = DeviceType.Register(deviceTypeName, definition.category, definition.order, collection.source != OSLCollectionSource.Core, collection.collectionId);
        OSLDeviceRegistration registration = new OSLDeviceRegistration
        {
            deviceType = deviceType,
            vendorId = collection.vendorId,
            collectionId = collection.collectionId,
            resourceSlug = collection.resourceSlug,
            canonicalDeviceId = canonicalDeviceId,
            deviceLocalId = deviceLocalId,
            deviceId = deviceId,
            displayName = !string.IsNullOrWhiteSpace(definition.displayName) ? definition.displayName : deviceLocalId,
            category = definition.category,
            tags = definition.tags != null ? definition.tags : new string[0],
            order = definition.order,
            source = collection.source == OSLCollectionSource.Core ? OSLDeviceSource.Core : OSLDeviceSource.Addon,
            providerId = collection.collectionId,
            enabled = definition.enabled,
            showInMenu = definition.showInMenu,
            registerForNetworkSpawn = definition.showInMenu,
            productInfo = definition.productInfo != null ? definition.productInfo : new OSLDeviceProductInfo(),
            nativeSymbols = collection.nativeSymbols != null ? collection.nativeSymbols : new string[0],
            prefabResourcePath = definition.prefabResourcePath,
            menuPrefabResourcePath = definition.menuPrefabResourcePath,
            symbolResourcePath = definition.symbolResourcePath,
            dataTypeName = definition.dataTypeName,
            legacyDeviceIds = definition.legacyDeviceIds != null ? definition.legacyDeviceIds : new string[0],
            legacyDataTypeNames = definition.legacyDataTypeNames != null ? definition.legacyDataTypeNames : new string[0],
            legacyXmlRootNames = definition.legacyXmlRootNames != null ? definition.legacyXmlRootNames : new string[0],
            previewPositionOffset = definition.previewPositionOffset,
            previewRotationOffset = definition.previewRotationOffset,
            previewScale = definition.previewScale,
            spawnPositionOffset = definition.spawnPositionOffset,
            spawnRotationOffset = definition.spawnRotationOffset,
            hasSpawnOffsets = true,
            collection = collection,
            collectionManifest = collection.collectionManifest,
            definition = definition
        };

        registration.dataTypes = lookupCache.BuildDataTypes(registration.dataTypeName, registration.legacyDataTypeNames);
        registration.dataType = registration.dataTypes.Length > 0 ? registration.dataTypes[0] : null;
        return registration;
    }

    static void SortRegistrations(List<OSLDeviceRegistration> sortableRegistrations, bool sortAlphabetically)
    {
        if (sortAlphabetically)
        {
            sortableRegistrations.Sort((left, right) => string.Compare(left.deviceId, right.deviceId, StringComparison.OrdinalIgnoreCase));
            return;
        }

        sortableRegistrations.Sort((left, right) =>
        {
            int categoryCompare = left.Category.CompareTo(right.Category);
            if (categoryCompare != 0) return categoryCompare;

            int orderCompare = left.Order.CompareTo(right.Order);
            if (orderCompare != 0) return orderCompare;

            return string.Compare(left.deviceId, right.deviceId, StringComparison.OrdinalIgnoreCase);
        });
    }
}
