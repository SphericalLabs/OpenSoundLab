using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class OSLDeviceLookupCache
{
    readonly Dictionary<string, OSLDeviceCollectionRegistration> collectionById = new Dictionary<string, OSLDeviceCollectionRegistration>();
    readonly Dictionary<string, List<OSLDeviceRegistration>> registrationsByStoreSku = new Dictionary<string, List<OSLDeviceRegistration>>();
    readonly Dictionary<string, OSLDeviceRegistration> registrationByCanonicalDeviceId = new Dictionary<string, OSLDeviceRegistration>();
    readonly Dictionary<string, OSLDeviceRegistration> registrationByDeviceLocalId = new Dictionary<string, OSLDeviceRegistration>();
    readonly Dictionary<string, OSLDeviceRegistration> registrationByLegacyDeviceId = new Dictionary<string, OSLDeviceRegistration>();
    readonly Dictionary<string, OSLDeviceRegistration> registrationByDataTypeName = new Dictionary<string, OSLDeviceRegistration>();
    readonly Dictionary<string, OSLDeviceRegistration> registrationByLegacyDataTypeName = new Dictionary<string, OSLDeviceRegistration>();
    readonly Dictionary<string, OSLDeviceRegistration> registrationByLegacyXmlRootName = new Dictionary<string, OSLDeviceRegistration>();
    readonly Dictionary<string, OSLDeviceRegistration> registrationByPrefabResourcePath = new Dictionary<string, OSLDeviceRegistration>();
    readonly Dictionary<string, OSLDeviceRegistration> registrationByPrefabName = new Dictionary<string, OSLDeviceRegistration>();

    public void Clear()
    {
        collectionById.Clear();
        registrationsByStoreSku.Clear();
        registrationByCanonicalDeviceId.Clear();
        registrationByDeviceLocalId.Clear();
        registrationByLegacyDeviceId.Clear();
        registrationByDataTypeName.Clear();
        registrationByLegacyDataTypeName.Clear();
        registrationByLegacyXmlRootName.Clear();
        registrationByPrefabResourcePath.Clear();
        registrationByPrefabName.Clear();
    }

    public bool RegisterCollection(OSLDeviceCollectionRegistration collection)
    {
        if (collection == null || string.IsNullOrWhiteSpace(collection.collectionId)) return false;
        if (collectionById.ContainsKey(collection.collectionId))
        {
            Debug.LogWarning("OpenSoundLab: Skipping duplicate device collection " + collection.collectionId + ".");
            return false;
        }

        collectionById[collection.collectionId] = collection;

        return true;
    }

    public void RegisterDevice(OSLDeviceRegistration registration)
    {
        if (registration == null) return;

        AddRegistrationLookup(registrationByCanonicalDeviceId, registration.canonicalDeviceId, registration, "canonicalDeviceId");
        AddDeviceLocalIdLookup(registration.deviceLocalId, registration);
        AddRegistrationLookup(registrationByDataTypeName, registration.dataTypeName, registration, "dataTypeName");
        AddRegistrationLookup(registrationByPrefabResourcePath, registration.prefabResourcePath, registration, "prefabResourcePath");
        AddRegistrationLookup(registrationByPrefabName, OSLDeviceIdentityCompatibility.GetResourceName(registration.prefabResourcePath), registration, "prefabName");
        AddRegistrationListLookups(registrationsByStoreSku, registration.StoreSkus, registration);

        AddRegistrationArrayLookup(registrationByLegacyDeviceId, registration.legacyDeviceIds, registration, "legacyDeviceId");
        AddRegistrationArrayLookup(registrationByLegacyDataTypeName, registration.legacyDataTypeNames, registration, "legacyDataTypeName");
        AddRegistrationArrayLookup(registrationByLegacyXmlRootName, registration.legacyXmlRootNames, registration, "legacyXmlRootName");
        AddGeneratedXmlRootLookups(registration);
    }

    public bool TryGetCollection(string collectionId, out OSLDeviceCollectionRegistration collection)
    {
        return TryGetCollectionLookup(collectionById, collectionId, out collection);
    }

    public bool TryGet(string deviceId, out OSLDeviceRegistration registration)
    {
        if (TryGetRegistrationLookup(registrationByCanonicalDeviceId, deviceId, out registration)) return true;
        if (TryGetRegistrationLookup(registrationByDeviceLocalId, deviceId, out registration)) return true;
        if (TryGetRegistrationLookup(registrationByLegacyDeviceId, deviceId, out registration)) return true;
        if (TryGetRegistrationLookup(registrationByPrefabResourcePath, deviceId, out registration)) return true;
        if (TryGetRegistrationLookup(registrationByPrefabName, deviceId, out registration)) return true;
        return false;
    }

    public bool TryGetByCanonicalDeviceId(string canonicalDeviceId, out OSLDeviceRegistration registration)
    {
        return TryGetRegistrationLookup(registrationByCanonicalDeviceId, canonicalDeviceId, out registration);
    }

    public bool TryGetByDataTypeName(string dataTypeName, out OSLDeviceRegistration registration)
    {
        return TryGetRegistrationLookup(registrationByDataTypeName, dataTypeName, out registration);
    }

    public bool TryGetByLegacyDataTypeName(string dataTypeName, out OSLDeviceRegistration registration)
    {
        return TryGetRegistrationLookup(registrationByLegacyDataTypeName, dataTypeName, out registration);
    }

    public bool TryGetByLegacyXmlRootName(string xmlRootName, out OSLDeviceRegistration registration)
    {
        return TryGetRegistrationLookup(registrationByLegacyXmlRootName, xmlRootName, out registration);
    }

    public bool TryGetByPrefabResourcePath(string prefabResourcePath, out OSLDeviceRegistration registration)
    {
        return TryGetRegistrationLookup(registrationByPrefabResourcePath, prefabResourcePath, out registration);
    }

    public bool TryGetByPrefabName(string prefabName, out OSLDeviceRegistration registration)
    {
        return TryGetRegistrationLookup(registrationByPrefabName, prefabName, out registration);
    }

    public bool TryGetByStoreSku(string storeSku, out OSLDeviceRegistration registration)
    {
        registration = null;
        List<OSLDeviceRegistration> registrations = GetByStoreSku(storeSku);
        if (registrations.Count == 0) return false;
        registration = registrations[0];
        return true;
    }

    public List<OSLDeviceRegistration> GetByStoreSku(string storeSku)
    {
        if (string.IsNullOrWhiteSpace(storeSku)) return new List<OSLDeviceRegistration>();
        if (!registrationsByStoreSku.TryGetValue(storeSku, out List<OSLDeviceRegistration> registrations) || registrations == null)
        {
            return new List<OSLDeviceRegistration>();
        }

        return new List<OSLDeviceRegistration>(registrations);
    }

    public bool TryGetCanonicalDeviceIdForDataType(Type dataType, out string canonicalDeviceId)
    {
        canonicalDeviceId = "";
        if (dataType == null) return false;

        OSLDeviceRegistration registration;
        if (!string.IsNullOrWhiteSpace(dataType.FullName) && TryGetByDataTypeName(dataType.FullName, out registration))
        {
            canonicalDeviceId = registration.canonicalDeviceId;
            return !string.IsNullOrWhiteSpace(canonicalDeviceId);
        }

        if (TryGetByDataTypeName(dataType.Name, out registration))
        {
            canonicalDeviceId = registration.canonicalDeviceId;
            return !string.IsNullOrWhiteSpace(canonicalDeviceId);
        }

        return false;
    }

    public Type[] BuildDataTypes(string dataTypeName, string[] legacyDataTypeNames)
    {
        List<Type> types = new List<Type>();
        AddInstrumentDataType(types, FindType(dataTypeName));

        if (legacyDataTypeNames != null)
        {
            for (int i = 0; i < legacyDataTypeNames.Length; ++i)
            {
                AddInstrumentDataType(types, FindType(legacyDataTypeNames[i]));
            }
        }

        return types.ToArray();
    }

    public Type[] GetInstrumentDataTypes(List<OSLDeviceRegistration> registrations)
    {
        List<Type> types = new List<Type>();

        for (int i = 0; i < registrations.Count; ++i)
        {
            OSLDeviceRegistration registration = registrations[i];
            if (registration == null || registration.dataTypes == null) continue;

            for (int dataTypeIndex = 0; dataTypeIndex < registration.dataTypes.Length; ++dataTypeIndex)
            {
                AddInstrumentDataType(types, registration.dataTypes[dataTypeIndex]);
            }
        }

        return types.ToArray();
    }

    void AddGeneratedXmlRootLookups(OSLDeviceRegistration registration)
    {
        if (registration == null) return;

        if (registration.IsCore)
        {
            AddDeviceIdXmlRootLookups(registration.deviceId, registration);
            AddDeviceIdXmlRootLookups(registration.deviceLocalId, registration);
        }
        else
        {
            AddDeviceIdXmlRootLookups(registration.canonicalDeviceId, registration);
        }

        if (registration.legacyDeviceIds == null) return;
        for (int i = 0; i < registration.legacyDeviceIds.Length; ++i)
        {
            AddDeviceIdXmlRootLookups(registration.legacyDeviceIds[i], registration);
        }
    }

    void AddDeviceLocalIdLookup(string key, OSLDeviceRegistration registration)
    {
        if (registration == null || string.IsNullOrWhiteSpace(key)) return;
        if (!registrationByDeviceLocalId.TryGetValue(key, out OSLDeviceRegistration existing))
        {
            registrationByDeviceLocalId[key] = registration;
            return;
        }

        if (existing == registration) return;
        if (existing != null && existing.IsCore && !registration.IsCore)
        {
            Debug.LogWarning("OpenSoundLab: Device local id '" + key + "' from " + DescribeRegistration(registration) + " is shadowed by core device " + DescribeRegistration(existing) + ". Use the canonical device id.");
            return;
        }

        if (registration.IsCore)
        {
            registrationByDeviceLocalId[key] = registration;
            Debug.LogWarning("OpenSoundLab: Device local id '" + key + "' from " + DescribeRegistration(existing) + " is shadowed by core device " + DescribeRegistration(registration) + ". Use the canonical device id.");
            return;
        }

        registrationByDeviceLocalId[key] = null;
        Debug.LogWarning("OpenSoundLab: Ambiguous device registry deviceLocalId '" + key + "' between " + DescribeRegistration(existing) + " and " + DescribeRegistration(registration) + ". Use canonical device ids.");
    }

    void AddDeviceIdXmlRootLookups(string deviceId, OSLDeviceRegistration registration)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) return;

        AddRegistrationLookup(registrationByLegacyXmlRootName, deviceId, registration, "legacyXmlRootName");
        AddRegistrationLookup(registrationByLegacyXmlRootName, deviceId + "s", registration, "legacyXmlRootName");
        AddRegistrationLookup(registrationByLegacyXmlRootName, OSLDeviceIdentityCompatibility.PluralizeDeviceId(deviceId), registration, "legacyXmlRootName");
    }

    void AddRegistrationArrayLookup(Dictionary<string, OSLDeviceRegistration> lookup, string[] keys, OSLDeviceRegistration registration, string label)
    {
        if (keys == null) return;
        for (int i = 0; i < keys.Length; ++i)
        {
            AddRegistrationLookup(lookup, keys[i], registration, label);
        }
    }

    void AddRegistrationListLookups(Dictionary<string, List<OSLDeviceRegistration>> lookup, string[] keys, OSLDeviceRegistration registration)
    {
        if (keys == null) return;
        for (int i = 0; i < keys.Length; ++i)
        {
            AddRegistrationListLookup(lookup, keys[i], registration);
        }
    }

    void AddRegistrationListLookup(Dictionary<string, List<OSLDeviceRegistration>> lookup, string key, OSLDeviceRegistration registration)
    {
        if (lookup == null || registration == null || string.IsNullOrWhiteSpace(key)) return;
        if (!lookup.TryGetValue(key, out List<OSLDeviceRegistration> registrations) || registrations == null)
        {
            registrations = new List<OSLDeviceRegistration>();
            lookup[key] = registrations;
        }

        if (!registrations.Contains(registration)) registrations.Add(registration);
    }

    void AddRegistrationLookup(Dictionary<string, OSLDeviceRegistration> lookup, string key, OSLDeviceRegistration registration, string label)
    {
        if (lookup == null || registration == null || string.IsNullOrWhiteSpace(key)) return;
        if (!lookup.TryGetValue(key, out OSLDeviceRegistration existing))
        {
            lookup[key] = registration;
            return;
        }

        if (existing == registration) return;

        lookup[key] = null;
        Debug.LogWarning("OpenSoundLab: Ambiguous device registry " + label + " '" + key + "' between " + DescribeRegistration(existing) + " and " + DescribeRegistration(registration) + ".");
    }

    bool TryGetRegistrationLookup(Dictionary<string, OSLDeviceRegistration> lookup, string key, out OSLDeviceRegistration registration)
    {
        registration = null;
        if (lookup == null || string.IsNullOrWhiteSpace(key)) return false;
        if (!lookup.TryGetValue(key, out registration)) return false;
        return registration != null;
    }

    bool TryGetCollectionLookup(Dictionary<string, OSLDeviceCollectionRegistration> lookup, string key, out OSLDeviceCollectionRegistration collection)
    {
        collection = null;
        if (lookup == null || string.IsNullOrWhiteSpace(key)) return false;
        if (!lookup.TryGetValue(key, out collection)) return false;
        return collection != null;
    }

    static void AddInstrumentDataType(List<Type> types, Type dataType)
    {
        if (types == null || dataType == null || !typeof(InstrumentData).IsAssignableFrom(dataType)) return;
        if (dataType == typeof(InstrumentData)) return;
        if (!types.Contains(dataType)) types.Add(dataType);
    }

    static Type FindType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return null;

        Type directType = Type.GetType(typeName);
        if (directType != null) return directType;

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; ++i)
        {
            Type resolvedType = assemblies[i].GetType(typeName);
            if (resolvedType != null) return resolvedType;
        }

        return null;
    }

    static string DescribeRegistration(OSLDeviceRegistration registration)
    {
        if (registration == null) return "unknown";
        if (!string.IsNullOrWhiteSpace(registration.canonicalDeviceId)) return registration.canonicalDeviceId;
        if (!string.IsNullOrWhiteSpace(registration.deviceId)) return registration.deviceId;
        return "unknown";
    }

}
