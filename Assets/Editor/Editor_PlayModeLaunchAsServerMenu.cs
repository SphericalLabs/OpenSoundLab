using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Editor_PlayModeLaunchAsServerMenu
{
    const string launchAsServerMenuPath = "OpenSoundLab/Play Mode/Launch as Server";
    const string launchAsServerPrefKey = "OpenSoundLab.PlayMode.LaunchAsServer";
    const int launchAsServerDefaultValue = 1;

    static Editor_PlayModeLaunchAsServerMenu()
    {
        Menu.SetChecked(launchAsServerMenuPath, isLaunchAsServerEnabled());
    }

    [MenuItem(launchAsServerMenuPath, false, 5)]
    public static void ToggleLaunchAsServer()
    {
        bool enabled = !isLaunchAsServerEnabled();
        PlayerPrefs.SetInt(launchAsServerPrefKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        Menu.SetChecked(launchAsServerMenuPath, enabled);
    }

    [MenuItem(launchAsServerMenuPath, true, 5)]
    public static bool ToggleLaunchAsServerValidate()
    {
        Menu.SetChecked(launchAsServerMenuPath, isLaunchAsServerEnabled());
        return true;
    }

    static bool isLaunchAsServerEnabled()
    {
        return PlayerPrefs.GetInt(launchAsServerPrefKey, launchAsServerDefaultValue) == 1;
    }
}
