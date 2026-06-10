using System.Collections.Generic;

public static class OSLDeviceEntitlements
{
    static readonly List<string> ownedStoreSkus = new List<string>();
    static readonly List<string> hostSessionDeviceIds = new List<string>();
    static readonly List<string> hostSessionCollectionIds = new List<string>();

    public static string[] OwnedStoreSkus => ownedStoreSkus.ToArray();
    public static string[] HostSessionDeviceIds => hostSessionDeviceIds.ToArray();
    public static string[] HostSessionCollectionIds => hostSessionCollectionIds.ToArray();

    public static void SetOwnedStoreSkus(IEnumerable<string> storeSkus)
    {
        ownedStoreSkus.Clear();
        AddOwnedStoreSkus(storeSkus);
    }

    public static void AddOwnedStoreSku(string storeSku)
    {
        AddUnique(ownedStoreSkus, storeSku);
    }

    public static void AddOwnedStoreSkus(IEnumerable<string> storeSkus)
    {
        if (storeSkus == null) return;
        foreach (string storeSku in storeSkus)
        {
            AddOwnedStoreSku(storeSku);
        }
    }

    public static void ClearOwnedStoreSkus()
    {
        ownedStoreSkus.Clear();
    }

    public static bool HasOwnedStoreSku(string[] storeSkus)
    {
        return ContainsAny(ownedStoreSkus, storeSkus);
    }

    public static void SetHostSessionCollections(IEnumerable<string> collectionIds)
    {
        hostSessionCollectionIds.Clear();
        AddHostSessionCollections(collectionIds);
    }

    public static void SetHostSessionDevices(IEnumerable<string> canonicalDeviceIds)
    {
        hostSessionDeviceIds.Clear();
        AddHostSessionDevices(canonicalDeviceIds);
    }

    public static void AddHostSessionDevice(string canonicalDeviceId)
    {
        AddUnique(hostSessionDeviceIds, canonicalDeviceId);
    }

    public static void AddHostSessionDevices(IEnumerable<string> canonicalDeviceIds)
    {
        if (canonicalDeviceIds == null) return;
        foreach (string canonicalDeviceId in canonicalDeviceIds)
        {
            AddHostSessionDevice(canonicalDeviceId);
        }
    }

    public static void ClearHostSessionDevices()
    {
        hostSessionDeviceIds.Clear();
    }

    public static bool HasHostSessionDevice(string canonicalDeviceId)
    {
        if (string.IsNullOrWhiteSpace(canonicalDeviceId)) return false;
        return hostSessionDeviceIds.Contains(canonicalDeviceId);
    }

    public static void AddHostSessionCollection(string collectionId)
    {
        AddUnique(hostSessionCollectionIds, collectionId);
    }

    public static void AddHostSessionCollections(IEnumerable<string> collectionIds)
    {
        if (collectionIds == null) return;
        foreach (string collectionId in collectionIds)
        {
            AddHostSessionCollection(collectionId);
        }
    }

    public static void ClearHostSessionCollections()
    {
        hostSessionCollectionIds.Clear();
    }

    public static bool HasHostSessionCollection(string collectionId)
    {
        if (string.IsNullOrWhiteSpace(collectionId)) return false;
        return hostSessionCollectionIds.Contains(collectionId);
    }

    static bool ContainsAny(List<string> knownValues, string[] candidateValues)
    {
        if (knownValues == null || candidateValues == null) return false;

        for (int i = 0; i < candidateValues.Length; ++i)
        {
            string candidateValue = candidateValues[i];
            if (!string.IsNullOrWhiteSpace(candidateValue) && knownValues.Contains(candidateValue)) return true;
        }

        return false;
    }

    static void AddUnique(List<string> values, string value)
    {
        if (values == null || string.IsNullOrWhiteSpace(value)) return;
        if (!values.Contains(value)) values.Add(value);
    }
}
