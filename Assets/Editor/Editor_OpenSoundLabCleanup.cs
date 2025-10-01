using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class Editor_OpenSoundLabCleanup
{
    [MenuItem("Tools/OpenSoundLab/User Files/Clear Samples", false, 200)]
    public static void ClearSamples()
    {
        if (!TryGetSaveRoot(out var root))
        {
            return;
        }

        var samplesPath = Path.Combine(root, "Samples");
        DeleteFolderWithConfirm(samplesPath, "Samples");
    }

    [MenuItem("Tools/OpenSoundLab/User Files/Clear Saves", false, 201)]
    public static void ClearSaves()
    {
        if (!TryGetSaveRoot(out var root))
        {
            return;
        }

        var savesPath = ResolveSavesPath(root);
        DeleteFolderWithConfirm(savesPath, "Saves");
    }

    [MenuItem("Tools/OpenSoundLab/User Files/Clear OpenSoundLab Folder", false, 202)]
    public static void ClearOpenSoundLabFolder()
    {
        if (!TryGetSaveRoot(out var root))
        {
            return;
        }

        DeleteFolderWithConfirm(root, "OpenSoundLab");
    }

    private static bool TryGetSaveRoot(out string saveRoot)
    {
        saveRoot = null;

        try
        {
            saveRoot = masterControl.ResolveDefaultSaveDir();
        }
        catch (Exception ex)
        {
            Debug.LogError($"OpenSoundLab Cleanup: failed to resolve save directory. {ex.Message}\n{ex.StackTrace}");
        }

        if (string.IsNullOrEmpty(saveRoot))
        {
            EditorUtility.DisplayDialog(
                "OpenSoundLab Cleanup",
                "Unable to resolve the OpenSoundLab user files directory.",
                "Ok");
            return false;
        }

        return true;
    }

    private static string ResolveSavesPath(string root)
    {
        var candidates = new[]
        {
            Path.Combine(root, "Saves"),
            Path.Combine(root, "saves")
        };

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return candidates[0];
    }

    private static void DeleteFolderWithConfirm(string folderPath, string displayName)
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            EditorUtility.DisplayDialog(
                "OpenSoundLab Cleanup",
                $"Path for {displayName} is not set.",
                "Ok");
            return;
        }

        if (!Directory.Exists(folderPath))
        {
            EditorUtility.DisplayDialog(
                "OpenSoundLab Cleanup",
                $"No {displayName} folder found at {folderPath}.",
                "Ok");
            return;
        }

        var message =
            $"Delete the {displayName} folder located at {folderPath}? This action cannot be undone.";
        if (!EditorUtility.DisplayDialog("OpenSoundLab Cleanup", message, "Delete", "Cancel"))
        {
            return;
        }

        try
        {
            FileUtil.DeleteFileOrDirectory(folderPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"OpenSoundLab Cleanup: failed to delete {folderPath}. {ex.Message}\n{ex.StackTrace}");
            EditorUtility.DisplayDialog(
                "OpenSoundLab Cleanup",
                $"Failed to delete {displayName} at {folderPath}. Check the console for details.",
                "Ok");
            return;
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "OpenSoundLab Cleanup",
            $"Deleted {displayName} folder at {folderPath}.",
            "Ok");
    }
}
