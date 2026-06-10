using System;
using UnityEngine;

public enum OSLStoreProductKind
{
    SingleDevice,
    Bundle
}

[CreateAssetMenu(fileName = "OSLStoreProductCatalog", menuName = "OpenSoundLab/Devices/Store Product Catalog")]
public sealed class OSLStoreProductCatalog : ScriptableObject
{
    public OSLStoreProductDefinition[] products = new OSLStoreProductDefinition[0];
}

[Serializable]
public sealed class OSLStoreProductDefinition
{
    public string sku;
    public string displayName;
    public OSLStoreProductKind kind = OSLStoreProductKind.SingleDevice;
    public bool enabled = true;
}
