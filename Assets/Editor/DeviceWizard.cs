using UnityEditor;
using UnityEngine;

public class DeviceWizard : EditorWindow
{
    DeviceScaffoldTargetKind targetKind = DeviceScaffoldTargetKind.Core;
    string makerDomain = "com.company.osl";
    string collectionLocalId = "collection";
    string localId = "";
    string displayName = "";
    DeviceCategory category = DeviceCategory.SoundGenerator;
    int order = 1;
    bool showInMenu = true;
    string tags = "";
    Vector2 scroll;

    [MenuItem("OpenSoundLab/Devices/Create Device")]
    public static void ShowWindow()
    {
        GetWindow<DeviceWizard>("OSL Device Entry");
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.HelpBox("Creates a registry-backed device scaffold. Devices are appended to collection manifests and assets are written into collection-local Scripts and Resources folders.", MessageType.Info);

        EditorGUILayout.LabelField(Content("Identity", "Choose whether this is a built-in core device or an add-on/local collection device."), EditorStyles.boldLabel);
        targetKind = (DeviceScaffoldTargetKind)EditorGUILayout.EnumPopup(Content("Target Kind", "Core writes into the category collection under Assets/OSLDevices/Core. Addon writes into a stable collection-id folder under Assets/OSLDevices/Addons."), targetKind);

        using (new EditorGUI.DisabledScope(targetKind == DeviceScaffoldTargetKind.Core))
        {
            string domainValue = targetKind == DeviceScaffoldTargetKind.Core ? OSLDeviceIdentityCompatibility.CoreDomain : makerDomain;
            domainValue = EditorGUILayout.TextField(Content("Maker Domain", "Reverse-DNS maker domain. Add-on collection ids append .addons.<collection>."), domainValue);
            if (targetKind == DeviceScaffoldTargetKind.Addon) makerDomain = domainValue;

            string collectionValue = targetKind == DeviceScaffoldTargetKind.Core ? OSLDeviceIdentityCompatibility.GetCoreCollectionLocalId(category) : collectionLocalId;
            collectionValue = EditorGUILayout.TextField(Content("Collection ID", "Local collection segment appended after core or addons."), collectionValue);
            if (targetKind == DeviceScaffoldTargetKind.Addon) collectionLocalId = collectionValue;
        }

        localId = EditorGUILayout.TextField(Content("Local ID", "Stable device ID inside the collection, for example GranularCloud."), localId);
        displayName = EditorGUILayout.TextField(Content("Display Name", "Human-readable menu label. Leave blank to use the local ID."), displayName);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(Content("Menu", "Menu placement and visibility."), EditorStyles.boldLabel);
        category = (DeviceCategory)EditorGUILayout.EnumPopup(Content("Category", "Existing menu category where the device appears. For Core, this also selects the owning collection."), category);
        order = EditorGUILayout.IntField(Content("Order", "Sort order within the category. Lower numbers appear earlier."), order);
        showInMenu = EditorGUILayout.Toggle(Content("Show In Menu", "Enable for normal user-facing devices. Disable for hidden/internal devices that should still load from patches."), showInMenu);
        tags = EditorGUILayout.TextField(Content("Tags", "Optional comma-separated grouping labels for future collection/search UI."), tags);

        DrawPreview();

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!CanCreate()))
        {
            if (GUILayout.Button(Content("Create Device", "Create scripts, prefabs, manifest entry, registry metadata and pending Unity compile finalization."))) CreateDeviceScaffold();
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawPreview()
    {
        DeviceScaffoldPlan plan = DeviceScaffoldPlanner.Build(BuildRequest());
        if (string.IsNullOrWhiteSpace(plan.localId)) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(Content("Generated Values", "These values are derived from the fields above."), EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel("Collection: " + plan.fullCollectionId, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.SelectableLabel("Canonical Device ID: " + plan.canonicalDeviceId, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.SelectableLabel("Namespace: " + plan.namespaceName, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.SelectableLabel("Device Interface: " + plan.interfaceClassName, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.SelectableLabel("Signal Generator: " + plan.signalClassName, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.SelectableLabel("Data Type: " + plan.dataTypeName, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(Content("Generated Assets", "Files and folders that will be created. Existing device files are not overwritten."), EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(plan.scriptFolder, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.SelectableLabel(plan.runtimePrefabPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        if (showInMenu) EditorGUILayout.SelectableLabel(plan.menuPrefabPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        if (showInMenu) EditorGUILayout.SelectableLabel(plan.symbolPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
    }

    bool CanCreate()
    {
        return !string.IsNullOrWhiteSpace(localId);
    }

    void CreateDeviceScaffold()
    {
        DeviceScaffoldPlan plan = DeviceScaffoldPlanner.Build(BuildRequest());
        string error = DeviceScaffoldValidator.Validate(plan);
        if (!string.IsNullOrWhiteSpace(error))
        {
            EditorUtility.DisplayDialog("Cannot Create Device", error, "OK");
            return;
        }

        DeviceScaffoldWriter.Write(plan);
        EditorUtility.DisplayDialog(
            "Device Scaffold Created",
            "Created " + plan.localId + ". Unity will compile the generated scripts, then the wizard will add the generated components to the prefab and run collection validation.",
            "OK");
    }

    DeviceScaffoldRequest BuildRequest()
    {
        return new DeviceScaffoldRequest
        {
            targetKind = targetKind,
            makerDomain = makerDomain,
            collectionLocalId = collectionLocalId,
            localId = localId,
            displayName = displayName,
            category = category,
            order = order,
            showInMenu = showInMenu,
            tags = tags
        };
    }

    static GUIContent Content(string text, string tooltip)
    {
        return new GUIContent(text, tooltip);
    }
}
