using System;
using UnityEditor;

internal static class DeviceRenameManifestUpdater
{
    public static void Apply(DeviceRenamePlan plan)
    {
        if (plan.mode == DeviceRenameMode.RenameCollection)
        {
            ApplyCollectionRename(plan.collection, plan.request.addLegacyAliases);
            return;
        }

        if (plan.mode == DeviceRenameMode.MoveDeviceToCollection) ApplyDeviceMove(plan);
        else ApplyDeviceRename(plan);
    }

    static void ApplyDeviceRename(DeviceRenamePlan plan)
    {
        DeviceRenameDevicePlan spec = plan.device;
        OSLDeviceDefinition device = spec.sourceDevice;
        if (device == null || spec.sourceCollection == null) return;

        Undo.RecordObject(spec.sourceCollection, "Rename OSL device");
        AddLegacyAliasesToDevice(device, spec, plan.request.addLegacyAliases);
        UpdateDeviceDefinition(device, spec);
        EditorUtility.SetDirty(spec.sourceCollection);
    }

    static void ApplyDeviceMove(DeviceRenamePlan plan)
    {
        DeviceRenameDevicePlan spec = plan.device;
        if (spec.sourceCollection == null || spec.targetCollection == null || spec.sourceDevice == null) return;

        OSLDeviceDefinition movedDevice = CloneDevice(spec.sourceDevice);
        AddLegacyAliasesToDevice(movedDevice, spec, plan.request.addLegacyAliases);
        UpdateDeviceDefinition(movedDevice, spec);

        Undo.RecordObject(spec.sourceCollection, "Move OSL device from collection");
        Undo.RecordObject(spec.targetCollection, "Move OSL device to collection");
        spec.sourceCollection.devices = RemoveDeviceAt(spec.sourceCollection.devices, plan.request.deviceIndex);
        spec.targetCollection.devices = Append(spec.targetCollection.devices, movedDevice);
        EditorUtility.SetDirty(spec.sourceCollection);
        EditorUtility.SetDirty(spec.targetCollection);
    }

    static void ApplyCollectionRename(DeviceRenameCollectionPlan spec, bool addLegacyAliases)
    {
        if (spec.collection == null) return;

        Undo.RecordObject(spec.collection, "Rename OSL collection");
        spec.collection.vendorId = spec.newVendorId;
        spec.collection.collectionId = spec.newCollectionId;
        spec.collection.resourceSlug = spec.newResourceSlug;
        spec.collection.displayName = !string.IsNullOrWhiteSpace(spec.newDisplayName) ? spec.newDisplayName : OSLDeviceIdentityCompatibility.ToDisplayName(spec.newCollectionLocalId);

        for (int i = 0; i < spec.devices.Count; ++i)
        {
            DeviceRenameCollectionDevicePlan plan = spec.devices[i];
            OSLDeviceDefinition device = plan.device;
            if (device == null) continue;

            if (addLegacyAliases)
            {
                device.legacyDeviceIds = OSLDeviceIdentityCompatibility.AddUnique(device.legacyDeviceIds, plan.oldCanonicalDeviceId);
                if (plan.oldDataTypeName != plan.newDataTypeName) device.legacyDataTypeNames = OSLDeviceIdentityCompatibility.AddUnique(device.legacyDataTypeNames, plan.oldDataTypeName);
            }

            device.canonicalDeviceId = plan.newCanonicalDeviceId;
            device.prefabResourcePath = ReplaceCollectionResourcePath(device.prefabResourcePath, spec.oldResourceSlug, spec.newResourceSlug);
            device.menuPrefabResourcePath = ReplaceCollectionResourcePath(device.menuPrefabResourcePath, spec.oldResourceSlug, spec.newResourceSlug);
            device.symbolResourcePath = ReplaceCollectionResourcePath(device.symbolResourcePath, spec.oldResourceSlug, spec.newResourceSlug);
            device.dataTypeName = plan.newDataTypeName;
        }

        EditorUtility.SetDirty(spec.collection);
    }

    static void UpdateDeviceDefinition(OSLDeviceDefinition device, DeviceRenameDevicePlan spec)
    {
        device.deviceLocalId = spec.newLocalId;
        device.canonicalDeviceId = spec.newCanonicalDeviceId;
        device.displayName = !string.IsNullOrWhiteSpace(spec.newDisplayName) ? spec.newDisplayName : spec.newLocalId;
        device.category = spec.newCategory;
        device.prefabResourcePath = spec.prefabResourcePath;
        device.menuPrefabResourcePath = spec.menuPrefabResourcePath;
        device.symbolResourcePath = spec.symbolResourcePath;
        device.dataTypeName = spec.newDataTypeName;
    }

    static void AddLegacyAliasesToDevice(OSLDeviceDefinition device, DeviceRenameDevicePlan spec, bool addLegacyAliases)
    {
        if (!addLegacyAliases || device == null) return;

        device.legacyDeviceIds = OSLDeviceIdentityCompatibility.AddUnique(device.legacyDeviceIds, spec.oldCanonicalDeviceId);
        device.legacyDeviceIds = OSLDeviceIdentityCompatibility.AddUnique(device.legacyDeviceIds, spec.oldLocalId);
        if (spec.oldDataTypeName != spec.newDataTypeName) device.legacyDataTypeNames = OSLDeviceIdentityCompatibility.AddUnique(device.legacyDataTypeNames, spec.oldDataTypeName);
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

    static OSLDeviceDefinition CloneDevice(OSLDeviceDefinition device)
    {
        if (device == null) return null;
        return new OSLDeviceDefinition
        {
            deviceLocalId = device.deviceLocalId,
            canonicalDeviceId = device.canonicalDeviceId,
            displayName = device.displayName,
            category = device.category,
            tags = CopyArray(device.tags),
            order = device.order,
            enabled = device.enabled,
            showInMenu = device.showInMenu,
            prefabResourcePath = device.prefabResourcePath,
            menuPrefabResourcePath = device.menuPrefabResourcePath,
            symbolResourcePath = device.symbolResourcePath,
            dataTypeName = device.dataTypeName,
            legacyDeviceIds = CopyArray(device.legacyDeviceIds),
            legacyDataTypeNames = CopyArray(device.legacyDataTypeNames),
            legacyXmlRootNames = CopyArray(device.legacyXmlRootNames),
            previewPositionOffset = device.previewPositionOffset,
            previewRotationOffset = device.previewRotationOffset,
            previewScale = device.previewScale,
            spawnPositionOffset = device.spawnPositionOffset,
            spawnRotationOffset = device.spawnRotationOffset
        };
    }

    static string[] CopyArray(string[] values)
    {
        if (values == null) return new string[0];
        string[] copy = new string[values.Length];
        Array.Copy(values, copy, values.Length);
        return copy;
    }

    static OSLDeviceDefinition[] RemoveDeviceAt(OSLDeviceDefinition[] devices, int index)
    {
        if (devices == null || index < 0 || index >= devices.Length) return devices != null ? devices : new OSLDeviceDefinition[0];
        OSLDeviceDefinition[] next = new OSLDeviceDefinition[devices.Length - 1];
        int nextIndex = 0;
        for (int i = 0; i < devices.Length; ++i)
        {
            if (i == index) continue;
            next[nextIndex] = devices[i];
            nextIndex++;
        }

        return next;
    }

    static T[] Append<T>(T[] values, T value)
    {
        if (values == null) values = new T[0];
        T[] next = new T[values.Length + 1];
        Array.Copy(values, next, values.Length);
        next[next.Length - 1] = value;
        return next;
    }
}
