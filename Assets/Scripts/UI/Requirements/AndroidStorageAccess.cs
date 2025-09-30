using UnityEngine;

/// <summary>
/// Minimal helper for handling the Android "Manage All Files" permission.
/// Defaults treat permissions as granted on non-Android targets or API levels below 30.
/// </summary>
public static class AndroidStorageAccess
{
    const int ManageAllFilesMinSdk = 30;

    /// <summary>
    /// Returns true when the Manage All Files permission is granted or not required.
    /// </summary>
    public static bool HasManageAllFilesAccess()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            int sdkInt = GetSdkInt();
            if (sdkInt < ManageAllFilesMinSdk)
            {
                return true;
            }

            using (var environment = new AndroidJavaClass("android.os.Environment"))
            {
                return environment.CallStatic<bool>("isExternalStorageManager");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"AndroidStorageAccess.HasManageAllFilesAccess failed: {ex.Message}");
            return false;
        }
#else
        return true;
#endif
    }

    /// <summary>
    /// Attempts to open the appropriate settings screen for Manage All Files access.
    /// Returns true if the intent was started successfully.
    /// </summary>
    public static bool TryOpenManageAllFilesSettings()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            int sdkInt = GetSdkInt();
            if (sdkInt < ManageAllFilesMinSdk)
            {
                return false;
            }

            AndroidJavaObject activity = null;
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }
            if (activity == null)
            {
                return false;
            }

            using (var settingsClass = new AndroidJavaClass("android.provider.Settings"))
            {
                string packageName = activity.Call<string>("getPackageName");
                string appAction = settingsClass.GetStatic<string>("ACTION_MANAGE_APP_ALL_FILES_ACCESS_PERMISSION");
                string allFilesAction = settingsClass.GetStatic<string>("ACTION_MANAGE_ALL_FILES_ACCESS_PERMISSION");
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    try
                    {
                        using (var intent = new AndroidJavaObject("android.content.Intent", appAction))
                        {
                            using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                            {
                                using (var uri = uriClass.CallStatic<AndroidJavaObject>("parse", "package:" + packageName))
                                {
                                    intent.Call<AndroidJavaObject>("setData", uri);
                                }
                            }
                            activity.Call("startActivity", intent);
                        }
                    }
                    catch (AndroidJavaException)
                    {
                        using (var fallbackIntent = new AndroidJavaObject("android.content.Intent", allFilesAction))
                        {
                            activity.Call("startActivity", fallbackIntent);
                        }
                    }
                }));
                return true;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"AndroidStorageAccess.TryOpenManageAllFilesSettings failed: {ex.Message}");
            return false;
        }
#else
        return false;
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    static int GetSdkInt()
    {
        using (var versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            return versionClass.GetStatic<int>("SDK_INT");
        }
    }
#endif
}
