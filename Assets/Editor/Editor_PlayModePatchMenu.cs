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
    const string loadOnPlayPrefKey = "LoadLastPlayModePatchOnPlay";
    const string patchFileName = "LastPlayModePatch.xml";
    static bool loadOnPlayPending;

    static Editor_PlayModePatchMenu()
    {
        EditorApplication.playModeStateChanged += handlePlayModeStateChanged;
        EditorApplication.update += handleEditorUpdate;
        Menu.SetChecked(loadOnPlayMenuPath, isLoadOnPlayEnabled());
    }

    [MenuItem(loadOnPlayMenuPath)]
    public static void ToggleLoadOnPlay()
    {
        bool enabled = !isLoadOnPlayEnabled();
        PlayerPrefs.SetInt(loadOnPlayPrefKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        Menu.SetChecked(loadOnPlayMenuPath, enabled);
    }

    [MenuItem(loadOnPlayMenuPath, true)]
    public static bool ToggleLoadOnPlayValidate()
    {
        Menu.SetChecked(loadOnPlayMenuPath, isLoadOnPlayEnabled());
        return true;
    }

    [MenuItem(loadMenuPath)]
    public static void LoadLastPlayModePatch()
    {
        tryLoadLastPlayModePatch();
    }

    [MenuItem(loadMenuPath, true)]
    public static bool LoadLastPlayModePatchValidate()
    {
        return Application.isPlaying;
    }

    [MenuItem(saveMenuPath)]
    public static void SaveLastPlayModePatch()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Save LastPlayModePatch only works in Play Mode.");
            return;
        }

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

    [MenuItem(saveMenuPath, true)]
    public static bool SaveLastPlayModePatchValidate()
    {
        return Application.isPlaying && SaveLoadInterface.instance != null;
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

        Scene activeScene = SceneManager.GetActiveScene();
        if (!isTargetPlayScene(activeScene))
        {
            return;
        }

        if (!isReadyToLoad(activeScene))
        {
            return;
        }

        loadOnPlayPending = false;
        tryLoadLastPlayModePatch();
    }

    static bool isReadyToLoad(Scene activeScene)
    {
        if (!activeScene.isLoaded) return false;
        if (!NetworkServer.active) return false;
        if (SaveLoadInterface.instance == null) return false;
        return true;
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
