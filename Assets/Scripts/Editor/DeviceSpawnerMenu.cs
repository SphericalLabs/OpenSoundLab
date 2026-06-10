using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Mirror;
using UnityEditor;
using UnityEngine;

public class DeviceSpawnerMenu : EditorWindow
{
    private const string MenuRoot = "OpenSoundLab/Play Mode/Device Spawner";
    private const string LocalScenePath = "Assets/Scenes/oslLocalNetworkScene.unity";
    private const string LocalNetworkManagerGuid = "96e6d8119da164ac1902626d138102d1";

    private static readonly Regex prefabGuidPattern = new Regex(@"guid:\s*([a-f0-9]{32})", RegexOptions.Compiled);

    private Vector2 scrollPosition;
    private string searchFilter = string.Empty;
    private string sourceLabel = string.Empty;
    private string refreshMessage = string.Empty;
    private List<GameObject> spawnablePrefabs = new List<GameObject>();

    [MenuItem(MenuRoot)]
    public static void ShowWindow()
    {
        DeviceSpawnerMenu window = GetWindow<DeviceSpawnerMenu>("Device Spawner");
        window.minSize = new Vector2(280f, 320f);
        window.RefreshSpawnablePrefabs();
    }

    private void OnEnable()
    {
        RefreshSpawnablePrefabs();
        EditorApplication.playModeStateChanged += handlePlayModeChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= handlePlayModeChanged;
    }

    private void OnFocus()
    {
        RefreshSpawnablePrefabs();
    }

    private void OnGUI()
    {
        drawToolbar();
        drawStatus();
        drawPrefabList();
    }

    private void handlePlayModeChanged(PlayModeStateChange state)
    {
        RefreshSpawnablePrefabs();
        Repaint();
    }

    private void drawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Search", GUILayout.Width(42f));
        searchFilter = GUILayout.TextField(searchFilter, GUILayout.ExpandWidth(true));

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
        {
            RefreshSpawnablePrefabs();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void drawStatus()
    {
        string modeText = Application.isPlaying ? "Play Mode" : "Edit Mode";
        EditorGUILayout.LabelField($"Source: {sourceLabel}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"State: {modeText}", EditorStyles.miniLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Spawning is only available in Play Mode. The list below is still populated from the local network scene.", MessageType.Info);
        }
        else if (NetworkSpawnManager.Instance == null)
        {
            EditorGUILayout.HelpBox("NetworkSpawnManager.Instance is null. Enter the local network scene before spawning devices.", MessageType.Warning);
        }

        if (!string.IsNullOrEmpty(refreshMessage))
        {
            EditorGUILayout.HelpBox(refreshMessage, MessageType.Warning);
        }

        EditorGUILayout.Space(4f);
    }

    private void drawPrefabList()
    {
        if (spawnablePrefabs.Count == 0)
        {
            EditorGUILayout.HelpBox("No registered spawnable prefabs were found.", MessageType.Warning);
            return;
        }

        bool canSpawn = Application.isPlaying && NetworkSpawnManager.Instance != null;
        int visibleCount = 0;

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (GameObject prefab in spawnablePrefabs)
        {
            if (!matchesFilter(prefab.name))
            {
                continue;
            }

            visibleCount++;

            EditorGUI.BeginDisabledGroup(!canSpawn);
            if (GUILayout.Button(prefab.name, GUILayout.Height(26f)))
            {
                Spawn(prefab.name);
            }
            EditorGUI.EndDisabledGroup();
        }

        EditorGUILayout.EndScrollView();

        if (visibleCount == 0)
        {
            EditorGUILayout.HelpBox("No devices match the current search filter.", MessageType.Info);
        }
    }

    private bool matchesFilter(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(searchFilter))
        {
            return true;
        }

        return prefabName.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void RefreshSpawnablePrefabs()
    {
        refreshMessage = string.Empty;

        if (TryGetRuntimePrefabs(out List<GameObject> runtimePrefabs))
        {
            spawnablePrefabs = buildUniquePrefabList(runtimePrefabs);
            sourceLabel = "NetworkManager.singleton.spawnPrefabs";
            return;
        }

        if (TryGetScenePrefabs(out List<GameObject> scenePrefabs))
        {
            spawnablePrefabs = buildUniquePrefabList(scenePrefabs);
            sourceLabel = $"{LocalScenePath} -> LocalNetworkManager.spawnPrefabs";
            return;
        }

        spawnablePrefabs = new List<GameObject>();
        sourceLabel = $"{LocalScenePath} -> LocalNetworkManager.spawnPrefabs";
        refreshMessage = "Failed to resolve registered spawnable prefabs from the local network scene.";
    }

    private bool TryGetRuntimePrefabs(out List<GameObject> prefabs)
    {
        prefabs = new List<GameObject>();

        if (NetworkManager.singleton == null || NetworkManager.singleton.spawnPrefabs == null)
        {
            return false;
        }

        foreach (GameObject prefab in NetworkManager.singleton.spawnPrefabs)
        {
            if (prefab == null)
            {
                continue;
            }

            prefabs.Add(prefab);
        }

        return prefabs.Count > 0;
    }

    private bool TryGetScenePrefabs(out List<GameObject> prefabs)
    {
        prefabs = new List<GameObject>();

        string projectRootPath = Directory.GetParent(Application.dataPath).FullName;
        string sceneFilePath = Path.Combine(projectRootPath, LocalScenePath);
        if (!File.Exists(sceneFilePath))
        {
            return false;
        }

        bool foundLocalNetworkManager = false;
        bool foundSpawnPrefabs = false;

        foreach (string line in File.ReadLines(sceneFilePath))
        {
            if (!foundLocalNetworkManager)
            {
                if (line.Contains($"guid: {LocalNetworkManagerGuid}, type: 3"))
                {
                    foundLocalNetworkManager = true;
                }

                continue;
            }

            if (!foundSpawnPrefabs)
            {
                if (line == "  spawnPrefabs:")
                {
                    foundSpawnPrefabs = true;
                }
                else if (line.StartsWith("--- !u!"))
                {
                    break;
                }

                continue;
            }

            if (!line.StartsWith("  - "))
            {
                break;
            }

            Match match = prefabGuidPattern.Match(line);
            if (!match.Success)
            {
                continue;
            }

            string prefabPath = AssetDatabase.GUIDToAssetPath(match.Groups[1].Value);
            if (string.IsNullOrEmpty(prefabPath))
            {
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                prefabs.Add(prefab);
            }
        }

        return prefabs.Count > 0;
    }

    private List<GameObject> buildUniquePrefabList(List<GameObject> prefabs)
    {
        List<GameObject> uniquePrefabs = new List<GameObject>();

        foreach (GameObject prefab in prefabs)
        {
            if (prefab == null)
            {
                continue;
            }

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            bool alreadyAdded = false;

            foreach (GameObject existingPrefab in uniquePrefabs)
            {
                if (AssetDatabase.GetAssetPath(existingPrefab) == prefabPath)
                {
                    alreadyAdded = true;
                    break;
                }
            }

            if (!alreadyAdded)
            {
                uniquePrefabs.Add(prefab);
            }
        }

        uniquePrefabs.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
        return uniquePrefabs;
    }

    private static void Spawn(string deviceName)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Device Spawner only works in Play Mode.");
            return;
        }

        if (NetworkSpawnManager.Instance == null)
        {
            Debug.LogError("NetworkSpawnManager Instance is null. Cannot spawn device.");
            return;
        }

        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;

        if (SceneView.lastActiveSceneView != null)
        {
            spawnPosition = SceneView.lastActiveSceneView.pivot;
            if (SceneView.lastActiveSceneView.camera != null)
            {
                Vector3 direction = SceneView.lastActiveSceneView.camera.transform.position - spawnPosition;
                if (direction != Vector3.zero)
                {
                    spawnRotation = Quaternion.LookRotation(direction);
                }
            }
        }

        GetOffsets(deviceName, out Vector3 localPositionOffset, out Vector3 localRotationOffset);

        if (NetworkManager.singleton != null && NetworkManager.singleton.mode == NetworkManagerMode.ClientOnly)
        {
            NetworkSpawnManager.Instance.CmdCreateItem(deviceName, spawnPosition, spawnRotation, localPositionOffset, localRotationOffset);
        }
        else
        {
            NetworkSpawnManager.Instance.CreateItem(deviceName, spawnPosition, spawnRotation, localPositionOffset, localRotationOffset);
        }

        Debug.Log($"Spawned {deviceName} via Device Spawner");
    }

    private static void GetOffsets(string deviceName, out Vector3 localPositionOffset, out Vector3 localRotationOffset)
    {
        localPositionOffset = Vector3.zero;
        localRotationOffset = Vector3.zero;

        switch (deviceName)
        {
            case "Tapes":
                localPositionOffset = new Vector3(.1f, .02f, .15f);
                localRotationOffset = new Vector3(0, 180, 0);
                break;
            case "Controller":
                localPositionOffset = new Vector3(0f, 0f, -0.15f);
                break;
            case "Xylophone":
                localRotationOffset = new Vector3(90, 0, 0);
                localPositionOffset = new Vector3(0.15f, 0f, -0.05f);
                break;
            case "Drum":
            case "Keyboard":
            case "MixerTwo":
                localRotationOffset = new Vector3(90, 0, 0);
                break;
        }
    }
}
