using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class Editor_MaterialLister : EditorWindow
{
    private List<Material> materialsInScene = new List<Material>();
    private Vector2 scrollPosition;
    private Vector2 materialsScrollPosition;
    private Dictionary<string, List<Material>> shaderToMaterials = new Dictionary<string, List<Material>>();
    private string selectedShader = string.Empty;

    // Shader mappings: Original Shader -> Meta Occlusion Shader
    private Dictionary<string, string> shaderMappings = new Dictionary<string, string>
    {
        { "Universal Render Pipeline/Lit", "Meta/Depth/URP/Occlusion Lit" },
        { "Universal Render Pipeline/Simple Lit", "Meta/Depth/URP/Occlusion Simple Lit" },
        { "Universal Render Pipeline/Baked Lit", "Meta/Depth/URP/Occlusion Baked Lit" },
        { "Universal Render Pipeline/Unlit", "Meta/Depth/URP/Occlusion Unlit" }
    };

    // HashSet for quick lookup of Meta Occlusion shaders
    private HashSet<string> metaOcclusionShaders;

    [MenuItem("Tools/Material Lister")]
    public static void ShowWindow()
    {
        GetWindow<Editor_MaterialLister>("Material Lister");
    }

    private void OnEnable()
    {
        // Initialize the HashSet with Meta Occlusion shader names
        metaOcclusionShaders = new HashSet<string>(shaderMappings.Values);
    }

    private void OnGUI()
    {
        GUILayout.Label("Material Lister", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // Find Materials Button
        if (GUILayout.Button("Find Materials in Scene"))
        {
            FindMaterialsInScene();
            // Clear any previously selected shader when re-finding materials
            selectedShader = string.Empty;
        }

        GUILayout.Space(10);

        // Display Found Materials
        if (materialsInScene.Count > 0)
        {
            GUILayout.Label($"Materials Without Meta Occlusion Shader ({materialsInScene.Count}):", EditorStyles.boldLabel);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            foreach (Material mat in materialsInScene)
            {
                EditorGUILayout.ObjectField(mat.name, mat, typeof(Material), false);
            }
            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Replace Shaders Button
            if (GUILayout.Button("Replace Shaders with Meta Quest Depth Shaders"))
            {
                ReplaceShaders();
            }

            GUILayout.Space(10);

            // List Unique Shaders Button
            if (GUILayout.Button("List Unique Shaders"))
            {
                ListUniqueShaders();
            }

            // Display Unique Shaders as Clickable Buttons if available
            if (shaderToMaterials != null && shaderToMaterials.Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label($"Unique Shaders ({shaderToMaterials.Count}):", EditorStyles.boldLabel);

                // Begin a scroll view for shader buttons
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
                foreach (var shader in shaderToMaterials.Keys.OrderBy(s => s))
                {
                    // Create a button for each unique shader
                    if (GUILayout.Button(shader, GUILayout.ExpandWidth(true)))
                    {
                        // Set the selected shader to display its materials
                        selectedShader = shader;
                    }
                }
                GUILayout.EndScrollView();

                GUILayout.Space(10);

                // Display Materials using the selected shader
                if (!string.IsNullOrEmpty(selectedShader) && shaderToMaterials.ContainsKey(selectedShader))
                {
                    GUILayout.Label($"Materials using '{selectedShader}' ({shaderToMaterials[selectedShader].Count}):", EditorStyles.boldLabel);

                    materialsScrollPosition = GUILayout.BeginScrollView(materialsScrollPosition, GUILayout.Height(200));
                    foreach (Material mat in shaderToMaterials[selectedShader])
                    {
                        EditorGUILayout.ObjectField(mat.name, mat, typeof(Material), false);
                    }
                    GUILayout.EndScrollView();

                    GUILayout.Space(5);

                    // Optional: Add a button to focus on the shader in the Project window
                    if (GUILayout.Button("Select Shader in Project"))
                    {
                        Shader shaderObj = Shader.Find(selectedShader);
                        if (shaderObj != null)
                        {
                            Selection.activeObject = shaderObj;
                            EditorGUIUtility.PingObject(shaderObj);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Shader Not Found", $"Shader '{selectedShader}' could not be found.", "OK");
                        }
                    }
                }

                GUILayout.Space(10);

                // Optional: Add a button to copy the shaders to clipboard
                if (GUILayout.Button("Copy Shaders to Clipboard"))
                {
                    string shadersList = string.Join("\n", shaderToMaterials.Keys.OrderBy(s => s));
                    TextEditor te = new TextEditor
                    {
                        text = shadersList
                    };
                    te.SelectAll();
                    te.Copy();
                    EditorUtility.DisplayDialog("Copied", "Unique shaders have been copied to the clipboard.", "OK");
                }
            }
            else
            {
                GUILayout.Space(10);
                GUILayout.Label("No unique shaders found. Click 'List Unique Shaders' to generate the list.", EditorStyles.wordWrappedLabel);
            }
        }
        else
        {
            GUILayout.Space(10);
            GUILayout.Label("No materials found that require shader replacement. Click 'Find Materials in Scene' to start.", EditorStyles.wordWrappedLabel);
        }
    }

    /// <summary>
    /// Finds all unique base materials in the current scene that do not use Meta Occlusion shaders.
    /// </summary>
    private void FindMaterialsInScene()
    {
        materialsInScene.Clear();
        shaderToMaterials.Clear(); // Clear previous shader mappings
        selectedShader = string.Empty; // Clear any selected shader

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
                        // Exclude materials that already use a Meta Occlusion shader
                        if (!metaOcclusionShaders.Contains(mat.shader.name))
                        {
                            materialSet.Add(mat);
                        }
                    }
                }
            }
        }

        materialsInScene.AddRange(materialSet);
        Debug.Log($"Found {materialsInScene.Count} unique base materials without Meta Occlusion shaders in the scene.");
    }

    /// <summary>
    /// Replaces the shaders of the filtered materials with their corresponding Meta Occlusion shaders.
    /// </summary>
    private void ReplaceShaders()
    {
        if (materialsInScene.Count == 0)
        {
            EditorUtility.DisplayDialog("No Materials to Replace", "There are no materials to replace shaders for.", "OK");
            return;
        }

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
            else
            {
                Debug.LogWarning($"No shader mapping found for shader '{currentShaderName}' on material '{mat.name}'.");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Shader Replacement Complete",
            $"Shaders have been replaced for {materialsChanged} materials.",
            "OK");

        // Optionally, refresh the material list after replacement
        FindMaterialsInScene();
    }

    /// <summary>
    /// Gathers and lists all unique shaders used by the currently found materials.
    /// </summary>
    private void ListUniqueShaders()
    {
        shaderToMaterials.Clear();

        foreach (Material mat in materialsInScene)
        {
            if (mat != null && mat.shader != null)
            {
                string shaderName = mat.shader.name;
                if (!shaderToMaterials.ContainsKey(shaderName))
                {
                    shaderToMaterials[shaderName] = new List<Material>();
                }
                shaderToMaterials[shaderName].Add(mat);
            }
        }

        Debug.Log($"Unique shaders found: {shaderToMaterials.Count}");
    }
}
