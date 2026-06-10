public enum OSLDeviceAvailability
{
    Available,
    Locked,
    Missing
}

public enum OSLDeviceAccessReason
{
    Included,
    Owned,
    LocalDeveloperOnly,
    HostSessionOwned
}

public struct OSLDeviceAccess
{
    public OSLDeviceAvailability availability;
    public OSLDeviceAccessReason reason;
    public string detail;

    public bool IsAvailable => availability == OSLDeviceAvailability.Available;

    public static OSLDeviceAccess Available(OSLDeviceAccessReason reason, string detail = "")
    {
        return new OSLDeviceAccess
        {
            availability = OSLDeviceAvailability.Available,
            reason = reason,
            detail = detail
        };
    }

    public static OSLDeviceAccess Locked(string detail = "")
    {
        return new OSLDeviceAccess
        {
            availability = OSLDeviceAvailability.Locked,
            reason = OSLDeviceAccessReason.Included,
            detail = detail
        };
    }

    public static OSLDeviceAccess Missing(string detail = "")
    {
        return new OSLDeviceAccess
        {
            availability = OSLDeviceAvailability.Missing,
            reason = OSLDeviceAccessReason.Included,
            detail = detail
        };
    }
}

public static class OSLDeviceAccessResolver
{
    public static OSLDeviceAccess Resolve(OSLDeviceRegistration registration)
    {
        if (registration == null || registration.collection == null)
        {
            return OSLDeviceAccess.Missing("Device is not registered.");
        }

        OSLDeviceCollectionRegistration collection = registration.collection;
        if (IsDeveloperOnlyCollection(collection))
        {
            if (OSLDeviceBuildPolicy.AllowsLocalDeveloperContent)
            {
                return OSLDeviceAccess.Available(OSLDeviceAccessReason.LocalDeveloperOnly, "Local developer collection is enabled for this app id.");
            }

            return OSLDeviceAccess.Missing("Local developer collection is hidden for this app id.");
        }

        if (!registration.RequiresEntitlement)
        {
            return OSLDeviceAccess.Available(OSLDeviceAccessReason.Included, "Device does not require entitlement.");
        }

        if (OSLDeviceEntitlements.HasOwnedStoreSku(registration.StoreSkus))
        {
            return OSLDeviceAccess.Available(OSLDeviceAccessReason.Owned, "Owned Store product unlocks this device.");
        }

        if (OSLDeviceEntitlements.HasHostSessionDevice(registration.canonicalDeviceId) ||
            OSLDeviceEntitlements.HasHostSessionCollection(collection.collectionId))
        {
            return OSLDeviceAccess.Available(OSLDeviceAccessReason.HostSessionOwned, "Host session unlocks this device temporarily.");
        }

        return OSLDeviceAccess.Locked("Device requires entitlement.");
    }

    public static bool IsDeveloperOnlyCollection(OSLDeviceCollectionRegistration collection)
    {
        if (collection == null) return false;
        return collection.source == OSLCollectionSource.Local || collection.source == OSLCollectionSource.Package;
    }

    public static bool IsDeveloperOnlyCollection(OSLDeviceCollectionManifest manifest)
    {
        if (manifest == null) return false;
        return manifest.source == OSLCollectionSource.Local || manifest.source == OSLCollectionSource.Package;
    }
}
