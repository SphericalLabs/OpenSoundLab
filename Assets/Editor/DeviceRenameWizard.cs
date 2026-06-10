using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DeviceRenameWizard : EditorWindow
{
    DeviceRenameMode mode = DeviceRenameMode.RenameDevice;
    OSLDeviceCollectionManifest sourceCollectionManifest;
    OSLDeviceCollectionManifest previousSourceCollectionManifest;
    OSLDeviceCollectionManifest targetCollectionManifest;
    OSLDeviceCollectionManifest previousTargetCollectionManifest;
    int deviceIndex;
    int previousDeviceIndex = -1;

    string makerDomain = "com.othercompany.osl";
    string collectionLocalId = "collection";
    string collectionDisplayName = "";
    string localId = "";
    string displayName = "";
    DeviceCategory category = DeviceCategory.SoundGenerator;
    bool renameAssets = true;
    bool rewriteScripts = true;
    bool addLegacyAliases = true;
    Vector2 scroll;

    [MenuItem("OpenSoundLab/Devices/Rename Device Or Collection")]
    public static void ShowWindow()
    {
        GetWindow<DeviceRenameWizard>("OSL Rename Device");
    }

    void OnEnable()
    {
        if (sourceCollectionManifest == null) sourceCollectionManifest = DeviceRenamePlanner.FindFirstCollectionManifest();
        LoadCurrentValues();
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.HelpBox("Warning: This wizard is not yet fully working and should not be used yet.", MessageType.Error);
        EditorGUILayout.HelpBox("Renames devices and collections that use OSLDeviceCollectionManifest. Device moves preserve asset GUIDs through AssetDatabase and keep resource paths collection-local.", MessageType.Info);

        EditorGUILayout.LabelField(Content("Operation", "Choose the kind of registry refactor to apply."), EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        mode = (DeviceRenameMode)EditorGUILayout.EnumPopup(Content("Mode", "Rename Device edits one device. Rename Collection edits every device in the collection. Move Device To Collection moves one device between collection manifests."), mode);
        if (EditorGUI.EndChangeCheck()) LoadCurrentValues();

        DrawManifestSelectors();
        if (mode != DeviceRenameMode.RenameCollection) DrawDeviceSelector();

        EditorGUILayout.Space();
        DrawIdentityFields();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(Content("Scope", "Choose how far the wizard should apply the change."), EditorStyles.boldLabel);
        renameAssets = EditorGUILayout.Toggle(Content("Move/Rename Assets", "Move prefabs, menu prefabs, symbols, resource folders and script folders through AssetDatabase so Unity .meta GUIDs are preserved."), renameAssets);
        rewriteScripts = EditorGUILayout.Toggle(Content("Rewrite Device Scripts", "Rewrite only scripts inside the affected device script folder(s)."), rewriteScripts);
        addLegacyAliases = EditorGUILayout.Toggle(Content("Add Legacy Aliases", "Store old canonical/local IDs and old data type names on moved or renamed device definitions."), addLegacyAliases);

        DrawPreview();

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!CanApply()))
        {
            if (GUILayout.Button(Content(GetButtonLabel(), "Apply the selected rename or move."))) ApplySelectedOperation();
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawManifestSelectors()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(Content("Source", "Collection manifest that owns the selected device or collection."), EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        sourceCollectionManifest = (OSLDeviceCollectionManifest)EditorGUILayout.ObjectField(Content("Source Collection", "Collection manifest to edit."), sourceCollectionManifest, typeof(OSLDeviceCollectionManifest), false);
        if (EditorGUI.EndChangeCheck() || previousSourceCollectionManifest != sourceCollectionManifest)
        {
            previousSourceCollectionManifest = sourceCollectionManifest;
            deviceIndex = 0;
            previousDeviceIndex = -1;
            LoadCurrentValues();
        }

        if (mode != DeviceRenameMode.MoveDeviceToCollection) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(Content("Target", "Existing collection manifest that should receive the device."), EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        targetCollectionManifest = (OSLDeviceCollectionManifest)EditorGUILayout.ObjectField(Content("Target Collection", "Destination collection manifest."), targetCollectionManifest, typeof(OSLDeviceCollectionManifest), false);
        if (EditorGUI.EndChangeCheck() || previousTargetCollectionManifest != targetCollectionManifest) previousTargetCollectionManifest = targetCollectionManifest;
    }

    void DrawDeviceSelector()
    {
        string[] labels = DeviceRenamePlanner.GetDeviceLabels(sourceCollectionManifest);
        if (labels.Length == 0)
        {
            EditorGUILayout.HelpBox("Select a collection manifest with at least one device entry.", MessageType.Warning);
            return;
        }

        if (deviceIndex < 0 || deviceIndex >= labels.Length) deviceIndex = 0;

        EditorGUI.BeginChangeCheck();
        deviceIndex = EditorGUILayout.Popup(Content("Device", "Manifest device entry to rename or move."), deviceIndex, labels);
        if (EditorGUI.EndChangeCheck() || previousDeviceIndex != deviceIndex)
        {
            previousDeviceIndex = deviceIndex;
            LoadCurrentValues();
        }

        if (GUILayout.Button(Content("Load Current Values", "Reset the edit fields from the selected manifest entry."))) LoadCurrentValues();
    }

    void DrawIdentityFields()
    {
        EditorGUILayout.LabelField(Content("New Identity", "Values that will be written by the selected operation."), EditorStyles.boldLabel);

        bool editCollection = mode == DeviceRenameMode.RenameCollection;
        using (new EditorGUI.DisabledScope(!editCollection))
        {
            makerDomain = EditorGUILayout.TextField(Content("Maker Domain", "Reverse-DNS maker domain. Add-on collection ids append .addons.<collection>."), makerDomain);
            collectionLocalId = EditorGUILayout.TextField(Content("Collection ID", "Local collection segment appended after core or addons."), collectionLocalId);
            collectionDisplayName = EditorGUILayout.TextField(Content("Collection Name", "Human-readable collection name stored on the collection manifest."), collectionDisplayName);
        }

        if (mode == DeviceRenameMode.RenameCollection) return;

        localId = EditorGUILayout.TextField(Content("Local ID", "Stable device ID inside the collection."), localId);
        displayName = EditorGUILayout.TextField(Content("Display Name", "Human-readable menu label. Leave blank to use the local ID."), displayName);
        category = (DeviceCategory)EditorGUILayout.EnumPopup(Content("Category", "Menu category for the device. This can change without moving the owning collection."), category);
    }

    void DrawPreview()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(Content("Preview", "Derived paths and IDs that will be touched."), EditorStyles.boldLabel);

        DeviceRenamePlan plan = DeviceRenamePlanner.Build(BuildRequest());
        if (mode == DeviceRenameMode.RenameCollection)
        {
            if (plan.collection == null || sourceCollectionManifest == null) return;
            EditorGUILayout.SelectableLabel("Old Collection: " + plan.collection.oldCollectionId, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.SelectableLabel("New Collection: " + plan.collection.newCollectionId, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.SelectableLabel("Old Root: " + plan.collection.collectionRootPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.SelectableLabel("New Root: " + plan.collection.newCollectionRootPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (rewriteScripts) EditorGUILayout.SelectableLabel("Script folders in scope: " + plan.textRewrites.Count, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.HelpBox("Collection rename updates every device canonical ID, resource path and qualified data type namespace in this manifest. Script rewrites are limited to Scripts/<DeviceId> folders.", MessageType.Info);
            return;
        }

        if (!HasSelectedDevice()) return;
        DeviceRenameDevicePlan device = plan.device;
        EditorGUILayout.SelectableLabel("Old Device ID: " + device.oldCanonicalDeviceId, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.SelectableLabel("New Device ID: " + device.newCanonicalDeviceId, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.SelectableLabel("Old Data Type: " + device.oldDataTypeName, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.SelectableLabel("New Data Type: " + device.newDataTypeName, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.SelectableLabel("Old Scripts: " + device.oldScriptFolder, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.SelectableLabel("New Scripts: " + device.newScriptFolder, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        if (rewriteScripts) EditorGUILayout.SelectableLabel("Script folders in scope: " + plan.textRewrites.Count, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
    }

    bool CanApply()
    {
        if (mode == DeviceRenameMode.RenameCollection) return sourceCollectionManifest != null;
        if (mode == DeviceRenameMode.MoveDeviceToCollection) return HasSelectedDevice() && targetCollectionManifest != null;
        return HasSelectedDevice();
    }

    string GetButtonLabel()
    {
        if (mode == DeviceRenameMode.RenameCollection) return "Rename Collection";
        if (mode == DeviceRenameMode.MoveDeviceToCollection) return "Move Device To Collection";
        return "Rename Device";
    }

    void ApplySelectedOperation()
    {
        DeviceRenamePlan plan = DeviceRenamePlanner.Build(BuildRequest());
        string error = DeviceRenameValidator.Validate(plan);
        if (!string.IsNullOrWhiteSpace(error))
        {
            EditorUtility.DisplayDialog("Cannot Apply Rename", error, "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(plan.Verb + " " + (mode == DeviceRenameMode.RenameCollection ? "Collection" : "Device"), plan.Verb + " " + plan.OldLabel + " to " + plan.NewLabel + "?", plan.Verb, "Cancel")) return;

        List<string> errors = new List<string>();
        DeviceRenameAssetMover.Apply(plan, DeviceRenameAssetMoveStage.BeforeText, errors);
        DeviceRenameTextUpdater.Apply(plan, errors);
        DeviceRenameManifestUpdater.Apply(plan);
        DeviceRenameAssetMover.Apply(plan, DeviceRenameAssetMoveStage.AfterText, errors);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        OSLDeviceRegistry.Refresh();

        if (errors.Count > 0)
        {
            Debug.LogError("OpenSoundLab Device Rename: Completed with " + errors.Count + " issue(s):\n" + string.Join("\n", errors.ToArray()));
            EditorUtility.DisplayDialog("Completed With Issues", string.Join("\n", errors.ToArray()), "OK");
            return;
        }

        Debug.Log("OpenSoundLab Device Rename: Operation completed.");
        EditorUtility.DisplayDialog("Rename Completed", "Operation completed. Unity may recompile generated scripts before validation is fully up to date.", "OK");
        LoadCurrentValues();
    }

    DeviceRenameRequest BuildRequest()
    {
        return new DeviceRenameRequest
        {
            mode = mode,
            sourceCollectionManifest = sourceCollectionManifest,
            targetCollectionManifest = targetCollectionManifest,
            deviceIndex = deviceIndex,
            makerDomain = makerDomain,
            collectionLocalId = collectionLocalId,
            collectionDisplayName = collectionDisplayName,
            localId = localId,
            displayName = displayName,
            category = category,
            renameAssets = renameAssets,
            rewriteScripts = rewriteScripts,
            addLegacyAliases = addLegacyAliases
        };
    }

    void LoadCurrentValues()
    {
        if (sourceCollectionManifest != null)
        {
            DeviceRenamePlanner.SplitCollectionId(sourceCollectionManifest.collectionId, out makerDomain, out collectionLocalId);
            collectionDisplayName = sourceCollectionManifest.displayName;
        }

        OSLDeviceDefinition device = GetSelectedDevice();
        if (device == null) return;

        localId = DeviceRenamePlanner.GetDeviceLocalId(device);
        displayName = device.displayName;
        category = device.category;
    }

    bool HasSelectedDevice()
    {
        return GetSelectedDevice() != null;
    }

    OSLDeviceDefinition GetSelectedDevice()
    {
        return DeviceRenamePlanner.GetDevice(sourceCollectionManifest, deviceIndex);
    }

    static GUIContent Content(string text, string tooltip)
    {
        return new GUIContent(text, tooltip);
    }
}
