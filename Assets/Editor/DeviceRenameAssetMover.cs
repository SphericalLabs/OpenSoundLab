using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

internal static class DeviceRenameAssetMover
{
    public static void Apply(DeviceRenamePlan plan, DeviceRenameAssetMoveStage stage, List<string> errors)
    {
        if (!plan.request.renameAssets) return;

        for (int i = 0; i < plan.assetMoves.Count; ++i)
        {
            DeviceRenameAssetMove move = plan.assetMoves[i];
            if (move.stage != stage) continue;
            MoveAsset(move, errors);
        }
    }

    static void MoveAsset(DeviceRenameAssetMove move, List<string> errors)
    {
        if (move.oldPath == move.newPath) return;
        if (string.IsNullOrWhiteSpace(move.oldPath) || string.IsNullOrWhiteSpace(move.newPath)) return;
        if (!File.Exists(move.oldPath) && !Directory.Exists(move.oldPath) && !AssetDatabase.IsValidFolder(move.oldPath)) return;
        if (File.Exists(move.newPath) || Directory.Exists(move.newPath) || AssetDatabase.IsValidFolder(move.newPath))
        {
            errors.Add("Target already exists: " + move.newPath);
            return;
        }

        DeviceScaffoldWriter.EnsureFolder(OSLDeviceIdentityCompatibility.GetParentPath(move.newPath));
        string error = AssetDatabase.MoveAsset(move.oldPath, move.newPath);
        if (!string.IsNullOrWhiteSpace(error)) errors.Add(error);
        if (move.renamePrefabRoot) RenamePrefabRoot(move.newPath, move.prefabRootName);
    }

    static void RenamePrefabRoot(string prefabPath, string rootName)
    {
        if (string.IsNullOrWhiteSpace(prefabPath) || string.IsNullOrWhiteSpace(rootName)) return;

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null) return;

        prefabRoot.name = rootName;
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }
}
