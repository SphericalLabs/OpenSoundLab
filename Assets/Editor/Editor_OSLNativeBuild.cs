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

#if UNITY_EDITOR

using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class Editor_OSLNativeBuild
{
    const string RebuildMenuPath = "OpenSoundLab/OSLNative/Rebuild";
    const string RebuildOnBuildMenuPath = "OpenSoundLab/OSLNative/Rebuild on Build";
    const string RebuildOnBuildEditorPref = "OpenSoundLab.OSLNative.RebuildOnBuild";
    static readonly string[] pluginMetaTemplatePaths =
    {
        Path.Combine("arm64", "Release", "libOSLNative.so.meta"),
        Path.Combine("macos", "Release", "libOSLNative.dylib.meta"),
        Path.Combine("x64", "Release", "OSLNative.dll.meta")
    };
    static bool suppressRebuildOnBuild;

    public static bool RebuildOnBuild
    {
        get { return EditorPrefs.GetBool(RebuildOnBuildEditorPref, false); }
    }

    public static bool ShouldRebuildOnBuild
    {
        get { return RebuildOnBuild && !suppressRebuildOnBuild; }
    }

    [MenuItem(RebuildMenuPath, false, 3000)]
    public static void Rebuild()
    {
        RebuildAllPossiblePlatforms();
    }

    [MenuItem(RebuildOnBuildMenuPath, false, 3001)]
    public static void ToggleRebuildOnBuild()
    {
        bool enabled = !RebuildOnBuild;
        EditorPrefs.SetBool(RebuildOnBuildEditorPref, enabled);
        Menu.SetChecked(RebuildOnBuildMenuPath, enabled);
        Debug.Log("OpenSoundLab: OSLNative rebuild on build " + (enabled ? "enabled." : "disabled."));
    }

    [MenuItem(RebuildOnBuildMenuPath, true)]
    public static bool ValidateToggleRebuildOnBuild()
    {
        Menu.SetChecked(RebuildOnBuildMenuPath, RebuildOnBuild);
        return true;
    }

    public static void RebuildAllPossiblePlatforms()
    {
        string nativeDirectory = GetNativeDirectory();
        bool currentEditorPluginAlreadyExisted = CurrentEditorPluginAlreadyExists(nativeDirectory);
        Debug.Log("OpenSoundLab: Rebuilding OSLNative plugins...");

        try
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                RunProcess("/bin/bash", Quote(Path.Combine(nativeDirectory, "build_all_from_macos.sh")) + " all",
                    nativeDirectory);
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                string scriptPath = Path.Combine(nativeDirectory, "build_all_from_windows.bat");
                RunProcess("cmd.exe", "/c " + Quote(scriptPath) + " all", nativeDirectory);
            }
            else
            {
                throw new BuildFailedException("OSLNative rebuild is only supported from the macOS and Windows Unity editors.");
            }

            RestorePluginMetaTemplates(nativeDirectory);
        }
        finally
        {
            AssetDatabase.Refresh();
        }

        Debug.Log("OpenSoundLab: OSLNative rebuild completed.");
        LogCurrentEditorPluginRestartWarning(nativeDirectory, currentEditorPluginAlreadyExisted);
    }

    public static void RunBuildBatch(Action buildAction)
    {
        if (!RebuildOnBuild)
        {
            buildAction();
            return;
        }

        RebuildAllPossiblePlatforms();
        suppressRebuildOnBuild = true;
        try
        {
            buildAction();
        }
        finally
        {
            suppressRebuildOnBuild = false;
        }
    }

    static string GetNativeDirectory()
    {
        DirectoryInfo projectDirectory = Directory.GetParent(Application.dataPath);
        if (projectDirectory == null)
        {
            throw new BuildFailedException("Could not resolve project directory from Application.dataPath.");
        }

        string nativeDirectory = Path.Combine(projectDirectory.FullName, "OSLNative");
        if (!Directory.Exists(nativeDirectory))
        {
            throw new BuildFailedException("Could not find OSLNative directory at " + nativeDirectory);
        }

        return nativeDirectory;
    }

    static void RestorePluginMetaTemplates(string nativeDirectory)
    {
        DirectoryInfo projectDirectory = Directory.GetParent(nativeDirectory);
        if (projectDirectory == null)
        {
            throw new BuildFailedException("Could not resolve project directory from OSLNative directory.");
        }

        string templateDirectory = Path.Combine(nativeDirectory, "PluginMetaTemplates");
        string pluginDirectory = Path.Combine(projectDirectory.FullName, "Assets", "Plugins", "OSLNative");

        for (int i = 0; i < pluginMetaTemplatePaths.Length; i++)
        {
            string relativePath = pluginMetaTemplatePaths[i];
            string sourcePath = Path.Combine(templateDirectory, relativePath);
            string destinationPath = Path.Combine(pluginDirectory, relativePath);
            string pluginPath = destinationPath.Substring(0, destinationPath.Length - ".meta".Length);

            if (!File.Exists(sourcePath))
            {
                throw new BuildFailedException("Missing OSLNative plugin meta template at " + sourcePath);
            }

            if (!File.Exists(pluginPath))
            {
                Debug.Log("OpenSoundLab: Skipping OSLNative plugin meta restore because plugin was not built: "
                    + pluginPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            File.Copy(sourcePath, destinationPath, true);
        }
    }

    static bool CurrentEditorPluginAlreadyExists(string nativeDirectory)
    {
        string pluginPath = GetCurrentEditorPluginPath(nativeDirectory);
        return !string.IsNullOrEmpty(pluginPath) && File.Exists(pluginPath);
    }

    static void LogCurrentEditorPluginRestartWarning(string nativeDirectory, bool currentEditorPluginAlreadyExisted)
    {
        if (!currentEditorPluginAlreadyExisted)
        {
            return;
        }

        string pluginPath = GetCurrentEditorPluginPath(nativeDirectory);
        if (string.IsNullOrEmpty(pluginPath) || !File.Exists(pluginPath))
        {
            return;
        }

        Debug.LogWarning("OpenSoundLab: IMPORTANT - OSLNative was rebuilt for the platform currently running this Unity Editor. "
            + "A native plugin already existed before the rebuild, so Unity may still have the previous binary loaded. "
            + "Restart Unity before testing or profiling OSLNative; otherwise the rebuilt native plugin will not be used.");
    }

    static string GetCurrentEditorPluginPath(string nativeDirectory)
    {
        DirectoryInfo projectDirectory = Directory.GetParent(nativeDirectory);
        if (projectDirectory == null)
        {
            return null;
        }

        if (Application.platform == RuntimePlatform.OSXEditor)
        {
            return Path.Combine(projectDirectory.FullName, "Assets", "Plugins", "OSLNative", "macos", "Release",
                "libOSLNative.dylib");
        }

        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            return Path.Combine(projectDirectory.FullName, "Assets", "Plugins", "OSLNative", "x64", "Release", "OSLNative.dll");
        }

        return null;
    }

    static void RunProcess(string fileName, string arguments, string workingDirectory)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using (Process process = new Process())
        {
            process.StartInfo = startInfo;
            process.OutputDataReceived += LogProcessOutput;
            process.ErrorDataReceived += LogProcessOutput;

            Debug.Log("OpenSoundLab: Running " + fileName + " " + arguments);
            if (!process.Start())
            {
                throw new BuildFailedException("Could not start " + fileName);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new BuildFailedException("OSLNative rebuild failed with exit code " + process.ExitCode + ".");
            }
        }
    }

    static void LogProcessOutput(object sender, DataReceivedEventArgs args)
    {
        if (!string.IsNullOrEmpty(args.Data))
        {
            Debug.Log("[OSLNative] " + args.Data);
        }
    }

    static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }
}

class Editor_OSLNativeBuildPreprocess : IPreprocessBuild
{
    public int callbackOrder
    {
        get { return -1000; }
    }

    public void OnPreprocessBuild(BuildTarget target, string path)
    {
        if (Editor_OSLNativeBuild.ShouldRebuildOnBuild)
        {
            Editor_OSLNativeBuild.RebuildAllPossiblePlatforms();
        }
    }
}

#endif
