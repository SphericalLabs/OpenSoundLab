using System;
using UnityEditor;

internal sealed class DeviceScaffoldPlan
{
    public DeviceScaffoldRequest request;
    public DeviceScaffoldTargetKind targetKind;
    public string makerDomain;
    public string vendorId;
    public string collectionLocalId;
    public string fullCollectionId;
    public string collectionFolderName;
    public string collectionDisplayName;
    public string localId;
    public string displayName;
    public string canonicalDeviceId;
    public string namespaceName;
    public string interfaceClassName;
    public string signalClassName;
    public string dataClassName;
    public string dataTypeName;
    public string collectionRoot;
    public string scriptFolder;
    public string prefabFolder;
    public string menuPrefabFolder;
    public string textureFolder;
    public string runtimePrefabPath;
    public string menuPrefabPath;
    public string symbolPath;
    public OSLDeviceCollectionManifest existingManifest;

    public string InterfaceScriptPath => scriptFolder + "/" + interfaceClassName + ".cs";
    public string SignalScriptPath => scriptFolder + "/" + signalClassName + ".cs";
}

internal static class DeviceScaffoldPlanner
{
    public static DeviceScaffoldPlan Build(DeviceScaffoldRequest request)
    {
        DeviceScaffoldPlan plan = new DeviceScaffoldPlan();
        plan.request = request;
        plan.targetKind = request.targetKind;
        plan.makerDomain = request.targetKind == DeviceScaffoldTargetKind.Core ? OSLDeviceIdentityCompatibility.CoreDomain : OSLDeviceIdentityCompatibility.NormalizeDomain(request.makerDomain);
        plan.vendorId = request.targetKind == DeviceScaffoldTargetKind.Core ? OSLDeviceIdentityCompatibility.CoreVendorId : OSLDeviceIdentityCompatibility.GetVendorId(plan.makerDomain);
        plan.collectionLocalId = request.targetKind == DeviceScaffoldTargetKind.Core ? OSLDeviceIdentityCompatibility.GetCoreCollectionLocalId(request.category) : OSLDeviceIdentityCompatibility.NormalizeDomain(request.collectionLocalId);
        plan.fullCollectionId = request.targetKind == DeviceScaffoldTargetKind.Core ?
            OSLDeviceIdentityCompatibility.BuildCoreCollectionId(plan.collectionLocalId) :
            OSLDeviceIdentityCompatibility.BuildAddonCollectionId(plan.makerDomain, plan.collectionLocalId);
        plan.collectionFolderName = request.targetKind == DeviceScaffoldTargetKind.Core ? request.category.ToString() : plan.fullCollectionId;
        plan.collectionDisplayName = request.targetKind == DeviceScaffoldTargetKind.Core ? OSLDeviceIdentityCompatibility.GetCategoryDisplayName(request.category) : OSLDeviceIdentityCompatibility.ToDisplayName(plan.collectionLocalId);
        plan.localId = OSLDeviceIdentityCompatibility.CleanIdentifier(request.localId);
        plan.displayName = !string.IsNullOrWhiteSpace(request.displayName) ? request.displayName.Trim() : plan.localId;
        plan.canonicalDeviceId = OSLDeviceIdentityCompatibility.BuildCanonicalDeviceId(plan.fullCollectionId, plan.localId);
        plan.namespaceName = OSLDeviceIdentityCompatibility.ToNamespace(plan.fullCollectionId);

        string classStem = OSLDeviceIdentityCompatibility.ToPascalIdentifier(plan.localId);
        plan.interfaceClassName = OSLDeviceIdentityCompatibility.ToLowerCamel(classStem) + "DeviceInterface";
        plan.signalClassName = OSLDeviceIdentityCompatibility.ToLowerCamel(classStem) + "SignalGenerator";
        plan.dataClassName = classStem + "Data";
        plan.dataTypeName = plan.namespaceName + "." + plan.dataClassName;
        if (string.IsNullOrWhiteSpace(plan.collectionFolderName)) plan.collectionFolderName = "Collection";

        plan.existingManifest = FindCollectionManifest(plan.fullCollectionId);
        plan.collectionRoot = plan.existingManifest != null ? GetCollectionRootPath(plan.existingManifest) : GetDefaultCollectionRoot(plan);
        string resourceRoot = plan.collectionRoot + "/Resources/" + plan.fullCollectionId;
        plan.scriptFolder = plan.collectionRoot + "/Scripts/" + plan.localId;
        plan.prefabFolder = resourceRoot + "/Prefabs";
        plan.menuPrefabFolder = resourceRoot + "/MenuPrefabs";
        plan.textureFolder = resourceRoot + "/Textures";
        plan.runtimePrefabPath = plan.prefabFolder + "/" + plan.localId + ".prefab";
        plan.menuPrefabPath = plan.menuPrefabFolder + "/" + plan.localId + "_Menu.prefab";
        plan.symbolPath = plan.textureFolder + "/" + plan.localId + "Symbol.png";
        return plan;
    }

    public static OSLDeviceCollectionManifest FindCollectionManifest(string collectionId)
    {
        if (string.IsNullOrWhiteSpace(collectionId)) return null;

        string[] guids = AssetDatabase.FindAssets("t:OSLDeviceCollectionManifest");
        for (int i = 0; i < guids.Length; ++i)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            OSLDeviceCollectionManifest manifest = AssetDatabase.LoadAssetAtPath<OSLDeviceCollectionManifest>(path);
            if (manifest != null && manifest.collectionId == collectionId) return manifest;
        }

        return null;
    }

    public static string GetCollectionRootPath(OSLDeviceCollectionManifest manifest)
    {
        string assetPath = AssetDatabase.GetAssetPath(manifest).Replace('\\', '/');
        int resourcesIndex = assetPath.IndexOf("/Resources/", StringComparison.Ordinal);
        if (resourcesIndex > 0) return assetPath.Substring(0, resourcesIndex);
        return "";
    }

    static string GetDefaultCollectionRoot(DeviceScaffoldPlan plan)
    {
        if (plan.targetKind == DeviceScaffoldTargetKind.Core) return "Assets/OSLDevices/Core/" + plan.collectionFolderName;
        return "Assets/OSLDevices/Addons/" + plan.collectionFolderName;
    }
}
