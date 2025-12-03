using UnityEditor;
using UnityEngine;

// Editor utilities for managing PlayerPrefs shortcuts used during testing and setup.
#if UNITY_EDITOR
public static class Editor_PlayerPrefsEditorTools
{
    private const string TutorialMenuPath = "OpenSoundLab/PlayerPrefs/Show Tutorials On Startup";
    private const string TutorialPrefKey = "showTutorialsOnStartup";
    private const int TutorialDefaultValue = 1;
    private const string ConsentToggleMenuPath = "OpenSoundLab/PlayerPrefs/Show Requirements Wizard";
    const string ConsentKey = "requirements_consent_v1";
    private const int ConsentDefaultValue = 0;

    [MenuItem("OpenSoundLab/PlayerPrefs/Clear All PlayerPrefs", priority = 101)]
    static void ClearAllPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("PlayerPrefsEditorTools: cleared all PlayerPrefs entries.");
    }

    [MenuItem(TutorialMenuPath, priority = 103)]
    private static void ToggleShowTutorialsOnStartup()
    {
        bool nextState = !IsShowTutorialsOnStartupEnabled();
        PlayerPrefs.SetInt(TutorialPrefKey, nextState ? 1 : 0);
        PlayerPrefs.Save();
    }

    [MenuItem(TutorialMenuPath, true)]
    private static bool ShowTutorialsOnStartupValidate()
    {
        Menu.SetChecked(TutorialMenuPath, IsShowTutorialsOnStartupEnabled());
        return true;
    }

    [MenuItem(ConsentToggleMenuPath, priority = 102)]
    private static void ToggleRequirementsConsent()
    {
        bool enabled = IsRequirementsConsentGiven();
        PlayerPrefs.SetInt(ConsentKey, enabled ? 0 : 1);
        PlayerPrefs.Save();
    }

    [MenuItem(ConsentToggleMenuPath, true)]
    private static bool RequirementsConsentValidate()
    {
        Menu.SetChecked(ConsentToggleMenuPath, !IsRequirementsConsentGiven());
        return true;
    }

    private static bool IsShowTutorialsOnStartupEnabled()
    {
        return PlayerPrefs.GetInt(TutorialPrefKey, TutorialDefaultValue) == 1;
    }

    private static bool IsRequirementsConsentGiven()
    {
        return PlayerPrefs.GetInt(ConsentKey, ConsentDefaultValue) == 1;
    }

}
#endif
