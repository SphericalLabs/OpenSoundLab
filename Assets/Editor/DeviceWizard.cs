using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using Mirror;
using UnityEditor.Compilation;

[InitializeOnLoad]
public class DeviceWizard : EditorWindow
{
    static DeviceWizard()
    {
        // This runs after every compilation
        EditorApplication.delayCall += CheckForPendingDevice;
    }

    private static void CheckForPendingDevice()
    {
        if (EditorPrefs.GetBool("OSL_PendingDevice", false))
        {
            string name = EditorPrefs.GetString("OSL_DeviceName");
            string interfaceName = EditorPrefs.GetString("OSL_InterfaceName");
            string generatorName = EditorPrefs.GetString("OSL_GeneratorName");

            FinalizePrefabs(name, interfaceName, generatorName);

            EditorPrefs.SetBool("OSL_PendingDevice", false);
            Debug.Log($"<color=green>OSL Device Wizard: Successfully finalized {name} prefabs after compilation.</color>");
        }
    }
    private string deviceName = "";
    private DeviceCategory category = DeviceCategory.WaveGenerator;
    private bool includeSignalGenerator = true;

    [MenuItem("OpenSoundLab/Devices/Create")]
    public static void ShowWindow()
    {
        GetWindow<DeviceWizard>("OSL Device Wizard");
    }

    private void OnGUI()
    {
        GUILayout.Label("New Device Settings", EditorStyles.boldLabel);
        deviceName = EditorGUILayout.TextField("Device Name", deviceName);
        category = (DeviceCategory)EditorGUILayout.EnumPopup("Category", category);
        includeSignalGenerator = EditorGUILayout.Toggle("Include Signal Generator", includeSignalGenerator);

        if (GUILayout.Button("Create Device"))
        {
            if (string.IsNullOrEmpty(deviceName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a device name.", "OK");
                return;
            }
            CreateDevice();
        }
    }

    private void CreateDevice()
    {
        string interfaceName = deviceName.ToLower() + "DeviceInterface";
        string dataName = deviceName + "Data";
        string generatorName = deviceName.ToLower() + "SignalGenerator";

        // 1. Create Script
        string scriptPath = CreateScripts(deviceName, interfaceName, dataName, generatorName);

        // 2. Inject into DeviceType
        InjectIntoDeviceType(deviceName, category);

        // 3. Inject into SaveLoadInterface
        InjectIntoSaveLoad(dataName);

        // 4. Register pending device for post-compilation prefab setup
        EditorPrefs.SetString("OSL_DeviceName", deviceName);
        EditorPrefs.SetString("OSL_InterfaceName", interfaceName);
        EditorPrefs.SetString("OSL_GeneratorName", includeSignalGenerator ? generatorName : "");
        EditorPrefs.SetBool("OSL_PendingDevice", true);

        AssetDatabase.Refresh();
        Debug.Log($"Created infrastructure for {deviceName}. Waiting for compilation to finalize prefabs...");

        EditorUtility.DisplayDialog("Step 1 Complete", $"Infrastructure for {deviceName} created.\n\nUnity is now compiling. The prefabs will be automatically finalized and populated once compilation finishes.", "OK");
    }

    private string CreateScripts(string name, string interfaceName, string dataName, string generatorName)
    {
        string folderPath = $"Assets/Scripts/{name}";
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        // Interface Template
        string interfaceTemplate = $@"using UnityEngine;
using System.Collections;
using System.Xml.Serialization;

public class {interfaceName} : deviceInterface
{{
    {(includeSignalGenerator ? $"private {generatorName} signal;" : "")}

    public override void Awake()
    {{
        base.Awake();
        {(includeSignalGenerator ? $"signal = GetComponent<{generatorName}>();" : "")}
    }}

    public override InstrumentData GetData()
    {{
        {dataName} data = new {dataName}
        {{
            deviceType = DeviceType.{name}
        }};
        GetTransformData(data);
        return data;
    }}

    public override void Load(InstrumentData d, bool copyMode)
    {{
        {dataName} data = d as {dataName};
        base.Load(data, copyMode);
    }}
}}

[XmlType(""{dataName}"")]
public class {dataName} : InstrumentData
{{
}}
";
        File.WriteAllText(Path.Combine(folderPath, interfaceName + ".cs"), interfaceTemplate);

        // Generator Template
        if (includeSignalGenerator)
        {
            string generatorTemplate = $@"using UnityEngine;
using System.Collections;

public class {generatorName} : signalGenerator
{{
    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {{
        // TODO: Implement signal processing logic
    }}
}}
";
            File.WriteAllText(Path.Combine(folderPath, generatorName + ".cs"), generatorTemplate);
        }

        return folderPath;
    }

    private static void FinalizePrefabs(string name, string interfaceName, string generatorName)
    {
        string prefabFolder = "Assets/Resources/Prefabs";
        string menuFolder = "Assets/Resources/MenuPrefabs";

        if (!Directory.Exists(prefabFolder)) Directory.CreateDirectory(prefabFolder);
        if (!Directory.Exists(menuFolder)) Directory.CreateDirectory(menuFolder);

        string basePrefabPath = Path.Combine(prefabFolder, name + ".prefab");
        string menuPrefabPath = Path.Combine(menuFolder, name + "_Menu.prefab");

        // 1. Create/Update Base Prefab
        GameObject baseObj = new GameObject(name);

        // ADD COMPONENTS IN ORDER (Interface/Generator FIRST as requested)
        System.Type interfaceType = System.Type.GetType(interfaceName + ",Assembly-CSharp");
        System.Type generatorType = !string.IsNullOrEmpty(generatorName) ? System.Type.GetType(generatorName + ",Assembly-CSharp") : null;

        if (generatorType != null) baseObj.AddComponent(generatorType);
        if (interfaceType != null) baseObj.AddComponent(interfaceType);
        else Debug.LogError($"Could not find type {interfaceName}. Make sure it compiled correctly.");

        // Networking
        baseObj.AddComponent<NetworkIdentity>();
        baseObj.AddComponent<NetworkAuthorityHandle>();

        // NetworkTransformUnreliable (Mirror)
        var netTrans = baseObj.AddComponent<NetworkTransformUnreliable>();
        netTrans.syncDirection = SyncDirection.ClientToServer;

        GameObject basePrefab = PrefabUtility.SaveAsPrefabAsset(baseObj, basePrefabPath);

        // 2. Create Menu Variant
        GameObject variantObj = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        PrefabUtility.SaveAsPrefabAsset(variantObj, menuPrefabPath);

        DestroyImmediate(baseObj);
        DestroyImmediate(variantObj);
    }

    private void InjectIntoDeviceType(string name, DeviceCategory cat)
    {
        string path = "Assets/Scripts/Menu/menuItem.cs";
        string content = File.ReadAllText(path);

        // Find the category marker, e.g., "// WaveGenerator"
        string marker = $"// {cat.ToString()}";
        string newField = $"    public static readonly DeviceType {name} = new DeviceType(\"{name}\", DeviceCategory.{cat}, 1);";

        if (content.Contains(newField)) return;

        // Insert after the marker
        int index = content.IndexOf(marker);
        if (index != -1)
        {
            int nextLine = content.IndexOf('\n', index) + 1;
            content = content.Insert(nextLine, newField + "\n");
            File.WriteAllText(path, content);
        }
    }

    private void InjectIntoSaveLoad(string dataName)
    {
        string path = "Assets/Scripts/CoreClasses/SaveLoadInterface.cs";
        string content = File.ReadAllText(path);

        string newInclude = $"[XmlInclude(typeof({dataName}))]";
        if (content.Contains(newInclude)) return;

        // Find the last XmlInclude and add after it
        int lastInclude = content.LastIndexOf("[XmlInclude");
        if (lastInclude != -1)
        {
            int nextLine = content.IndexOf('\n', lastInclude) + 1;
            content = content.Insert(nextLine, newInclude + "\n");
            File.WriteAllText(path, content);
        }
    }
}
