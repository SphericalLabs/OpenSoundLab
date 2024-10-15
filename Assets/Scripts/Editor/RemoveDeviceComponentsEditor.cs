using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RemoveDeviceComponents))]
public class RemoveDeviceComponentsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var myScript = target as RemoveDeviceComponents;
        if (GUILayout.Button("Remove Components"))
        {
            myScript.RemoveComponents();
        }
    }
}
