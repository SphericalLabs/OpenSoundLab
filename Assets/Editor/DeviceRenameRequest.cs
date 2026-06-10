using UnityEngine;

internal enum DeviceRenameMode
{
    RenameDevice,
    RenameCollection,
    MoveDeviceToCollection
}

internal sealed class DeviceRenameRequest
{
    public DeviceRenameMode mode;
    public OSLDeviceCollectionManifest sourceCollectionManifest;
    public OSLDeviceCollectionManifest targetCollectionManifest;
    public int deviceIndex;
    public string makerDomain;
    public string collectionLocalId;
    public string collectionDisplayName;
    public string localId;
    public string displayName;
    public DeviceCategory category;
    public bool renameAssets;
    public bool rewriteScripts;
    public bool addLegacyAliases;
}
