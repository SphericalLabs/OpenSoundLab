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

using System.IO;
using Mirror;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Editor_PlayModePatchMenu
{
    const string loadOnPlayMenuPath = "OpenSoundLab/Play Mode/Load LastPlayModePatch on Play";
    const string loadMenuPath = "OpenSoundLab/Play Mode/Load LastPlayModePatch";
    const string saveMenuPath = "OpenSoundLab/Play Mode/Save LastPlayModePatch";
    const string newPatchMenuPath = "OpenSoundLab/Play Mode/New Patch";
    const string loadOnPlayPrefKey = "LoadLastPlayModePatchOnPlay";
    const string patchFileName = "LastPlayModePatch.xml";
    static bool loadOnPlayPending;

    static Editor_PlayModePatchMenu()
    {
        EditorApplication.playModeStateChanged += handlePlayModeStateChanged;
        EditorApplication.update += handleEditorUpdate;
        Menu.SetChecked(loadOnPlayMenuPath, isLoadOnPlayEnabled());
    }

    [MenuItem(loadOnPlayMenuPath, false, 20)]
    public static void ToggleLoadOnPlay()
    {
        bool enabled = !isLoadOnPlayEnabled();
        PlayerPrefs.SetInt(loadOnPlayPrefKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        Menu.SetChecked(loadOnPlayMenuPath, enabled);
    }

    [MenuItem(loadOnPlayMenuPath, true, 20)]
    public static bool ToggleLoadOnPlayValidate()
    {
        Menu.SetChecked(loadOnPlayMenuPath, isLoadOnPlayEnabled());
        return true;
    }

    [MenuItem(loadMenuPath, false, 10)]
    public static void LoadLastPlayModePatch()
    {
        if (NetworkServer.active)
        {
            clearPatchForManualLoad();
            tryLoadLastPlayModePatch();
        }
        else if (NetworkClient.isConnected && NetworkClient.localPlayer != null)
        {
            var player = NetworkClient.localPlayer.GetComponent<VRNetworkPlayer>();
            if (player != null)
            {
                if (player.RequestLoadPatchFromLocalFile(getPatchPath()))
                {
                    Debug.Log("Requested Server to Load LastPlayModePatch from the client patch file.");
                }
            }
        }
        else
        {
             Debug.LogWarning("Cannot Load LastPlayModePatch: Network not ready.");
        }
    }

    [MenuItem(loadMenuPath, true, 10)]
    public static bool LoadLastPlayModePatchValidate()
    {
        return Application.isPlaying && (NetworkServer.active || (NetworkClient.isConnected && NetworkClient.localPlayer != null));
    }

    [MenuItem(saveMenuPath, false, 30)]
    public static void SaveLastPlayModePatch()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Save LastPlayModePatch only works in Play Mode.");
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

    [MenuItem(saveMenuPath, true, 30)]
    public static bool SaveLastPlayModePatchValidate()
    {
        return Application.isPlaying &&
               ((NetworkServer.active && SaveLoadInterface.instance != null) ||
                (NetworkClient.isConnected && NetworkClient.localPlayer != null));
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
             // Client Auto Load?
             // If the user wants "Load LastPlayModePatch on Play" and they start as Client...
             // They might want to trigger the server to load it?
             // But if multiple clients join, restarting the patch every time a client joins is bad.
             // Usually "Load On Play" is for the host/server startup.
             // So I will NOT auto-trigger load on client join, unless explicitly requested.
             // The original code checked checks NetworkServer.active.
             // I will leave auto-load for Server only for now, as it disrupts other players if a client forces a reload.
             // Unless "Play Mode" implies single user context.
             // But simpler to keep auto-load as server-side feature to avoid chaos.
             // However, I should check if I should turn off `loadOnPlayPending` if I am a client so it doesn't spin forever?
             // Use a timeout or just check checks.

             // If I am client, I can't load locally anyway (need server).
             // Checking NetworkClient.isConnected is enough to say "I am fully initialized".
             // If I am client, and I see I am connected, I should probably stop pending.
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
        if (!Application.isPlaying) return;

        if (!NetworkServer.active)
        {
            Debug.Log("Load LastPlayModePatch skipped because Mirror server is not active.");
            return;
        }

        SaveLoadInterface saveLoad = SaveLoadInterface.instance;
        if (saveLoad == null)
        {
            Debug.LogWarning("SaveLoadInterface is not available.");
            return;
        }

        string path = getPatchPath();
        if (!File.Exists(path))
        {
            Debug.LogWarning($"LastPlayModePatch not found at {path}");
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
