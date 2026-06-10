using System;
using System.IO;
using UnityEditor;
using UnityEngine;

internal static class DeviceScaffoldWriter
{
    public static void Write(DeviceScaffoldPlan plan)
    {
        EnsureFolder(plan.scriptFolder);
        EnsureFolder(plan.prefabFolder);
        EnsureFolder(plan.menuPrefabFolder);
        EnsureFolder(plan.textureFolder);

        File.WriteAllText(plan.InterfaceScriptPath, BuildDeviceInterfaceSource(plan));
        File.WriteAllText(plan.SignalScriptPath, BuildSignalGeneratorSource(plan));
        CopyRuntimePrefab(plan);
        if (plan.request.showInMenu) CopySymbol(plan);
        AddManifestEntry(plan);

        AssetDatabase.SaveAssets();
        DeviceScaffoldPostprocessor.SavePendingFinalization(plan);
        AssetDatabase.Refresh();
    }

    static string BuildDeviceInterfaceSource(DeviceScaffoldPlan plan)
    {
        return @"using System.Xml.Serialization;
using UnityEngine;

namespace " + plan.namespaceName + @"
{
public class " + plan.interfaceClassName + @" : deviceInterface
{
    " + plan.signalClassName + @" signal;

    public override void Awake()
    {
        base.Awake();
        signal = GetComponent<" + plan.signalClassName + @">();
    }

    public override InstrumentData GetData()
    {
        " + plan.dataClassName + @" data = new " + plan.dataClassName + @"();
        setDeviceType(data);
        GetTransformData(data);
        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        " + plan.dataClassName + @" data = d as " + plan.dataClassName + @";
        base.Load(data, copyMode);
    }
}

[XmlType(""" + plan.canonicalDeviceId + @""")]
public class " + plan.dataClassName + @" : InstrumentData
{
}
}
";
    }

    static string BuildSignalGeneratorSource(DeviceScaffoldPlan plan)
    {
        return @"using UnityEngine;

namespace " + plan.namespaceName + @"
{
public class " + plan.signalClassName + @" : signalGenerator
{
    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        for (int i = 0; i < buffer.Length; ++i)
        {
            buffer[i] = 0f;
        }
    }
}
}
";
    }

    static void CopyRuntimePrefab(DeviceScaffoldPlan plan)
    {
        if (!AssetDatabase.CopyAsset(DeviceScaffoldValidator.DefaultPrefabTemplatePath, plan.runtimePrefabPath))
        {
            Debug.LogError("OpenSoundLab Device Wizard: Failed to copy runtime prefab template to " + plan.runtimePrefabPath + ".");
            return;
        }

        RenamePrefabRoot(plan.runtimePrefabPath, Path.GetFileNameWithoutExtension(plan.runtimePrefabPath));
    }

    static void CopySymbol(DeviceScaffoldPlan plan)
    {
        if (AssetDatabase.CopyAsset(DeviceScaffoldValidator.DefaultSymbolPath, plan.symbolPath)) return;
        Debug.LogWarning("OpenSoundLab Device Wizard: Failed to copy default symbol to " + plan.symbolPath + ".");
    }

    static void AddManifestEntry(DeviceScaffoldPlan plan)
    {
        OSLDeviceCollectionManifest manifest = GetOrCreateCollectionManifest(plan);
        string menuPrefabResourcePath = plan.request.showInMenu ? OSLDeviceIdentityCompatibility.GetResourcePathFromAssetPath(plan.menuPrefabPath) : "";
        string symbolResourcePath = plan.request.showInMenu ? OSLDeviceIdentityCompatibility.GetResourcePathFromAssetPath(plan.symbolPath) : "";

        Undo.RecordObject(manifest, "Add OSL device");
        manifest.devices = Append(manifest.devices, new OSLDeviceDefinition
        {
            deviceLocalId = plan.localId,
            canonicalDeviceId = plan.canonicalDeviceId,
            displayName = plan.displayName,
            category = plan.request.category,
            order = plan.request.order,
            enabled = true,
            showInMenu = plan.request.showInMenu,
            productInfo = new OSLDeviceProductInfo(),
            prefabResourcePath = OSLDeviceIdentityCompatibility.GetResourcePathFromAssetPath(plan.runtimePrefabPath),
            menuPrefabResourcePath = menuPrefabResourcePath,
            symbolResourcePath = symbolResourcePath,
            dataTypeName = plan.dataTypeName,
            tags = ParseList(plan.request.tags),
            legacyDeviceIds = new string[0],
            legacyDataTypeNames = new string[0],
            legacyXmlRootNames = new string[0],
            previewScale = Vector3.one
        });
        EditorUtility.SetDirty(manifest);
    }

    static OSLDeviceCollectionManifest GetOrCreateCollectionManifest(DeviceScaffoldPlan plan)
    {
        if (plan.existingManifest != null) return plan.existingManifest;

        string manifestFolder = plan.collectionRoot + "/Resources/" + plan.fullCollectionId;
        EnsureFolder(manifestFolder);

        OSLDeviceCollectionManifest manifest = ScriptableObject.CreateInstance<OSLDeviceCollectionManifest>();
        manifest.vendorId = plan.vendorId;
        manifest.collectionId = plan.fullCollectionId;
        manifest.resourceSlug = plan.fullCollectionId;
        manifest.displayName = plan.collectionDisplayName;
        manifest.description = plan.targetKind == DeviceScaffoldTargetKind.Core ? "Included OpenSoundLab " + plan.collectionDisplayName + " devices." : "";
        manifest.source = plan.targetKind == DeviceScaffoldTargetKind.Core ? OSLCollectionSource.Core : OSLCollectionSource.Local;
        manifest.enabled = true;
        manifest.nativeSymbols = new string[0];
        manifest.devices = new OSLDeviceDefinition[0];

        AssetDatabase.CreateAsset(manifest, manifestFolder + "/" + plan.fullCollectionId + ".asset");
        AssetDatabase.SaveAssets();
        return manifest;
    }

    public static void EnsureFolder(string path)
    {
        string normalizedPath = path.Replace('\\', '/').Trim('/');
        string[] parts = normalizedPath.Split('/');
        if (parts.Length == 0 || parts[0] != "Assets") return;

        string current = "Assets";
        for (int i = 1; i < parts.Length; ++i)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
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

    static string[] ParseList(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new string[0];

        string[] pieces = value.Split(new char[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        int count = 0;
        for (int i = 0; i < pieces.Length; ++i)
        {
            pieces[i] = pieces[i].Trim();
            if (!string.IsNullOrWhiteSpace(pieces[i])) count++;
        }

        string[] values = new string[count];
        int valueIndex = 0;
        for (int i = 0; i < pieces.Length; ++i)
        {
            if (string.IsNullOrWhiteSpace(pieces[i])) continue;
            values[valueIndex] = pieces[i];
            valueIndex++;
        }

        return values;
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
