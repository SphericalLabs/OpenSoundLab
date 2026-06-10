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
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using Mirror;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Editor_PlayModePatchMenu
{
    const string newPatchMenuPath = "OpenSoundLab/Play Mode/New Patch";
    const string loadPatchMenuPath = "OpenSoundLab/Play Mode/Load Patch";
    const string savePatchMenuPath = "OpenSoundLab/Play Mode/Save Patch";
    const string loadMenuPath = "OpenSoundLab/Play Mode/Load LastPlayModePatch";
    const string saveMenuPath = "OpenSoundLab/Play Mode/Save LastPlayModePatch";
    const string loadOnPlayMenuPath = "OpenSoundLab/Play Mode/Load LastPlayModePatch on Play";
    const string loadOnPlayPrefKey = "LoadLastPlayModePatchOnPlay";
    const string patchFileName = "LastPlayModePatch.xml";
    static bool loadOnPlayPending;

    static Editor_PlayModePatchMenu()
    {
        EditorApplication.playModeStateChanged += handlePlayModeStateChanged;
        EditorApplication.update += handleEditorUpdate;
        Menu.SetChecked(loadOnPlayMenuPath, isLoadOnPlayEnabled());
    }

    [MenuItem(loadOnPlayMenuPath, false, 6)]
    public static void ToggleLoadOnPlay()
    {
        bool enabled = !isLoadOnPlayEnabled();
        PlayerPrefs.SetInt(loadOnPlayPrefKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        Menu.SetChecked(loadOnPlayMenuPath, enabled);
    }

    [MenuItem(loadOnPlayMenuPath, true, 6)]
    public static bool ToggleLoadOnPlayValidate()
    {
        Menu.SetChecked(loadOnPlayMenuPath, isLoadOnPlayEnabled());
        return true;
    }

    [MenuItem(newPatchMenuPath, false, 1)]
    public static void StartNewPatch()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("New Patch only works in Play Mode.");
            return;
        }

        if (NetworkServer.active)
        {
            SaveLoadInterface saveLoad = SaveLoadInterface.instance;
            if (saveLoad == null)
            {
                Debug.LogWarning("SaveLoadInterface is not available.");
                return;
            }
            saveLoad.StartNewPatch();
            Debug.Log("Started new patch.");
        }
        else if (NetworkClient.isConnected && NetworkClient.localPlayer != null)
        {
            var player = NetworkClient.localPlayer.GetComponent<VRNetworkPlayer>();
            if (player != null)
            {
                player.CmdNewPatch();
                Debug.Log("Requested Server to Start New Patch.");
            }
        }
        else
        {
            Debug.LogWarning("New Patch requires active Network connection.");
        }
    }

    [MenuItem(newPatchMenuPath, true, 1)]
    public static bool StartNewPatchValidate()
    {
        return Application.isPlaying &&
               ((NetworkServer.active && SaveLoadInterface.instance != null) ||
                (NetworkClient.isConnected && NetworkClient.localPlayer != null));
    }

    [MenuItem(loadPatchMenuPath, false, 2)]
    public static void LoadPatch()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Load Patch only works in Play Mode.");
            return;
        }

        string path = EditorUtility.OpenFilePanelWithFilters(
            "Load Patch",
            getPatchDirectory(),
            new[] { "XML files", "xml", "All files", "*" });

        if (string.IsNullOrEmpty(path)) return;

        loadPatch(path, "Patch");
    }

    [MenuItem(loadPatchMenuPath, true, 2)]
    public static bool LoadPatchValidate()
    {
        return canLoadPatch();
    }

    [MenuItem(savePatchMenuPath, false, 3)]
    public static void SavePatch()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Save Patch only works in Play Mode.");
            return;
        }

        if (!NetworkServer.active)
        {
            Debug.LogWarning("Save Patch requires an active Mirror server.");
            return;
        }

        SaveLoadInterface saveLoad = SaveLoadInterface.instance;
        if (saveLoad == null)
        {
            Debug.LogWarning("SaveLoadInterface is not available.");
            return;
        }

        string path = EditorUtility.SaveFilePanel(
            "Save Patch",
            getDocumentsOpenSoundLabDirectory(),
            getDefaultPatchFileName(),
            "xml");

        if (string.IsNullOrEmpty(path)) return;

        path = ensureXmlExtension(path);
        ensurePatchDirectory(path);
        saveLoad.Save(path);
        Debug.Log("Saved Patch to " + path);
    }

    [MenuItem(savePatchMenuPath, true, 3)]
    public static bool SavePatchValidate()
    {
        return Application.isPlaying && NetworkServer.active && SaveLoadInterface.instance != null;
    }

    [MenuItem(loadMenuPath, false, 4)]
    public static void LoadLastPlayModePatch()
    {
        loadPatch(getPatchPath(), "LastPlayModePatch");
    }

    [MenuItem(loadMenuPath, true, 4)]
    public static bool LoadLastPlayModePatchValidate()
    {
        return canLoadPatch();
    }

    [MenuItem(saveMenuPath, false, 5)]
    public static void SaveLastPlayModePatch()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Save LastPlayModePatch only works in Play Mode.");
            return;
        }

        saveLastPlayModePatch();
    }

    [MenuItem(saveMenuPath, true, 5)]
    public static bool SaveLastPlayModePatchValidate()
    {
        return Application.isPlaying &&
               ((NetworkServer.active && SaveLoadInterface.instance != null) ||
                (NetworkClient.isConnected && NetworkClient.localPlayer != null));
    }

    static bool canLoadPatch()
    {
        return Application.isPlaying && (NetworkServer.active || (NetworkClient.isConnected && NetworkClient.localPlayer != null));
    }

    static void loadPatch(string path, string patchLabel)
    {
        if (NetworkServer.active)
        {
            clearPatchForManualLoad();
            tryLoadPatch(path, patchLabel);
        }
        else if (NetworkClient.isConnected && NetworkClient.localPlayer != null)
        {
            var player = NetworkClient.localPlayer.GetComponent<VRNetworkPlayer>();
            if (player != null)
            {
                if (player.RequestLoadPatchFromLocalFile(path))
                {
                    Debug.Log("Requested Server to Load " + patchLabel + " from the client patch file.");
                }
            }
        }
        else
        {
            Debug.LogWarning("Cannot Load " + patchLabel + ": Network not ready.");
        }
    }

    static void saveLastPlayModePatch()
    {
        if (NetworkServer.active)
        {
            SaveLoadInterface saveLoad = SaveLoadInterface.instance;
            if (saveLoad == null)
            {
                Debug.LogWarning("SaveLoadInterface is not available.");
                return;
            }
            string path = getPatchPath();
            ensurePatchDirectory(path);
            saveLoad.Save(path);
            Debug.Log($"Saved LastPlayModePatch to {path}");
        }
        else if (NetworkClient.isConnected && NetworkClient.localPlayer != null)
        {
            var player = NetworkClient.localPlayer.GetComponent<VRNetworkPlayer>();
            if (player != null)
            {
                player.CmdSaveLastPlayModePatch();
                Debug.Log("Requested Server to Save LastPlayModePatch.");
            }
        }
        else
        {
            Debug.LogWarning("Cannot Save LastPlayModePatch: Network not ready.");
        }
    }

    static void handlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && isLoadOnPlayEnabled())
        {
            loadOnPlayPending = true;
            return;
        }

        if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.ExitingEditMode)
        {
            loadOnPlayPending = false;
        }
    }

    static void handleEditorUpdate()
    {
        if (!loadOnPlayPending)
        {
            return;
        }

        if (!Application.isPlaying)
        {
            loadOnPlayPending = false;
            return;
        }

        // On Client, we might need to wait until connected
        if (NetworkServer.active)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!isTargetPlayScene(activeScene)) return;
            if (!isReadyToLoad(activeScene)) return;

            loadOnPlayPending = false;
            tryLoadLastPlayModePatch();
        }
        else if (NetworkClient.isConnected && NetworkClient.localPlayer != null)
        {
            // Auto-load is for Server only to avoid chaos
            if (NetworkClient.isConnected)
                loadOnPlayPending = false;
        }
    }

    static bool isReadyToLoad(Scene activeScene)
    {
        if (!activeScene.isLoaded) return false;
        if (!NetworkServer.active) return false;
        if (SaveLoadInterface.instance == null) return false;
        return true;
    }

    static void clearPatchForManualLoad()
    {
        if (!Application.isPlaying) return;
        if (!NetworkServer.active) return;

        SaveLoadInterface saveLoad = SaveLoadInterface.instance;
        if (saveLoad == null) return;

        saveLoad.StartNewPatch();
    }

    static void tryLoadLastPlayModePatch()
    {
        tryLoadPatch(getPatchPath(), "LastPlayModePatch");
    }

    static void tryLoadPatch(string path, string patchLabel)
    {
        if (!Application.isPlaying) return;

        if (!NetworkServer.active)
        {
            Debug.Log("Load " + patchLabel + " skipped because Mirror server is not active.");
            return;
        }

        SaveLoadInterface saveLoad = SaveLoadInterface.instance;
        if (saveLoad == null)
        {
            Debug.LogWarning("SaveLoadInterface is not available.");
            return;
        }

        if (!File.Exists(path))
        {
            Debug.LogWarning(patchLabel + " not found at " + path);
            return;
        }

        saveLoad.Load(path);
    }

    static bool isLoadOnPlayEnabled()
    {
        return PlayerPrefs.GetInt(loadOnPlayPrefKey, 0) == 1;
    }

    static bool isTargetPlayScene(Scene scene)
    {
        int index = scene.buildIndex;
        return index == (int)masterControl.Scenes.Local || index == (int)masterControl.Scenes.Relay;
    }

    static string getPatchPath()
    {
        string baseDir = masterControl.instance != null ? masterControl.instance.SaveDir : null;
        if (string.IsNullOrEmpty(baseDir))
        {
            baseDir = Application.persistentDataPath;
        }

        string savesDir = Path.Combine(baseDir, "Saves");
        return Path.Combine(savesDir, patchFileName);
    }

    static string getPatchDirectory()
    {
        return Path.GetDirectoryName(getPatchPath());
    }

    static string getDocumentsOpenSoundLabDirectory()
    {
        string documentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrEmpty(documentsDir)) return getPatchDirectory();

        string openSoundLabDir = Path.Combine(documentsDir, "OpenSoundLab");
        if (Directory.Exists(openSoundLabDir)) return openSoundLabDir;
        if (Directory.Exists(documentsDir)) return documentsDir;
        return getPatchDirectory();
    }

    static string getDefaultPatchFileName()
    {
        return string.Format("{0:yyyy-MM-dd_HH-mm-ss}.xml", DateTime.Now);
    }

    static string ensureXmlExtension(string path)
    {
        if (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) return path;
        return path + ".xml";
    }

    static void ensurePatchDirectory(string path)
    {
        string dir = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(dir)) return;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }
}
