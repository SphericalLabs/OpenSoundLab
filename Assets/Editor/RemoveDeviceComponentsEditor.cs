// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
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
    private const string prefabsFolder = "Assets/Resources/Prefabs";
    private const string menuPrefabsFolder = "Assets/Resources/MenuPrefabs";
    private const string defaultMaterialPath = "Assets/Materials/uncategorized/InstrumentSelectMat.mat";

    private string[] prefabPaths = new string[0];
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
            newMaterial = AssetDatabase.LoadAssetAtPath<Material>(defaultMaterialPath);
        }
        refreshPrefabs();
    }

    private void OnGUI()
    {
        GUILayout.Label("Create Menu Variant", EditorStyles.boldLabel);
        if (prefabNames.Length == 0)
        {
            EditorGUILayout.HelpBox("No prefabs found in Assets/Resources/Prefabs.", MessageType.Info);
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
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabsFolder });
        prefabPaths = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => !path.EndsWith("_Menu.prefab"))
            .OrderBy(path => Path.GetFileNameWithoutExtension(path))
            .ToArray();
        prefabNames = prefabPaths
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .ToArray();

        if (selectedIndex >= prefabPaths.Length) selectedIndex = 0;
        Repaint();
    }

    private void createMenuVariant()
    {
        if (prefabPaths.Length == 0) return;

        string sourcePath = prefabPaths[selectedIndex];
        if (!Directory.Exists(menuPrefabsFolder)) Directory.CreateDirectory(menuPrefabsFolder);

        GameObject sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (sourceAsset == null)
        {
            Debug.LogError($"Could not load prefab at {sourcePath}");
            return;
        }

        string menuPath = Path.Combine(menuPrefabsFolder, sourceAsset.name + "_Menu.prefab");
        if (File.Exists(menuPath))
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Menu Prefab Exists",
                $"{sourceAsset.name}_Menu.prefab already exists. Overwrite?",
                "Overwrite",
                "Cancel");
            if (!overwrite)
            {
                return;
            }
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(sourceAsset);
        if (instance == null)
        {
            Debug.LogError($"Could not instantiate prefab at {sourcePath}");
            return;
        }

        removeComponentsFromPrefab(instance, searchInChildren, newMaterial);
        PrefabUtility.SaveAsPrefabAsset(instance, menuPath);
        DestroyImmediate(instance);

        AssetDatabase.Refresh();
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(menuPath));
    }

    private void removeComponentsFromPrefab(GameObject target, bool searchChildren, Material replacement)
    {
        GameObject toolObject = new GameObject("RemoveDeviceComponents_Temp");
        toolObject.hideFlags = HideFlags.HideAndDontSave;
        RemoveDeviceComponents tool = toolObject.AddComponent<RemoveDeviceComponents>();
        tool.searchInChildren = searchChildren;
        tool.newMaterial = replacement;
        tool.targets = new[] { target };
        tool.RemoveComponents();
        DestroyImmediate(toolObject);
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
