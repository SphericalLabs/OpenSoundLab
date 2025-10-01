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

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.Networking;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.GZip;

public class sampleManager : MonoBehaviour
{
    public static sampleManager instance;

    public Dictionary<string, Dictionary<string, string>> sampleDictionary;

    public event Action SamplesLoaded;
    public bool IsReady { get; private set; }

    List<string> customSamples = new List<string>();
    bool _initializationStarted;

    void EnsureCategory(string key)
    {
        if (sampleDictionary == null)
        {
            return;
        }

        if (!sampleDictionary.ContainsKey(key))
        {
            sampleDictionary[key] = new Dictionary<string, string>();
        }
    }

    void EnsureDefaultCategories()
    {
        EnsureCategory("Custom");
        EnsureCategory("Recordings");
        EnsureCategory("Sessions");
    }

    public void ClearCustomSamples()
    {
        if (!sampleDictionary.ContainsKey("Custom"))
        {
            return;
        }

        for (int i = 0; i < customSamples.Count; i++)
        {
            samplerLoad[] samplers = FindObjectsOfType<samplerLoad>();
            for (int i2 = 0; i2 < samplers.Length; i2++)
            {
                if (samplers[i2].CurTapeLabel == customSamples[i])
                {
                    samplers[i2].ForceEject();
                }
            }

            sampleDictionary["Custom"].Remove(customSamples[i]);
        }

        PlayerPrefs.DeleteAll();
        customSamples.Clear();

        libraryDeviceInterface[] libs = FindObjectsOfType<libraryDeviceInterface>();
        for (int i2 = 0; i2 < libs.Length; i2++)
        {
            if (libs[i2].curPrimary == "Custom") libs[i2].updateSecondaryPanels("Custom");

        }
    }

    public string parseFilename(string f)
    {

        if (f == "") return "";

        if (f.Substring(0, 3) == "APP")
        {
            f = f.Remove(0, 3);
            f = f.Insert(0, Directory.GetParent(Application.persistentDataPath).FullName + Path.DirectorySeparatorChar + "samples");
        }
        else if (f.Substring(0, 3) == "DOC")
        {
            f = f.Remove(0, 3);
            f = f.Insert(0, masterControl.instance.SaveDir + Path.DirectorySeparatorChar + "Samples");
        }
        return f;
    }



    public void AddSample(string newsample)
    {
        EnsureCategory("Custom");

        if (sampleDictionary["Custom"].ContainsKey(Path.GetFileNameWithoutExtension(newsample))) return;

        if (!File.Exists(newsample))
        {
            return;
        }

        customSamples.Add(Path.GetFileNameWithoutExtension(newsample));

        sampleDictionary["Custom"][Path.GetFileNameWithoutExtension(newsample)] = newsample;

        libraryDeviceInterface[] libs = FindObjectsOfType<libraryDeviceInterface>();
        for (int i2 = 0; i2 < libs.Length; i2++)
        {
            if (libs[i2].curPrimary == "Custom") libs[i2].updateSecondaryPanels("Custom");
        }

        if (!inStartup)
        {
            customSampleCount++;
            PlayerPrefs.SetInt("sampCount", customSampleCount);
            PlayerPrefs.SetString("samp" + customSampleCount, newsample);
        }
    }

    public void AddRecording(string newsample)
    {
        EnsureCategory("Recordings");

        if (sampleDictionary["Recordings"].ContainsKey(Path.GetFileNameWithoutExtension(newsample)))
        {
            return;
        }

        sampleDictionary["Recordings"][Path.GetFileNameWithoutExtension(newsample)] = newsample;

        libraryDeviceInterface[] libs = FindObjectsOfType<libraryDeviceInterface>();
        for (int i2 = 0; i2 < libs.Length; i2++)
        {
            if (libs[i2].curPrimary == "Recordings") libs[i2].updateSecondaryPanels("Recordings");
        }
    }

    public void AddSession(string newsample)
    {
        EnsureCategory("Sessions");

        if (sampleDictionary["Sessions"].ContainsKey(Path.GetFileNameWithoutExtension(newsample)))
        {
            return;
        }

        sampleDictionary["Sessions"][Path.GetFileNameWithoutExtension(newsample)] = newsample;

        libraryDeviceInterface[] libs = FindObjectsOfType<libraryDeviceInterface>();
        for (int i2 = 0; i2 < libs.Length; i2++)
        {
            if (libs[i2].curPrimary == "Sessions") libs[i2].updateSecondaryPanels("Sessions");
        }
    }

    int customSampleCount = 0;
    bool inStartup = false;
    void AddCustomSamples()
    {
        inStartup = true;

        EnsureCategory("Custom");

        if (!PlayerPrefs.HasKey("sampCount")) PlayerPrefs.SetInt("sampCount", 0);

        customSampleCount = PlayerPrefs.GetInt("sampCount");
        for (int i = 0; i < customSampleCount; i++)
        {

            AddSample(PlayerPrefs.GetString("samp" + (i + 1)));
        }
        inStartup = false;
    }

    void loadSampleDictionary(string dir, string pathtype)
    {
        if (Directory.Exists(dir))
        {
            string[] subdirs = Directory.GetDirectories(dir);
            for (int i = 0; i < subdirs.Length; i++)
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                string s = subdirs[i].Replace(dir + "\\", "");
#elif UNITY_ANDROID || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                string s = subdirs[i].Replace(dir + "/", "");
#endif
                sampleDictionary[s] = new Dictionary<string, string>();

                for (int i2 = 0; i2 < fileEndings.Length; i2++)
                {
                    string[] subdirFiles = Directory.GetFiles(subdirs[i], fileEndings[i2]);
                    foreach (string d in subdirFiles)
                    {
                        sampleDictionary[s][Path.GetFileNameWithoutExtension(d)] = pathtype + Path.DirectorySeparatorChar + s + Path.DirectorySeparatorChar + Path.GetFileName(d);
                    }
                }
            }

        }
        else
        {
            Debug.Log("NO SAMPLES FOLDER FOUND");
        }
    }

    string[] fileEndings = new string[] { "*.wav"/*, "*.ogg", "*.mp3" */}; // disabled ogg and mp3, since NVorbis and NAudio are not supporting Android / ARM64 and NLayer somehow is also not working as a fallback and it actually would only have supported 44.1khz mp3 files.

    public void Init()
    {
        if (_initializationStarted)
        {
            return;
        }

        _initializationStarted = true;
        instance = this;
        sampleDictionary = new Dictionary<string, Dictionary<string, string>>();
        EnsureDefaultCategories();

        StartCoroutine(InitRoutine());
    }

    IEnumerator InitRoutine()
    {
        string dir = masterControl.instance.SaveDir + Path.DirectorySeparatorChar + "Samples";
        bool directoryExists = Directory.Exists(dir);
        bool needsExtraction = !directoryExists;

        if (!needsExtraction && Directory.GetFileSystemEntries(dir).Length == 0)
        {
            needsExtraction = true;
        }

        Directory.CreateDirectory(dir);

        SampleInstallOverlay overlay = null;

        if (needsExtraction)
        {
            overlay = SampleInstallOverlay.CreateAndAttach();
            if (overlay != null)
            {
                overlay.SetProgress(0f);
            }

            string compressedPath = Path.Combine(Directory.GetParent(Application.persistentDataPath).FullName, "Samples.tgz");
            string sourceUri = BuildStreamingAssetUri("Samples.tgz");

            bool copySucceeded = false;
            string copyError = null;

            string fileSystemSource = TryGetFileSystemPath(sourceUri);
            if (!string.IsNullOrEmpty(fileSystemSource) && File.Exists(fileSystemSource))
            {
                const int BufferSize = 128 * 1024;
                long totalBytes = 0;
                long copiedBytes = 0;
                Exception copyException = null;

                try
                {
                    totalBytes = new FileInfo(fileSystemSource).Length;
                }
                catch (Exception ex)
                {
                    copyError = ex.Message;
                }

                if (copyError == null)
                {
                    Task copyTask = Task.Run(() =>
                    {
                        try
                        {
                            byte[] buffer = new byte[BufferSize];
                            using (FileStream sourceStream = File.OpenRead(fileSystemSource))
                            using (FileStream destinationStream = File.Create(compressedPath))
                            {
                                int read;
                                while ((read = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    destinationStream.Write(buffer, 0, read);
                                    Interlocked.Add(ref copiedBytes, read);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            copyException = ex;
                        }
                    });

                    while (!copyTask.IsCompleted)
                    {
                        if (overlay != null && totalBytes > 0)
                        {
                            long progressBytes = Interlocked.Read(ref copiedBytes);
                            float progress = Mathf.Clamp01((float)progressBytes / totalBytes);
                            overlay.SetProgress(0.5f * progress);
                        }

                        yield return null;
                    }

                    if (overlay != null)
                    {
                        if (totalBytes > 0)
                        {
                            long progressBytes = Interlocked.Read(ref copiedBytes);
                            float progress = Mathf.Clamp01((float)progressBytes / totalBytes);
                            overlay.SetProgress(0.5f * progress);
                        }
                        else
                        {
                            overlay.SetProgress(0.5f);
                        }
                    }

                    if (copyException == null)
                    {
                        copySucceeded = true;
                        if (overlay != null)
                        {
                            overlay.SetProgress(0.5f);
                        }
                    }
                    else
                    {
                        copyError = copyException.Message;
                    }
                }
            }
            else
            {
                using (UnityWebRequest request = UnityWebRequest.Get(sourceUri))
                {
                    var downloadHandler = new DownloadHandlerFile(compressedPath)
                    {
                        removeFileOnAbort = true
                    };
                    request.downloadHandler = downloadHandler;

                    UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                    long contentLength = -1;

                    while (!operation.isDone)
                    {
                        if (contentLength <= 0)
                        {
                            string header = request.GetResponseHeader("Content-Length");
                            if (!string.IsNullOrEmpty(header) && long.TryParse(header, out long parsedLength))
                            {
                                contentLength = parsedLength;
                            }
                        }

                        if (overlay != null)
                        {
                            float progress;
                            if (contentLength > 0)
                            {
                                progress = (float)(request.downloadedBytes / (double)contentLength);
                            }
                            else
                            {
                                progress = request.downloadProgress;
                            }

                            if (float.IsNaN(progress) || progress < 0f)
                            {
                                progress = 0f;
                            }

                            overlay.SetProgress(0.5f * Mathf.Clamp01(progress));
                        }

                        yield return null;
                    }

#if UNITY_2020_1_OR_NEWER
                    if (request.result == UnityWebRequest.Result.Success)
#else
                    if (!request.isHttpError && !request.isNetworkError)
#endif
                    {
                        copySucceeded = true;
                        if (overlay != null)
                        {
                            overlay.SetProgress(0.5f);
                        }
                    }
                    else
                    {
                        copyError = request.error;
                    }
                }
            }

            if (!copySucceeded)
            {
                if (!string.IsNullOrEmpty(copyError))
                {
                    Debug.LogError($"sampleManager: failed to copy Samples.tgz from streaming assets. {copyError}");
                }
                else
                {
                    Debug.LogError("sampleManager: failed to copy Samples.tgz from streaming assets.");
                }
            }
            else
            {
                Exception extractionError = null;
                float extractionProgress = 0f;

                Task extractTask = Task.Run(() =>
                {
                    try
                    {
                        Utility_SharpZipCommands.ExtractTGZ(compressedPath, dir, (processed, total) =>
                        {
                            float normalized = total > 0 ? (float)(processed / (double)total) : 1f;
                            Volatile.Write(ref extractionProgress, Mathf.Clamp01(normalized));
                        });
                        Volatile.Write(ref extractionProgress, 1f);
                    }
                    catch (Exception ex)
                    {
                        extractionError = ex;
                    }
                });

                while (!extractTask.IsCompleted)
                {
                    if (overlay != null)
                    {
                        float current = Volatile.Read(ref extractionProgress);
                        overlay.SetProgress(0.5f + 0.5f * current);
                    }

                    yield return null;
                }

                if (overlay != null)
                {
                    float final = Volatile.Read(ref extractionProgress);
                    overlay.SetProgress(0.5f + 0.5f * final);
                }

                if (extractionError != null)
                {
                    Debug.LogError($"sampleManager: error extracting Samples.tgz: {extractionError.Message}");
                }
            }

            try
            {
                if (File.Exists(compressedPath))
                {
                    File.Delete(compressedPath);
                }
            }
            catch (IOException ex)
            {
                Debug.LogWarning($"sampleManager: could not delete temporary archive. {ex.Message}");
            }
        }

        Directory.CreateDirectory(dir + Path.DirectorySeparatorChar + "Recordings");
        Directory.CreateDirectory(dir + Path.DirectorySeparatorChar + "Sessions");

        loadSampleDictionary(dir, "DOC");
        EnsureDefaultCategories();
        AddCustomSamples();

        IsReady = true;
        SamplesLoaded?.Invoke();

        if (overlay != null)
        {
            overlay.CompleteAndHide();
        }
    }

    static string BuildStreamingAssetUri(string fileName)
    {
        string combinedPath = Path.Combine(Application.streamingAssetsPath, fileName);
        if (string.IsNullOrEmpty(combinedPath))
        {
            return string.Empty;
        }

        if (combinedPath.Contains("://"))
        {
            return combinedPath;
        }

        try
        {
            return new Uri(combinedPath).AbsoluteUri;
        }
        catch (UriFormatException)
        {
            string normalized = combinedPath.Replace('\\', '/');
            if (!normalized.StartsWith("/", StringComparison.Ordinal))
            {
                normalized = "/" + normalized;
            }
            return "file://" + normalized;
        }
    }

    static string TryGetFileSystemPath(string uri)
    {
        if (string.IsNullOrEmpty(uri))
        {
            return string.Empty;
        }

        if (!uri.Contains("://"))
        {
            return uri;
        }

        if (uri.StartsWith("jar:", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        try
        {
            Uri parsed = new Uri(uri);
            if (parsed.IsFile)
            {
                return parsed.LocalPath;
            }
        }
        catch (UriFormatException)
        {
        }

        return string.Empty;
    }

    public static string GetFileName(string path)
    {
        string fileName = Path.GetFileNameWithoutExtension(CorrectPathSeparators(path));

        //Debug.Log($"Filename {fileName}");
        return fileName;
    }


    public static string CorrectPathSeparators(string path)
    {
        if (Path.DirectorySeparatorChar == '\\')
        {
            // Windows-Umgebung: Korrigiere alle / zu \
            return path.Replace('/', '\\');
        }
        else
        {
            // Unix-Umgebung (Linux, macOS): Korrigiere alle \ zu /
            return path.Replace('\\', '/');
        }
    }
}
