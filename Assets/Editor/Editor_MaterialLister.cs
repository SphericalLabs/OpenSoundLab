using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class Editor_MaterialLister : EditorWindow
{
    private List<Material> materialsInScene = new List<Material>();
    private Vector2 scrollPosition;

    // Shader mappings
    private Dictionary<string, string> shaderMappings = new Dictionary<string, string>
    {
        { "Universal Render Pipeline/Lit", "Meta/Depth/URP/Occlusion Lit" },
        { "Universal Render Pipeline/Simple Lit", "Meta/Depth/URP/Occlusion Simple Lit" },
        { "Universal Render Pipeline/Baked Lit", "Meta/Depth/URP/Occlusion Baked Lit" },
        { "Universal Render Pipeline/Unlit", "Meta/Depth/URP/Occlusion Unlit Lit" }
    };

    [MenuItem("Tools/Material Lister")]
    public static void ShowWindow()
    {
        GetWindow<Editor_MaterialLister>("Material Lister");
    }

    private void OnGUI()
    {
        GUILayout.Label("Material Lister", EditorStyles.boldLabel);

        if (GUILayout.Button("Find Materials in Scene"))
        {
            FindMaterialsInScene();
        }

        if (materialsInScene.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label("Materials Found:", EditorStyles.boldLabel);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(400));
            foreach (Material mat in materialsInScene)
            {
                EditorGUILayout.ObjectField(mat.name, mat, typeof(Material), false);
            }
            GUILayout.EndScrollView();

            GUILayout.Space(10);
            if (GUILayout.Button("Replace Shaders with Meta Quest Depth Shaders"))
            {
                ReplaceShaders();
            }
        }
    }

    private void FindMaterialsInScene()
    {
        materialsInScene.Clear();
        Renderer[] renderers = FindObjectsOfType<Renderer>(true); // Include inactive objects
        HashSet<Material> materialSet = new HashSet<Material>();

        foreach (Renderer renderer in renderers)
        {
            // Using sharedMaterials to get the base materials
            foreach (Material mat in renderer.sharedMaterials)
            {
                if (mat != null)
                {
                    // Check if the material is an asset (base material)
                    string assetPath = AssetDatabase.GetAssetPath(mat);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        materialSet.Add(mat);
                    }
                }
            }
        }

        materialsInScene.AddRange(materialSet);
        Debug.Log($"Found {materialsInScene.Count} unique base materials in the scene.");
    }

    private void ReplaceShaders()
    {
        int materialsChanged = 0;

        foreach (Material mat in materialsInScene)
        {
            string currentShaderName = mat.shader.name;

            if (shaderMappings.ContainsKey(currentShaderName))
            {
                string newShaderName = shaderMappings[currentShaderName];
                Shader newShader = Shader.Find(newShaderName);

                if (newShader != null)
                {
                    Undo.RecordObject(mat, "Replace Shader");
                    mat.shader = newShader;
                    EditorUtility.SetDirty(mat);
                    materialsChanged++;
                }
                else
                {
                    Debug.LogWarning($"New shader '{newShaderName}' not found for material '{mat.name}'.");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Shader Replacement Complete",
            $"Shaders have been replaced for {materialsChanged} materials.",
            "OK");
    }
}
