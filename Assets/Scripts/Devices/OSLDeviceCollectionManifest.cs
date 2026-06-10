using System;
using UnityEngine;

public enum OSLCollectionSource
{
    Core,
    OfficialPackage,
    Local,
    Package
}

[CreateAssetMenu(fileName = "OSLDeviceCollection", menuName = "OpenSoundLab/Devices/Collection Manifest")]
public sealed class OSLDeviceCollectionManifest : ScriptableObject
{
    public string vendorId = "io.sphericals";
    public string collectionId = "io.sphericals.osl.core.soundgenerator";
    public string resourceSlug = "io.sphericals.osl.core.soundgenerator";
    public string displayName = "Sound Generator";
    public string description;
    public OSLCollectionSource source = OSLCollectionSource.Core;
    public bool enabled = true;
    public string[] nativeSymbols = new string[0];
    public OSLDeviceDefinition[] devices = new OSLDeviceDefinition[0];
}

[Serializable]
public sealed class OSLDeviceProductInfo
{
    public bool isPurchasable;
    public bool requiresEntitlement;
    public string storeSku;
    public string[] storeSkus = new string[0];
}

[Serializable]
public sealed class OSLDeviceDefinition
{
    public string deviceLocalId;
    public string canonicalDeviceId;
    public string displayName;

    public DeviceCategory category;
    public string[] tags = new string[0];
    public int order;

    public bool enabled = true;
    public bool showInMenu = true;
    public OSLDeviceProductInfo productInfo = new OSLDeviceProductInfo();

    public string prefabResourcePath;
    public string menuPrefabResourcePath;
    public string symbolResourcePath;

    public string dataTypeName;

    public string[] legacyDeviceIds = new string[0];
    public string[] legacyDataTypeNames = new string[0];
    public string[] legacyXmlRootNames = new string[0];

    public Vector3 previewPositionOffset;
    public Vector3 previewRotationOffset;
    public Vector3 previewScale = Vector3.one;

    public Vector3 spawnPositionOffset;
    public Vector3 spawnRotationOffset;
}
