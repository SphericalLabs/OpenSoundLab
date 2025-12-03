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
    const string saveMenuPath = "OpenSoundLab/Play Mode/Save LastPlayModePatch";
    const string loadOnPlayPrefKey = "LoadLastPlayModePatchOnPlay";
    const string patchFileName = "LastPlayModePatch.xml";
    const double targetSceneDelaySeconds = 0.5d;

    static bool loadOnPlayPending;
    static double targetSceneEnteredTime = -1;

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
            targetSceneEnteredTime = -1;
            return;
        }

        if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.ExitingEditMode)
        {
            loadOnPlayPending = false;
            targetSceneEnteredTime = -1;
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
            targetSceneEnteredTime = -1;
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!isTargetPlayScene(activeScene))
        {
            targetSceneEnteredTime = -1;
            return;
        }

        if (targetSceneEnteredTime < 0)
        {
            targetSceneEnteredTime = EditorApplication.timeSinceStartup;
            return;
        }

        double elapsed = EditorApplication.timeSinceStartup - targetSceneEnteredTime;
        if (elapsed < targetSceneDelaySeconds)
        {
            return;
        }

        loadOnPlayPending = false;
        targetSceneEnteredTime = -1;
        tryLoadLastPlayModePatch();
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
