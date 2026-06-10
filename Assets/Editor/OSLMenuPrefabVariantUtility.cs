using System.IO;
using UnityEditor;
using UnityEngine;

public static class OSLMenuPrefabVariantUtility
{
    public const string DefaultMaterialPath = "Assets/Materials/uncategorized/InstrumentSelectMat.mat";

    public static bool CreateFromPrefab(string sourcePrefabPath, string menuPrefabPath, bool searchInChildren = true, Material replacementMaterial = null)
    {
        if (string.IsNullOrWhiteSpace(sourcePrefabPath) || string.IsNullOrWhiteSpace(menuPrefabPath)) return false;

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(sourcePrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError("OpenSoundLab: Could not load prefab at " + sourcePrefabPath + ".");
            return false;
        }

        bool saved = false;
        try
        {
            prefabRoot.name = Path.GetFileNameWithoutExtension(menuPrefabPath);
            Strip(prefabRoot, searchInChildren, replacementMaterial != null ? replacementMaterial : LoadDefaultMaterial());

            string folder = Path.GetDirectoryName(menuPrefabPath);
            if (!string.IsNullOrWhiteSpace(folder) && !Directory.Exists(folder)) Directory.CreateDirectory(folder);

            saved = PrefabUtility.SaveAsPrefabAsset(prefabRoot, menuPrefabPath) != null;
            if (!saved) Debug.LogError("OpenSoundLab: Could not save menu prefab at " + menuPrefabPath + ".");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        return saved;
    }

    public static void Strip(GameObject target, bool searchInChildren = true, Material replacementMaterial = null)
    {
        if (target == null) return;

        GameObject toolObject = new GameObject("OSLMenuPrefabVariantUtility_Temp");
        toolObject.hideFlags = HideFlags.HideAndDontSave;

        RemoveDeviceComponents tool = toolObject.AddComponent<RemoveDeviceComponents>();
        tool.searchInChildren = searchInChildren;
        tool.newMaterial = replacementMaterial != null ? replacementMaterial : LoadDefaultMaterial();
        tool.targets = new[] { target };
        tool.RemoveComponents();

        Object.DestroyImmediate(toolObject);
    }

    public static Material LoadDefaultMaterial()
    {
        return AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterialPath);
    }
}
