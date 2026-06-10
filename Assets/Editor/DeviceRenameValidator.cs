using System.IO;
using UnityEditor;

internal static class DeviceRenameValidator
{
    public static string Validate(DeviceRenamePlan plan)
    {
        string error = plan.mode == DeviceRenameMode.RenameCollection ? ValidateCollection(plan) : ValidateDevice(plan);
        if (!string.IsNullOrWhiteSpace(error)) return error;
        return ValidateMovesAndRewrites(plan);
    }

    static string ValidateDevice(DeviceRenamePlan plan)
    {
        DeviceRenameDevicePlan spec = plan.device;
        DeviceRenameRequest request = plan.request;
        if (spec.sourceCollection == null) return "Select a source collection manifest.";
        if (spec.sourceDevice == null) return "Select a source device.";
        if (string.IsNullOrWhiteSpace(spec.newLocalId)) return "Enter a new local ID.";
        if (spec.newLocalId != request.localId.Trim()) return "The local ID must be one C#-safe identifier segment. Use letters, numbers or underscores, and start with a letter or underscore.";
        if (string.IsNullOrWhiteSpace(spec.sourceRootPath)) return "The source collection manifest must be under a collection-local Resources folder.";

        if (plan.mode == DeviceRenameMode.MoveDeviceToCollection)
        {
            if (spec.targetCollection == null) return "Select a target collection manifest.";
            if (spec.targetCollection == spec.sourceCollection) return "Source and target collection are the same. Use Rename Device instead.";
            if (string.IsNullOrWhiteSpace(spec.targetRootPath)) return "The target collection manifest must be under a collection-local Resources folder.";
            if (DeviceRenamePlanner.HasCollectionDevice(spec.targetCollection, spec.newLocalId, -1)) return "The target collection already has a device with local ID " + spec.newLocalId + ".";
        }
        else if (DeviceRenamePlanner.HasCollectionDevice(spec.sourceCollection, spec.newLocalId, request.deviceIndex))
        {
            return "This collection already has a device with local ID " + spec.newLocalId + ".";
        }

        return "";
    }

    static string ValidateCollection(DeviceRenamePlan plan)
    {
        DeviceRenameCollectionPlan spec = plan.collection;
        DeviceRenameRequest request = plan.request;
        if (spec.collection == null) return "Select a collection manifest.";
        if (string.IsNullOrWhiteSpace(request.makerDomain)) return "Enter a maker domain.";
        if (string.IsNullOrWhiteSpace(request.collectionLocalId)) return "Enter a collection ID.";
        if (!OSLDeviceIdentityCompatibility.IsNamespacePath(spec.newCollectionId)) return "The collection ID must be a dot-separated namespace path.";
        if (string.IsNullOrWhiteSpace(spec.collectionRootPath)) return "The collection manifest must be under a collection-local Resources folder.";

        OSLDeviceCollectionManifest duplicate = DeviceRenamePlanner.FindCollectionManifest(spec.newCollectionId, spec.collection);
        if (duplicate != null) return "Another collection manifest already uses " + spec.newCollectionId + ".";
        return "";
    }

    static string ValidateMovesAndRewrites(DeviceRenamePlan plan)
    {
        if (plan.request.rewriteScripts)
        {
            for (int i = 0; i < plan.textRewrites.Count; ++i)
            {
                if (!Directory.Exists(plan.textRewrites[i].scriptFolder)) return "Script folder is missing: " + plan.textRewrites[i].scriptFolder;
            }
        }

        if (!plan.request.renameAssets) return "";
        for (int i = 0; i < plan.assetMoves.Count; ++i)
        {
            DeviceRenameAssetMove move = plan.assetMoves[i];
            if (move.oldPath == move.newPath) continue;
            if (string.IsNullOrWhiteSpace(move.oldPath) || string.IsNullOrWhiteSpace(move.newPath)) continue;
            if (move.requireSourceNow && !File.Exists(move.oldPath) && !Directory.Exists(move.oldPath)) return "Source asset is missing: " + move.oldPath;
            if (File.Exists(move.newPath) || Directory.Exists(move.newPath) || AssetDatabase.IsValidFolder(move.newPath)) return "Target already exists: " + move.newPath;
        }

        return "";
    }
}
