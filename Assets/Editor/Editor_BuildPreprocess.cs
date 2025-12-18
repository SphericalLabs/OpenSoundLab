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

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;


class CustomBuildPreProcess : IPreprocessBuild
{
    public int callbackOrder { get { return 0; } }
    public void OnPreprocessBuild(BuildTarget target, string path)
    {
        prepareStreamingAssetBundles();
    }

    [MenuItem("OpenSoundLab/Factory Presets/Prepare Bundle", false, 1000)]
    public static void PrepareStreamingAssetBundlesMenu()
    {
        prepareStreamingAssetBundles();
        Debug.Log("OpenSoundLab: Prepared Factory Preset bundles.");
    }

    private static void prepareStreamingAssetBundles()
    {
        Directory.CreateDirectory(Application.streamingAssetsPath);

        string dataDirectory = Application.dataPath + "/StreamingAssetsPreZip/Examples/";
        string fileToCreate = Application.streamingAssetsPath + "/Examples.tgz";

        Utility_SharpZipCommands.CreateTarGZ_FromDirectory(fileToCreate, dataDirectory);

        dataDirectory = Application.dataPath + "/StreamingAssetsPreZip/Samples/";
        fileToCreate = Application.streamingAssetsPath + "/Samples.tgz";
        Utility_SharpZipCommands.CreateTarGZ_FromDirectory(fileToCreate, dataDirectory);
    }
}

#endif
