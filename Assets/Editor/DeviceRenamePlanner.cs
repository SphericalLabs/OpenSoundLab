using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

internal enum DeviceRenameAssetMoveStage
{
    BeforeText,
    AfterText
}

internal sealed class DeviceRenameAssetMove
{
    public string oldPath;
    public string newPath;
    public bool isFolder;
    public bool renamePrefabRoot;
    public string prefabRootName;
    public bool requireSourceNow = true;
    public DeviceRenameAssetMoveStage stage;
}

internal sealed class DeviceRenameTextRewrite
{
    public string scriptFolder;
    public string oldCanonicalDeviceId;
    public string newCanonicalDeviceId;
    public string oldDataTypeName;
    public string newDataTypeName;
    public string oldNamespace;
    public string newNamespace;
    public string oldInterfaceClassName;
    public string newInterfaceClassName;
    public string oldSignalClassName;
    public string newSignalClassName;
    public string oldDataClassName;
    public string newDataClassName;
    public bool appendLegacyDataShell;
    public string legacyInterfaceScriptPath;
    public string legacyLocalDeviceId;
}

internal sealed class DeviceRenameDevicePlan
{
    public OSLDeviceCollectionManifest sourceCollection;
    public OSLDeviceCollectionManifest targetCollection;
    public OSLDeviceDefinition sourceDevice;
    public string sourceCollectionId;
    public string targetCollectionId;
    public string sourceResourceSlug;
    public string targetResourceSlug;
    public string sourceRootPath;
    public string targetRootPath;
    public string oldLocalId;
    public string newLocalId;
    public string oldDisplayName;
    public string newDisplayName;
    public DeviceCategory newCategory;
    public string oldCanonicalDeviceId;
    public string newCanonicalDeviceId;
    public string oldDataTypeName;
    public string newDataTypeName;
    public string oldNamespace;
    public string newNamespace;
    public string oldInterfaceClassName;
    public string newInterfaceClassName;
    public string oldSignalClassName;
    public string newSignalClassName;
    public string oldDataClassName;
    public string newDataClassName;
    public string oldScriptFolder;
    public string newScriptFolder;
    public string prefabResourcePath;
    public string menuPrefabResourcePath;
    public string symbolResourcePath;
}

internal sealed class DeviceRenameCollectionDevicePlan
{
    public OSLDeviceDefinition device;
    public string localId;
    public string oldCanonicalDeviceId;
    public string newCanonicalDeviceId;
    public string oldDataTypeName;
    public string newDataTypeName;
    public string oldDataClassName;
    public string newDataClassName;
    public string scriptFolder;
}

internal sealed class DeviceRenameCollectionPlan
{
    public OSLDeviceCollectionManifest collection;
    public string oldCollectionId;
    public string newCollectionId;
    public string oldResourceSlug;
    public string newResourceSlug;
    public string oldNamespace;
    public string newNamespace;
    public string newVendorId;
    public string newDisplayName;
    public string collectionRootPath;
    public string newCollectionRootPath;
    public string resourceFolderPath;
    public string newResourceFolderPath;
    public string oldManifestPath;
    public string newManifestPath;
    public string finalManifestPath;
    public string newCollectionLocalId;
    public List<DeviceRenameCollectionDevicePlan> devices = new List<DeviceRenameCollectionDevicePlan>();
}

internal sealed class DeviceRenamePlan
{
    public DeviceRenameRequest request;
    public DeviceRenameMode mode;
    public DeviceRenameDevicePlan device;
    public DeviceRenameCollectionPlan collection;
    public List<DeviceRenameAssetMove> assetMoves = new List<DeviceRenameAssetMove>();
    public List<DeviceRenameTextRewrite> textRewrites = new List<DeviceRenameTextRewrite>();

    public string Verb
    {
        get
        {
            if (mode == DeviceRenameMode.RenameCollection) return "Rename";
            if (mode == DeviceRenameMode.MoveDeviceToCollection) return "Move";
            return "Rename";
        }
    }

    public string OldLabel
    {
        get
        {
            if (mode == DeviceRenameMode.RenameCollection) return collection != null ? collection.oldCollectionId : "";
            return device != null ? device.oldCanonicalDeviceId : "";
        }
    }

    public string NewLabel
    {
        get
        {
            if (mode == DeviceRenameMode.RenameCollection) return collection != null ? collection.newCollectionId : "";
            return device != null ? device.newCanonicalDeviceId : "";
        }
    }
}

internal static class DeviceRenamePlanner
{
    public static DeviceRenamePlan Build(DeviceRenameRequest request)
    {
        DeviceRenamePlan plan = new DeviceRenamePlan();
        plan.request = request;
        plan.mode = request.mode;

        if (request.mode == DeviceRenameMode.RenameCollection)
        {
            plan.collection = BuildCollectionPlan(request);
            AddCollectionTextRewrites(plan);
            AddCollectionAssetMoves(plan);
            return plan;
        }

        plan.device = BuildDevicePlan(request);
        AddDeviceAssetMoves(plan);
        AddDeviceTextRewrite(plan);
        return plan;
    }

    public static OSLDeviceCollectionManifest FindFirstCollectionManifest()
    {
        string[] guids = AssetDatabase.FindAssets("t:OSLDeviceCollectionManifest");
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<OSLDeviceCollectionManifest>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    public static OSLDeviceCollectionManifest FindCollectionManifest(string collectionId, OSLDeviceCollectionManifest ignoredManifest)
    {
        if (string.IsNullOrWhiteSpace(collectionId)) return null;

        string[] guids = AssetDatabase.FindAssets("t:OSLDeviceCollectionManifest");
        for (int i = 0; i < guids.Length; ++i)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            OSLDeviceCollectionManifest manifest = AssetDatabase.LoadAssetAtPath<OSLDeviceCollectionManifest>(path);
            if (manifest != null && manifest != ignoredManifest && manifest.collectionId == collectionId) return manifest;
        }

        return null;
    }

    public static OSLDeviceDefinition GetDevice(OSLDeviceCollectionManifest manifest, int index)
    {
        if (manifest == null || manifest.devices == null) return null;
        if (index < 0 || index >= manifest.devices.Length) return null;
        return manifest.devices[index];
    }

    public static string[] GetDeviceLabels(OSLDeviceCollectionManifest manifest)
    {
        if (manifest == null || manifest.devices == null) return new string[0];
        string[] labels = new string[manifest.devices.Length];
        for (int i = 0; i < manifest.devices.Length; ++i)
        {
            OSLDeviceDefinition device = manifest.devices[i];
            labels[i] = device != null ? GetDeviceLocalId(device) + " - " + device.displayName : "Missing Device";
        }

        return labels;
    }

    public static string GetDeviceLocalId(OSLDeviceDefinition device)
    {
        if (device == null) return "";
        if (!string.IsNullOrWhiteSpace(device.deviceLocalId)) return device.deviceLocalId;
        return OSLDeviceIdentityCompatibility.GetLocalIdFromCanonicalId(device.canonicalDeviceId);
    }

    public static string GetCanonicalDeviceId(string collectionId, OSLDeviceDefinition device)
    {
        if (device == null) return "";
        if (!string.IsNullOrWhiteSpace(device.canonicalDeviceId)) return device.canonicalDeviceId;
        return OSLDeviceIdentityCompatibility.BuildCanonicalDeviceId(collectionId, GetDeviceLocalId(device));
    }

    public static string GetResourceSlug(OSLDeviceCollectionManifest manifest)
    {
        if (manifest == null) return "";
        return !string.IsNullOrWhiteSpace(manifest.resourceSlug) ? manifest.resourceSlug : manifest.collectionId;
    }

    public static string GetCollectionRootPath(OSLDeviceCollectionManifest manifest)
    {
        if (manifest == null) return "";

        string assetPath = AssetDatabase.GetAssetPath(manifest).Replace('\\', '/');
        int resourcesIndex = assetPath.IndexOf("/Resources/", StringComparison.Ordinal);
        if (resourcesIndex > 0) return assetPath.Substring(0, resourcesIndex);
        return "";
    }

    public static string GetCollectionResourceFolderPath(OSLDeviceCollectionManifest manifest)
    {
        if (manifest == null) return "";
        string assetPath = AssetDatabase.GetAssetPath(manifest).Replace('\\', '/');
        return OSLDeviceIdentityCompatibility.GetParentPath(assetPath);
    }

    public static bool HasCollectionDevice(OSLDeviceCollectionManifest manifest, string deviceId, int ignoredIndex)
    {
        if (manifest == null || manifest.devices == null) return false;
        for (int i = 0; i < manifest.devices.Length; ++i)
        {
            if (i == ignoredIndex) continue;
            OSLDeviceDefinition device = manifest.devices[i];
            if (device != null && GetDeviceLocalId(device) == deviceId) return true;
        }

        return false;
    }

    public static string FindResourceAssetPath(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath)) return "";

        string resourceName = OSLDeviceIdentityCompatibility.GetResourceName(resourcePath);
        string[] guids = AssetDatabase.FindAssets(resourceName);
        for (int i = 0; i < guids.Length; ++i)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (OSLDeviceIdentityCompatibility.GetResourcePathFromAssetPath(path) == resourcePath) return path;
        }

        return "";
    }

    public static void SplitCollectionId(string collectionId, out string maker, out string local)
    {
        maker = "";
        local = "";
        if (string.IsNullOrWhiteSpace(collectionId)) return;

        int separator = collectionId.LastIndexOf('.');
        if (separator <= 0 || separator >= collectionId.Length - 1)
        {
            local = collectionId;
            return;
        }

        maker = collectionId.Substring(0, separator);
        local = collectionId.Substring(separator + 1);
    }

    static DeviceRenameDevicePlan BuildDevicePlan(DeviceRenameRequest request)
    {
        OSLDeviceDefinition device = GetDevice(request.sourceCollectionManifest, request.deviceIndex);
        OSLDeviceCollectionManifest target = request.mode == DeviceRenameMode.MoveDeviceToCollection ? request.targetCollectionManifest : request.sourceCollectionManifest;
        DeviceRenameDevicePlan plan = new DeviceRenameDevicePlan();
        plan.sourceCollection = request.sourceCollectionManifest;
        plan.targetCollection = target;
        plan.sourceDevice = device;
        plan.sourceCollectionId = request.sourceCollectionManifest != null ? request.sourceCollectionManifest.collectionId : "";
        plan.targetCollectionId = target != null ? target.collectionId : plan.sourceCollectionId;
        plan.sourceResourceSlug = GetResourceSlug(request.sourceCollectionManifest);
        plan.targetResourceSlug = GetResourceSlug(target);
        plan.sourceRootPath = GetCollectionRootPath(request.sourceCollectionManifest);
        plan.targetRootPath = GetCollectionRootPath(target);
        plan.oldLocalId = GetDeviceLocalId(device);
        plan.newLocalId = OSLDeviceIdentityCompatibility.CleanIdentifier(request.localId);
        plan.oldDisplayName = device != null ? device.displayName : "";
        plan.newDisplayName = request.displayName;
        plan.newCategory = request.category;
        plan.oldCanonicalDeviceId = GetCanonicalDeviceId(plan.sourceCollectionId, device);
        plan.newCanonicalDeviceId = OSLDeviceIdentityCompatibility.BuildCanonicalDeviceId(plan.targetCollectionId, plan.newLocalId);
        plan.oldDataTypeName = device != null ? device.dataTypeName : "";
        plan.oldDataClassName = OSLDeviceIdentityCompatibility.GetTypeName(plan.oldDataTypeName);
        if (string.IsNullOrWhiteSpace(plan.oldDataClassName)) plan.oldDataClassName = OSLDeviceIdentityCompatibility.ToPascalIdentifier(plan.oldLocalId) + "Data";
        plan.oldNamespace = OSLDeviceIdentityCompatibility.GetNamespaceName(plan.oldDataTypeName);
        if (string.IsNullOrWhiteSpace(plan.oldNamespace)) plan.oldNamespace = OSLDeviceIdentityCompatibility.ToNamespace(plan.sourceCollectionId);
        plan.newDataTypeName = DeriveNewDataTypeName(plan.oldDataTypeName, plan.sourceCollectionId, plan.targetCollectionId, plan.newLocalId);
        plan.newDataClassName = OSLDeviceIdentityCompatibility.GetTypeName(plan.newDataTypeName);
        if (string.IsNullOrWhiteSpace(plan.newDataClassName)) plan.newDataClassName = OSLDeviceIdentityCompatibility.ToPascalIdentifier(plan.newLocalId) + "Data";
        plan.newNamespace = OSLDeviceIdentityCompatibility.GetNamespaceName(plan.newDataTypeName);
        if (string.IsNullOrWhiteSpace(plan.newNamespace)) plan.newNamespace = OSLDeviceIdentityCompatibility.ToNamespace(plan.targetCollectionId);
        plan.oldInterfaceClassName = OSLDeviceIdentityCompatibility.ToLowerCamel(OSLDeviceIdentityCompatibility.ToPascalIdentifier(plan.oldLocalId)) + "DeviceInterface";
        plan.newInterfaceClassName = OSLDeviceIdentityCompatibility.ToLowerCamel(OSLDeviceIdentityCompatibility.ToPascalIdentifier(plan.newLocalId)) + "DeviceInterface";
        plan.oldSignalClassName = OSLDeviceIdentityCompatibility.ToLowerCamel(OSLDeviceIdentityCompatibility.ToPascalIdentifier(plan.oldLocalId)) + "SignalGenerator";
        plan.newSignalClassName = OSLDeviceIdentityCompatibility.ToLowerCamel(OSLDeviceIdentityCompatibility.ToPascalIdentifier(plan.newLocalId)) + "SignalGenerator";
        plan.oldScriptFolder = plan.sourceRootPath + "/Scripts/" + plan.oldLocalId;
        plan.newScriptFolder = plan.targetRootPath + "/Scripts/" + plan.newLocalId;
        plan.prefabResourcePath = device != null ? device.prefabResourcePath : "";
        plan.menuPrefabResourcePath = device != null ? device.menuPrefabResourcePath : "";
        plan.symbolResourcePath = device != null ? device.symbolResourcePath : "";
        return plan;
    }

    static DeviceRenameCollectionPlan BuildCollectionPlan(DeviceRenameRequest request)
    {
        DeviceRenameCollectionPlan plan = new DeviceRenameCollectionPlan();
        plan.collection = request.sourceCollectionManifest;
        plan.oldCollectionId = request.sourceCollectionManifest != null ? request.sourceCollectionManifest.collectionId : "";
        plan.oldResourceSlug = GetResourceSlug(request.sourceCollectionManifest);
        plan.oldNamespace = OSLDeviceIdentityCompatibility.ToNamespace(plan.oldCollectionId);
        plan.newCollectionLocalId = OSLDeviceIdentityCompatibility.NormalizeDomain(request.collectionLocalId);
        string newMakerDomain = OSLDeviceIdentityCompatibility.NormalizeDomain(request.makerDomain);
        OSLCollectionSource collectionSource = request.sourceCollectionManifest != null ? request.sourceCollectionManifest.source : OSLCollectionSource.Local;
        plan.newCollectionId = collectionSource == OSLCollectionSource.Core ?
            OSLDeviceIdentityCompatibility.BuildCoreCollectionId(plan.newCollectionLocalId) :
            OSLDeviceIdentityCompatibility.BuildAddonCollectionId(newMakerDomain, plan.newCollectionLocalId);
        plan.newResourceSlug = plan.newCollectionId;
        plan.newNamespace = OSLDeviceIdentityCompatibility.ToNamespace(plan.newCollectionId);
        plan.newVendorId = OSLDeviceIdentityCompatibility.GetVendorId(plan.newCollectionId);
        plan.newDisplayName = request.collectionDisplayName;
        plan.collectionRootPath = GetCollectionRootPath(request.sourceCollectionManifest);
        plan.newCollectionRootPath = OSLDeviceIdentityCompatibility.GetParentPath(plan.collectionRootPath) + "/" + GetCollectionFolderName(request.sourceCollectionManifest, plan.newCollectionLocalId, plan.newCollectionId);
        plan.resourceFolderPath = GetCollectionResourceFolderPath(request.sourceCollectionManifest);
        plan.newResourceFolderPath = plan.collectionRootPath + "/Resources/" + plan.newResourceSlug;
        plan.oldManifestPath = request.sourceCollectionManifest != null ? AssetDatabase.GetAssetPath(request.sourceCollectionManifest).Replace('\\', '/') : "";
        plan.newManifestPath = plan.newResourceFolderPath + "/" + plan.newCollectionId + ".asset";
        plan.finalManifestPath = plan.newManifestPath.Replace(plan.collectionRootPath, plan.newCollectionRootPath);

        if (request.sourceCollectionManifest != null && request.sourceCollectionManifest.devices != null)
        {
            for (int i = 0; i < request.sourceCollectionManifest.devices.Length; ++i)
            {
                OSLDeviceDefinition device = request.sourceCollectionManifest.devices[i];
                if (device == null) continue;
                string localId = GetDeviceLocalId(device);
                string oldDataTypeName = device.dataTypeName;
                string newDataTypeName = DeriveNewDataTypeName(oldDataTypeName, plan.oldCollectionId, plan.newCollectionId, localId);
                string oldDataClassName = OSLDeviceIdentityCompatibility.GetTypeName(oldDataTypeName);
                string newDataClassName = OSLDeviceIdentityCompatibility.GetTypeName(newDataTypeName);
                if (string.IsNullOrWhiteSpace(oldDataClassName)) oldDataClassName = OSLDeviceIdentityCompatibility.ToPascalIdentifier(localId) + "Data";
                if (string.IsNullOrWhiteSpace(newDataClassName)) newDataClassName = oldDataClassName;

                plan.devices.Add(new DeviceRenameCollectionDevicePlan
                {
                    device = device,
                    localId = localId,
                    oldCanonicalDeviceId = GetCanonicalDeviceId(plan.oldCollectionId, device),
                    newCanonicalDeviceId = OSLDeviceIdentityCompatibility.BuildCanonicalDeviceId(plan.newCollectionId, localId),
                    oldDataTypeName = oldDataTypeName,
                    newDataTypeName = newDataTypeName,
                    oldDataClassName = oldDataClassName,
                    newDataClassName = newDataClassName,
                    scriptFolder = plan.collectionRootPath + "/Scripts/" + localId
                });
            }
        }

        return plan;
    }

    static void AddDeviceAssetMoves(DeviceRenamePlan plan)
    {
        DeviceRenameDevicePlan device = plan.device;
        if (device == null) return;

        if (plan.mode == DeviceRenameMode.MoveDeviceToCollection)
        {
            string targetResourceRoot = device.targetRootPath + "/Resources/" + device.targetResourceSlug;
            if (plan.request.renameAssets)
            {
                AddMoveResourceAsset(plan, device.prefabResourcePath, targetResourceRoot + "/Prefabs/" + device.newLocalId + ".prefab", true, DeviceRenameAssetMoveStage.BeforeText, value => device.prefabResourcePath = value);
                AddMoveResourceAsset(plan, device.menuPrefabResourcePath, targetResourceRoot + "/MenuPrefabs/" + device.newLocalId + "_Menu.prefab", true, DeviceRenameAssetMoveStage.BeforeText, value => device.menuPrefabResourcePath = value);
                AddMoveResourceAsset(plan, device.symbolResourcePath, targetResourceRoot + "/Textures/" + device.newLocalId + "Symbol.png", false, DeviceRenameAssetMoveStage.BeforeText, value => device.symbolResourcePath = value);
                AddFolderMove(plan, device.oldScriptFolder, device.newScriptFolder, DeviceRenameAssetMoveStage.BeforeText);
                AddScriptFileRenames(plan, device.newScriptFolder, device);
            }
            else
            {
                device.prefabResourcePath = ReplaceCollectionResourcePath(device.prefabResourcePath, device.sourceResourceSlug, device.targetResourceSlug);
                device.menuPrefabResourcePath = ReplaceCollectionResourcePath(device.menuPrefabResourcePath, device.sourceResourceSlug, device.targetResourceSlug);
                device.symbolResourcePath = ReplaceCollectionResourcePath(device.symbolResourcePath, device.sourceResourceSlug, device.targetResourceSlug);
            }

            return;
        }

        if (!plan.request.renameAssets) return;
        AddRenameResourceAsset(plan, device.prefabResourcePath, device.oldLocalId, device.newLocalId, true, value => device.prefabResourcePath = value);
        AddRenameResourceAsset(plan, device.menuPrefabResourcePath, device.oldLocalId + "_Menu", device.newLocalId + "_Menu", true, value => device.menuPrefabResourcePath = value);
        AddRenameResourceAsset(plan, device.symbolResourcePath, device.oldLocalId + "Symbol", device.newLocalId + "Symbol", false, value => device.symbolResourcePath = value);
        AddScriptFileRenames(plan, device.oldScriptFolder, device);
        AddFolderMove(plan, device.oldScriptFolder, device.newScriptFolder, DeviceRenameAssetMoveStage.AfterText);
    }

    static void AddDeviceTextRewrite(DeviceRenamePlan plan)
    {
        if (!plan.request.rewriteScripts || plan.device == null) return;
        DeviceRenameDevicePlan device = plan.device;
        string scriptFolder = plan.mode == DeviceRenameMode.MoveDeviceToCollection && plan.request.renameAssets ? device.newScriptFolder : device.oldScriptFolder;
        string interfaceScriptPath = scriptFolder + "/" + device.newInterfaceClassName + ".cs";

        plan.textRewrites.Add(new DeviceRenameTextRewrite
        {
            scriptFolder = scriptFolder,
            oldCanonicalDeviceId = device.oldCanonicalDeviceId,
            newCanonicalDeviceId = device.newCanonicalDeviceId,
            oldDataTypeName = device.oldDataTypeName,
            newDataTypeName = device.newDataTypeName,
            oldNamespace = device.oldNamespace,
            newNamespace = device.newNamespace,
            oldInterfaceClassName = device.oldInterfaceClassName,
            newInterfaceClassName = device.newInterfaceClassName,
            oldSignalClassName = device.oldSignalClassName,
            newSignalClassName = device.newSignalClassName,
            oldDataClassName = device.oldDataClassName,
            newDataClassName = device.newDataClassName,
            appendLegacyDataShell = plan.request.addLegacyAliases,
            legacyInterfaceScriptPath = interfaceScriptPath,
            legacyLocalDeviceId = device.newLocalId
        });
    }

    static void AddCollectionTextRewrites(DeviceRenamePlan plan)
    {
        if (!plan.request.rewriteScripts || plan.collection == null) return;

        for (int i = 0; i < plan.collection.devices.Count; ++i)
        {
            DeviceRenameCollectionDevicePlan device = plan.collection.devices[i];
            plan.textRewrites.Add(new DeviceRenameTextRewrite
            {
                scriptFolder = device.scriptFolder,
                oldCanonicalDeviceId = device.oldCanonicalDeviceId,
                newCanonicalDeviceId = device.newCanonicalDeviceId,
                oldDataTypeName = device.oldDataTypeName,
                newDataTypeName = device.newDataTypeName,
                oldNamespace = plan.collection.oldNamespace,
                newNamespace = plan.collection.newNamespace,
                oldDataClassName = device.oldDataClassName,
                newDataClassName = device.newDataClassName,
                appendLegacyDataShell = plan.request.addLegacyAliases && device.oldDataTypeName != device.newDataTypeName,
                legacyLocalDeviceId = device.localId
            });
        }
    }

    static void AddCollectionAssetMoves(DeviceRenamePlan plan)
    {
        if (!plan.request.renameAssets || plan.collection == null) return;

        AddFolderMove(plan, plan.collection.resourceFolderPath, plan.collection.newResourceFolderPath, DeviceRenameAssetMoveStage.AfterText);
        plan.assetMoves.Add(new DeviceRenameAssetMove
        {
            oldPath = plan.collection.newResourceFolderPath + "/" + Path.GetFileName(plan.collection.oldManifestPath),
            newPath = plan.collection.newManifestPath,
            requireSourceNow = false,
            stage = DeviceRenameAssetMoveStage.AfterText
        });
        AddFolderMove(plan, plan.collection.collectionRootPath, plan.collection.newCollectionRootPath, DeviceRenameAssetMoveStage.AfterText);
    }

    static void AddScriptFileRenames(DeviceRenamePlan plan, string scriptFolder, DeviceRenameDevicePlan device)
    {
        plan.assetMoves.Add(new DeviceRenameAssetMove { oldPath = scriptFolder + "/" + device.oldInterfaceClassName + ".cs", newPath = scriptFolder + "/" + device.newInterfaceClassName + ".cs", stage = DeviceRenameAssetMoveStage.BeforeText });
        plan.assetMoves.Add(new DeviceRenameAssetMove { oldPath = scriptFolder + "/" + device.oldSignalClassName + ".cs", newPath = scriptFolder + "/" + device.newSignalClassName + ".cs", stage = DeviceRenameAssetMoveStage.BeforeText });
    }

    static void AddRenameResourceAsset(DeviceRenamePlan plan, string resourcePath, string oldLeafName, string newLeafName, bool renamePrefabRoot, Action<string> setResourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath) || oldLeafName == newLeafName) return;

        string assetPath = FindResourceAssetPath(resourcePath);
        string targetPath = "";
        if (!string.IsNullOrWhiteSpace(assetPath))
        {
            string directory = OSLDeviceIdentityCompatibility.GetParentPath(assetPath);
            string extension = Path.GetExtension(assetPath);
            targetPath = directory + "/" + newLeafName + extension;
            plan.assetMoves.Add(new DeviceRenameAssetMove { oldPath = assetPath, newPath = targetPath, renamePrefabRoot = renamePrefabRoot && extension == ".prefab", prefabRootName = newLeafName, stage = DeviceRenameAssetMoveStage.BeforeText });
            setResourcePath(OSLDeviceIdentityCompatibility.GetResourcePathFromAssetPath(targetPath));
            return;
        }

        setResourcePath(ReplaceResourceLeaf(resourcePath, oldLeafName, newLeafName));
    }

    static void AddMoveResourceAsset(DeviceRenamePlan plan, string resourcePath, string targetPath, bool renamePrefabRoot, DeviceRenameAssetMoveStage stage, Action<string> setResourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            setResourcePath("");
            return;
        }

        string assetPath = FindResourceAssetPath(resourcePath);
        if (!string.IsNullOrWhiteSpace(assetPath))
        {
            plan.assetMoves.Add(new DeviceRenameAssetMove { oldPath = assetPath, newPath = targetPath, renamePrefabRoot = renamePrefabRoot && Path.GetExtension(targetPath) == ".prefab", prefabRootName = Path.GetFileNameWithoutExtension(targetPath), stage = stage });
        }

        setResourcePath(OSLDeviceIdentityCompatibility.GetResourcePathFromAssetPath(targetPath));
    }

    static void AddFolderMove(DeviceRenamePlan plan, string oldPath, string newPath, DeviceRenameAssetMoveStage stage)
    {
        if (oldPath == newPath) return;
        plan.assetMoves.Add(new DeviceRenameAssetMove { oldPath = oldPath, newPath = newPath, isFolder = true, stage = stage });
    }

    static string GetCollectionFolderName(OSLDeviceCollectionManifest manifest, string collectionLocalId, string collectionId)
    {
        if (manifest != null && manifest.source == OSLCollectionSource.Core)
        {
            Array categories = Enum.GetValues(typeof(DeviceCategory));
            for (int i = 0; i < categories.Length; ++i)
            {
                DeviceCategory deviceCategory = (DeviceCategory)categories.GetValue(i);
                if (deviceCategory.ToString().ToLowerInvariant() == collectionLocalId) return deviceCategory.ToString();
            }
        }

        return collectionId;
    }

    static string DeriveNewDataTypeName(string oldDataTypeName, string oldCollectionId, string newCollectionId, string newLocalId)
    {
        string newDataClassName = OSLDeviceIdentityCompatibility.ToPascalIdentifier(newLocalId) + "Data";
        if (string.IsNullOrWhiteSpace(oldDataTypeName)) return OSLDeviceIdentityCompatibility.ToNamespace(newCollectionId) + "." + newDataClassName;

        string oldNamespace = OSLDeviceIdentityCompatibility.GetNamespaceName(oldDataTypeName);
        if (string.IsNullOrWhiteSpace(oldNamespace)) return newDataClassName;

        string oldCollectionNamespace = OSLDeviceIdentityCompatibility.ToNamespace(oldCollectionId);
        if (oldNamespace == oldCollectionNamespace || oldDataTypeName.StartsWith(oldCollectionNamespace + ".", StringComparison.Ordinal)) return OSLDeviceIdentityCompatibility.ToNamespace(newCollectionId) + "." + newDataClassName;
        return OSLDeviceIdentityCompatibility.ToNamespace(newCollectionId) + "." + newDataClassName;
    }

    static string ReplaceResourceLeaf(string resourcePath, string oldLeafName, string newLeafName)
    {
        string currentLeaf = OSLDeviceIdentityCompatibility.GetResourceName(resourcePath);
        if (currentLeaf != oldLeafName) return resourcePath;

        int slashIndex = resourcePath.LastIndexOf('/');
        if (slashIndex < 0) return newLeafName;
        return resourcePath.Substring(0, slashIndex + 1) + newLeafName;
    }

    static string ReplaceCollectionResourcePath(string resourcePath, string oldResourceSlug, string newResourceSlug)
    {
        if (string.IsNullOrWhiteSpace(resourcePath)) return resourcePath;
        if (oldResourceSlug == newResourceSlug) return resourcePath;

        string oldPrefix = oldResourceSlug + "/";
        string newPrefix = newResourceSlug + "/";
        if (!resourcePath.StartsWith(oldPrefix, StringComparison.Ordinal)) return resourcePath;
        return newPrefix + resourcePath.Substring(oldPrefix.Length);
    }
}
