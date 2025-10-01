using UnityEditor;
using UnityEngine;

// Utility helpers for quickly resetting player prefs tied to the requirements flow while testing in-editor.
public static class RequirementsTestingUtility
{
    const string ConsentKey = "requirements_consent_v1";

    [MenuItem("Tools/OpenSoundLab/Clear Requirements Consent", priority = 200)]
    static void ClearRequirementsPrefs()
    {
        PlayerPrefs.DeleteKey(ConsentKey);
        PlayerPrefs.Save();
        Debug.Log("RequirementsTestingUtility: cleared requirements consent PlayerPrefs key.");
    }

    [MenuItem("Tools/OpenSoundLab/Clear All Player Prefs", priority = 201)]
    static void ClearAllPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("RequirementsTestingUtility: cleared all PlayerPrefs entries.");
    }
}
