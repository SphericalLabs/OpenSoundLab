using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Serializable descriptor for a requirement document stored under StreamingAssets.
/// </summary>
[System.Serializable]
public class RequirementDocument
{
    public string title = "Document";

    [Tooltip("Relative path under StreamingAssets to the document text file, e.g. Requirements/eula.txt")]
    public string streamingAssetRelativePath;

    string cachedBody;
    bool isLoaded;

    public IEnumerator loadBody(string fallback)
    {
        if (isLoaded)
        {
            yield break;
        }

        string resolved = string.Empty;

        if (!string.IsNullOrEmpty(streamingAssetRelativePath))
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, streamingAssetRelativePath);
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var request = UnityWebRequest.Get(fullPath))
            {
                yield return request.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
                bool success = request.result == UnityWebRequest.Result.Success;
#else
                bool success = !request.isNetworkError && !request.isHttpError;
#endif
                if (success)
                {
                    resolved = request.downloadHandler.text;
                }
                else
                {
                    Debug.LogWarning($"RequirementsManager: failed to load streaming asset '{fullPath}': {request.error}");
                }
            }
#else
            try
            {
                if (File.Exists(fullPath))
                {
                    resolved = File.ReadAllText(fullPath);
                }
                else
                {
                    Debug.LogWarning($"RequirementsManager: streaming asset missing at '{fullPath}'.");
                }
            }
            catch (IOException ex)
            {
                Debug.LogWarning($"RequirementsManager: error reading '{fullPath}': {ex.Message}");
            }
#endif
        }
        else
        {
            Debug.LogWarning($"RequirementsManager: no streaming asset path configured for '{title}'.");
        }

        if (string.IsNullOrEmpty(resolved))
        {
            resolved = fallback;
        }

        cachedBody = normalizeLineEndings(resolved);
        isLoaded = true;
    }

    public string resolveBody()
    {
        return cachedBody ?? string.Empty;
    }

    static string normalizeLineEndings(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        value = value.Replace("\r\n", "\n");
        value = value.Replace('\r', '\n');
        return value;
    }
}
