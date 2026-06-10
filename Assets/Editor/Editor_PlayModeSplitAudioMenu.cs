using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Editor_PlayModeSplitAudioMenu
{
    const string splitAudioMenuPath = "OpenSoundLab/Play Mode/LR-Split Audio for Server & Client";

    static Editor_PlayModeSplitAudioMenu()
    {
        Menu.SetChecked(splitAudioMenuPath, ClientServerPanorama.IsEditorSplitAudioEnabled());
    }

    [MenuItem(splitAudioMenuPath, false, 8)]
    public static void ToggleSplitAudio()
    {
        bool enabled = !ClientServerPanorama.IsEditorSplitAudioEnabled();
        PlayerPrefs.SetInt(ClientServerPanorama.editorSplitAudioPrefKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        Menu.SetChecked(splitAudioMenuPath, enabled);
    }

    [MenuItem(splitAudioMenuPath, true, 8)]
    public static bool ToggleSplitAudioValidate()
    {
        Menu.SetChecked(splitAudioMenuPath, ClientServerPanorama.IsEditorSplitAudioEnabled());
        return true;
    }
}
