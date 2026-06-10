using UnityEngine;

internal enum DeviceScaffoldTargetKind
{
    Core,
    Addon
}

internal sealed class DeviceScaffoldRequest
{
    public DeviceScaffoldTargetKind targetKind;
    public string makerDomain;
    public string collectionLocalId;
    public string localId;
    public string displayName;
    public DeviceCategory category;
    public int order;
    public bool showInMenu;
    public string tags;
}
