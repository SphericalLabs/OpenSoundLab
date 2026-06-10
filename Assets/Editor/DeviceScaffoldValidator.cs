using System.IO;

internal static class DeviceScaffoldValidator
{
    public const string DefaultPrefabTemplatePath = "Assets/Resources/Prefabs/Template.prefab";
    public const string DefaultSymbolPath = "Assets/Resources/Textures/RecycleSymbol.png";

    public static string Validate(DeviceScaffoldPlan plan)
    {
        DeviceScaffoldRequest request = plan.request;
        if (request.targetKind == DeviceScaffoldTargetKind.Addon && string.IsNullOrWhiteSpace(request.makerDomain)) return "Enter a maker domain for the add-on.";
        if (request.targetKind == DeviceScaffoldTargetKind.Addon && string.IsNullOrWhiteSpace(request.collectionLocalId)) return "Enter a collection ID for the add-on.";
        if (string.IsNullOrWhiteSpace(request.localId)) return "Enter a local ID.";
        if (plan.localId != request.localId.Trim()) return "The local ID must be one C#-safe identifier segment. Use letters, numbers or underscores, and start with a letter or underscore.";
        if (!OSLDeviceIdentityCompatibility.IsNamespacePath(plan.fullCollectionId)) return "The domain and collection ID must use dot-separated identifier segments, for example com.othercompany.osl.addons.experimental.";
        if (string.IsNullOrWhiteSpace(plan.namespaceName)) return "The derived namespace is invalid.";

        if (!File.Exists(DefaultPrefabTemplatePath)) return "Missing prefab template: " + DefaultPrefabTemplatePath;
        if (request.showInMenu && !File.Exists(DefaultSymbolPath)) return "Missing default symbol texture: " + DefaultSymbolPath;
        if (File.Exists(plan.runtimePrefabPath)) return "Runtime prefab already exists: " + plan.runtimePrefabPath;
        if (request.showInMenu && File.Exists(plan.menuPrefabPath)) return "Menu prefab already exists: " + plan.menuPrefabPath;
        if (File.Exists(plan.InterfaceScriptPath)) return "Device interface script already exists: " + plan.InterfaceScriptPath;
        if (File.Exists(plan.SignalScriptPath)) return "Signal generator script already exists: " + plan.SignalScriptPath;
        if (plan.existingManifest != null && HasCollectionDevice(plan.existingManifest, plan.localId)) return "Collection manifest already contains device " + plan.localId + ".";
        return "";
    }

    static bool HasCollectionDevice(OSLDeviceCollectionManifest manifest, string deviceId)
    {
        if (manifest == null || manifest.devices == null) return false;
        for (int i = 0; i < manifest.devices.Length; ++i)
        {
            OSLDeviceDefinition device = manifest.devices[i];
            if (device == null) continue;
            if (device.deviceLocalId == deviceId) return true;
            if (OSLDeviceIdentityCompatibility.GetLocalIdFromCanonicalId(device.canonicalDeviceId) == deviceId) return true;
        }

        return false;
    }
}
