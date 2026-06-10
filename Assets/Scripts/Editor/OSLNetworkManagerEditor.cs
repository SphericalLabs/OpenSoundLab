using UnityEditor;
using UnityEngine;

public abstract class OSLNetworkManagerEditorBase : Editor
{
    const string spawnPrefabsProperty = "spawnPrefabs";
    const string readmePath = "Assets/OSLDevices/README.md";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", spawnPrefabsProperty);
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(4f);

        // OSL network spawn prefabs are inferred from enabled device manifests by
        // OSLNetworkSpawnPrefabRegistrar, not populated manually through Mirror's inspector.
        EditorGUILayout.HelpBox(
            "Spawnable prefabs are populated from OSL device manifests at runtime. " +
            "Do not use Mirror's manual Populate Spawnable Prefabs flow for OSL network managers. " +
            "See " + readmePath + ".",
            MessageType.Info);
    }
}

[CustomEditor(typeof(Network.LocalNetworkManager))]
[CanEditMultipleObjects]
public class OSLLocalNetworkManagerEditor : OSLNetworkManagerEditorBase
{
}

[CustomEditor(typeof(Network.OslRelayNetworkManager))]
[CanEditMultipleObjects]
public class OSLRelayNetworkManagerEditor : OSLNetworkManagerEditorBase
{
}
