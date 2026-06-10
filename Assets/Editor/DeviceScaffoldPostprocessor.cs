using System;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class DeviceScaffoldPostprocessor
{
    const string pendingKey = "OSL_DeviceWizardPending";
    const string pendingRuntimePrefabPathKey = "OSL_DeviceWizardRuntimePrefabPath";
    const string pendingMenuPrefabPathKey = "OSL_DeviceWizardMenuPrefabPath";
    const string pendingInterfaceTypeNameKey = "OSL_DeviceWizardInterfaceTypeName";
    const string pendingSignalTypeNameKey = "OSL_DeviceWizardSignalTypeName";
    const string pendingShowInMenuKey = "OSL_DeviceWizardShowInMenu";

    static DeviceScaffoldPostprocessor()
    {
        EditorApplication.delayCall += FinalizePendingDevice;
    }

    public static void SavePendingFinalization(DeviceScaffoldPlan plan)
    {
        EditorPrefs.SetBool(pendingKey, true);
        EditorPrefs.SetString(pendingRuntimePrefabPathKey, plan.runtimePrefabPath);
        EditorPrefs.SetString(pendingMenuPrefabPathKey, plan.request.showInMenu ? plan.menuPrefabPath : "");
        EditorPrefs.SetString(pendingInterfaceTypeNameKey, plan.namespaceName + "." + plan.interfaceClassName);
        EditorPrefs.SetString(pendingSignalTypeNameKey, plan.namespaceName + "." + plan.signalClassName);
        EditorPrefs.SetBool(pendingShowInMenuKey, plan.request.showInMenu);
    }

    static void FinalizePendingDevice()
    {
        if (!EditorPrefs.GetBool(pendingKey, false)) return;

        string runtimePrefabPath = EditorPrefs.GetString(pendingRuntimePrefabPathKey, "");
        string menuPrefabPath = EditorPrefs.GetString(pendingMenuPrefabPathKey, "");
        string interfaceTypeName = EditorPrefs.GetString(pendingInterfaceTypeNameKey, "");
        string signalTypeName = EditorPrefs.GetString(pendingSignalTypeNameKey, "");
        bool pendingShowInMenu = EditorPrefs.GetBool(pendingShowInMenuKey, true);

        Type interfaceType = FindType(interfaceTypeName);
        Type signalType = FindType(signalTypeName);
        if (interfaceType == null || signalType == null)
        {
            Debug.LogWarning("OpenSoundLab Device Wizard: Waiting for generated device types to compile.");
            return;
        }

        if (!typeof(Component).IsAssignableFrom(signalType) || !typeof(Component).IsAssignableFrom(interfaceType))
        {
            Debug.LogError("OpenSoundLab Device Wizard: Generated types are not Unity components.");
            return;
        }

        AddGeneratedComponents(runtimePrefabPath, signalType, interfaceType);

        if (pendingShowInMenu && !string.IsNullOrWhiteSpace(menuPrefabPath))
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(menuPrefabPath) == null)
            {
                OSLMenuPrefabVariantUtility.CreateFromPrefab(runtimePrefabPath, menuPrefabPath);
            }
        }

        ClearPendingFinalization();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        OSLDeviceRegistry.Refresh();

        OSLDeviceCollectionValidator.ValidationReport report = OSLDeviceCollectionValidator.ValidateCollections();
        if (report.errors > 0)
        {
            Debug.LogError("OpenSoundLab Device Wizard: Device scaffold finalized, but validation found " + report.errors + " error(s) and " + report.warnings + " warning(s).");
        }
        else
        {
            Debug.Log("OpenSoundLab Device Wizard: Device scaffold finalized. Validation passed with " + report.warnings + " warning(s).");
        }
    }

    static void ClearPendingFinalization()
    {
        EditorPrefs.DeleteKey(pendingKey);
        EditorPrefs.DeleteKey(pendingRuntimePrefabPathKey);
        EditorPrefs.DeleteKey(pendingMenuPrefabPathKey);
        EditorPrefs.DeleteKey(pendingInterfaceTypeNameKey);
        EditorPrefs.DeleteKey(pendingSignalTypeNameKey);
        EditorPrefs.DeleteKey(pendingShowInMenuKey);
    }

    static void AddGeneratedComponents(string prefabPath, Type signalType, Type interfaceType)
    {
        if (string.IsNullOrWhiteSpace(prefabPath)) return;

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null) return;

        if (prefabRoot.GetComponent(signalType) == null) prefabRoot.AddComponent(signalType);
        if (prefabRoot.GetComponent(interfaceType) == null) prefabRoot.AddComponent(interfaceType);

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    static Type FindType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return null;

        Type directType = Type.GetType(typeName);
        if (directType != null) return directType;

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; ++i)
        {
            Type resolvedType = assemblies[i].GetType(typeName);
            if (resolvedType != null) return resolvedType;
        }

        return null;
    }
}
