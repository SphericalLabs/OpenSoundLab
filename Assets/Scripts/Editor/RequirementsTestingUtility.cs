using UnityEditor;
using UnityEngine;

// Utility helpers for quickly resetting player prefs tied to the requirements flow while testing in-editor.
public static class RequirementsTestingUtility
{
    const string ConsentKey = "requirements_consent_v1";

    [MenuItem("Tools/OpenSoundLab/PlayerPrefs/Clear Requirements Consent", priority = 100)]
    static void ClearRequirementsPrefs()
    {
        PlayerPrefs.DeleteKey(ConsentKey);
        PlayerPrefs.Save();
        Debug.Log("RequirementsTestingUtility: cleared requirements consent PlayerPrefs key.");
    }

    [MenuItem("Tools/OpenSoundLab/PlayerPrefs/Clear All PlayerPrefs", priority = 101)]
    static void ClearAllPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("RequirementsTestingUtility: cleared all PlayerPrefs entries.");
    }
}
