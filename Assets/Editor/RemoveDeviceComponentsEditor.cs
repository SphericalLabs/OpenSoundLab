// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2026 OSLLv1 Sphericals OpenSoundLab
//
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
//
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
//
//
// Copyright © 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright © 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright © 2017 Apache 2.0 Google LLC SoundStage VR
//
// Licensed under the Apache License, Version 2.0 (the "License");
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class RemoveDeviceComponentsEditorWindow : EditorWindow
{
    private string[] prefabPaths = new string[0];
    private string[] menuPrefabPaths = new string[0];
    private string[] prefabNames = new string[0];
    private int selectedIndex = 0;
    private bool searchInChildren = true;
    private Material newMaterial;

    [MenuItem("OpenSoundLab/Devices/Create Menu Variant")]
    public static void ShowWindow()
    {
        GetWindow<RemoveDeviceComponentsEditorWindow>("Menu Variant Creator");
    }

    private void OnEnable()
    {
        if (newMaterial == null)
        {
            newMaterial = OSLMenuPrefabVariantUtility.LoadDefaultMaterial();
        }
        refreshPrefabs();
    }

    private void OnGUI()
    {
        GUILayout.Label("Create Menu Variant", EditorStyles.boldLabel);
        if (prefabNames.Length == 0)
        {
            EditorGUILayout.HelpBox("No enabled, menu-visible device prefabs were found in the OSL device manifests.", MessageType.Info);
            if (GUILayout.Button("Refresh Prefab List")) refreshPrefabs();
            return;
        }

        selectedIndex = EditorGUILayout.Popup("Source Prefab", selectedIndex, prefabNames);
        searchInChildren = EditorGUILayout.Toggle("Search In Children", searchInChildren);
        newMaterial = (Material)EditorGUILayout.ObjectField("New Material", newMaterial, typeof(Material), false);

        EditorGUILayout.Space();
        if (GUILayout.Button("Create Menu Variant"))
        {
            createMenuVariant();
        }

        if (GUILayout.Button("Refresh Prefab List")) refreshPrefabs();
    }

    private void refreshPrefabs()
    {
        OSLDeviceRegistry.Refresh();

        List<string> sourcePaths = new List<string>();
        List<string> destinationPaths = new List<string>();
        List<string> names = new List<string>();

        List<OSLDeviceRegistration> registrations = OSLDeviceRegistry.GetAll(true);
        for (int i = 0; i < registrations.Count; ++i)
        {
            OSLDeviceRegistration registration = registrations[i];
            if (registration == null || !registration.enabled || !registration.showInMenu) continue;

            GameObject prefab = registration.LoadPrefab();
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrWhiteSpace(prefabPath)) continue;

            sourcePaths.Add(prefabPath);
            destinationPaths.Add(getMenuPrefabPath(registration, prefabPath));
            names.Add(registration.displayName + " (" + registration.deviceId + ")");
        }

        prefabPaths = sourcePaths.ToArray();
        menuPrefabPaths = destinationPaths.ToArray();
        prefabNames = names.ToArray();

        if (selectedIndex >= prefabPaths.Length) selectedIndex = 0;
        Repaint();
    }

    private void createMenuVariant()
    {
        if (prefabPaths.Length == 0) return;

        string sourcePath = prefabPaths[selectedIndex];
        string menuPath = menuPrefabPaths[selectedIndex];
        if (File.Exists(menuPath))
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Menu Prefab Exists",
                Path.GetFileName(menuPath) + " already exists. Overwrite?",
                "Overwrite",
                "Cancel");
            if (!overwrite)
            {
                return;
            }
        }

        OSLMenuPrefabVariantUtility.CreateFromPrefab(sourcePath, menuPath, searchInChildren, newMaterial);

        AssetDatabase.Refresh();
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(menuPath));
    }

    private string getMenuPrefabPath(OSLDeviceRegistration registration, string prefabPath)
    {
        string resourceRoot = getResourceRoot(prefabPath);
        if (!string.IsNullOrWhiteSpace(resourceRoot) && !string.IsNullOrWhiteSpace(registration.menuPrefabResourcePath))
        {
            return resourceRoot + "/" + registration.menuPrefabResourcePath + ".prefab";
        }

        string folder = Path.GetDirectoryName(prefabPath);
        if (string.IsNullOrWhiteSpace(folder)) folder = "Assets";
        string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
        return folder.Replace("/Prefabs", "/MenuPrefabs") + "/" + prefabName + "_Menu.prefab";
    }

    private string getResourceRoot(string prefabPath)
    {
        string normalizedPath = prefabPath.Replace('\\', '/');
        int resourcesIndex = normalizedPath.IndexOf("/Resources/", System.StringComparison.Ordinal);
        if (resourcesIndex < 0) return "";
        return normalizedPath.Substring(0, resourcesIndex + "/Resources".Length);
    }
}

[CustomEditor(typeof(RemoveDeviceComponents))]
public class RemoveDeviceComponentsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var myScript = target as RemoveDeviceComponents;
        if (GUILayout.Button("Remove Components"))
        {
            myScript.RemoveComponents();
        }
    }
}
