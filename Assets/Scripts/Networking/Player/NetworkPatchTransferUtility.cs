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

using System.IO;
using System.IO.Compression;
using System.Text;
using Mirror;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

public static class NetworkPatchTransferUtility
{
    const int commandEnvelopeBytes = 128;
    const string incomingDirectoryName = "Incoming";
    const string defaultPatchName = "Patch";

    public static bool TryPreparePatchUpload(string patchPath, out string patchFileName, out byte[] compressedPatch, out string error)
    {
        patchFileName = Path.GetFileName(patchPath);
        compressedPatch = null;
        error = null;

        if (string.IsNullOrEmpty(patchPath))
        {
            error = "Cannot upload patch: no file path was provided.";
            return false;
        }

        if (!File.Exists(patchPath))
        {
            error = $"Cannot upload patch: file not found at {patchPath}";
            return false;
        }

        if (string.IsNullOrEmpty(patchFileName))
        {
            patchFileName = defaultPatchName + ".xml";
        }

        try
        {
            byte[] patchBytes = File.ReadAllBytes(patchPath);
            compressedPatch = compressBytes(patchBytes);
        }
        catch (System.Exception ex)
        {
            error = $"Cannot upload patch {patchPath}: {ex.Message}";
            return false;
        }

        int maxCompressedBytes = GetMaxCompressedPatchBytes(patchFileName);
        if (maxCompressedBytes <= 0)
        {
            compressedPatch = null;
            error = "Cannot upload patch: Mirror transport is not ready for reliable patch transfer.";
            return false;
        }

        if (compressedPatch.Length > maxCompressedBytes)
        {
            int compressedBytes = compressedPatch.Length;
            compressedPatch = null;
            error = $"Cannot upload patch {patchFileName}: compressed payload is {compressedBytes} bytes, max reliable upload size is {maxCompressedBytes} bytes.";
            return false;
        }

        return true;
    }

    public static bool TryWriteUploadedPatch(byte[] compressedPatch, string patchFileName, uint uploaderNetId, out string patchPath, out string error)
    {
        patchPath = null;
        error = null;

        if (compressedPatch == null || compressedPatch.Length == 0)
        {
            error = "Cannot load uploaded patch: compressed patch payload is empty.";
            return false;
        }

        string incomingDirectory = getIncomingDirectory();
        string safeFileName = getIncomingPatchFileName(patchFileName, uploaderNetId);
        string targetPath = Path.Combine(incomingDirectory, safeFileName);
        string tempPath = targetPath + ".tmp";

        try
        {
            Directory.CreateDirectory(incomingDirectory);
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            using (MemoryStream inputStream = new MemoryStream(compressedPatch))
            using (GZipStream gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            using (FileStream outputStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                gzipStream.CopyTo(outputStream);
            }

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            File.Move(tempPath, targetPath);
            patchPath = targetPath;
            return true;
        }
        catch (System.Exception ex)
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            error = $"Cannot write uploaded patch {patchFileName}: {ex.Message}";
            return false;
        }
    }

    public static int GetMaxCompressedPatchBytes(string patchFileName)
    {
        if (Transport.active == null)
        {
            return 0;
        }

        int maxContentBytes = NetworkMessages.MaxContentSize(Channels.Reliable);
        if (maxContentBytes <= 0)
        {
            return 0;
        }

        int fileNameBytes = Encoding.UTF8.GetByteCount(string.IsNullOrEmpty(patchFileName) ? defaultPatchName : patchFileName);
        int maxBytes = maxContentBytes - fileNameBytes - commandEnvelopeBytes;
        return Mathf.Max(0, maxBytes);
    }

    static byte[] compressBytes(byte[] patchBytes)
    {
        using (MemoryStream outputStream = new MemoryStream())
        {
            using (GZipStream gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal, true))
            {
                gzipStream.Write(patchBytes, 0, patchBytes.Length);
            }

            return outputStream.ToArray();
        }
    }

    static string getIncomingDirectory()
    {
        string baseDir = masterControl.instance != null ? masterControl.instance.SaveDir : null;
        if (string.IsNullOrEmpty(baseDir))
        {
            baseDir = Application.persistentDataPath;
        }

        return Path.Combine(baseDir, "Saves", incomingDirectoryName);
    }

    static string getIncomingPatchFileName(string patchFileName, uint uploaderNetId)
    {
        string baseName = sanitizeFileName(Path.GetFileNameWithoutExtension(patchFileName));
        if (string.IsNullOrEmpty(baseName))
        {
            baseName = defaultPatchName;
        }

        return "ClientPatch_" + uploaderNetId + "_" + baseName + ".xml";
    }

    static string sanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return defaultPatchName;
        }

        char[] sanitized = fileName.ToCharArray();
        char[] invalidChars = Path.GetInvalidFileNameChars();

        for (int i = 0; i < sanitized.Length; i++)
        {
            for (int j = 0; j < invalidChars.Length; j++)
            {
                if (sanitized[i] == invalidChars[j])
                {
                    sanitized[i] = '_';
                    break;
                }
            }
        }

        return new string(sanitized);
    }
}
