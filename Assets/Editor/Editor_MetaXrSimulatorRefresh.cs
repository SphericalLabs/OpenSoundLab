using System;
using System.IO;
using System.Reflection;
using Meta.XR.Simulator.Editor;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Editor_MetaXrSimulatorRefresh
{
    const string refreshMenuPath = "OpenSoundLab/Play Mode/Refresh Meta XR Simulator";
    const string jsonServerPathMenuPath = "OpenSoundLab/Play Mode/Set Meta XR JSON Server Path";
    const string jsonServerToggleMenuPath = "OpenSoundLab/Play Mode/Use Meta XR JSON Server";

    // the idea of this class is to auto-disable the glitchy XR simulator when leaving play mod
    // so that it does not catch any stale inputs from mouse and keyboards which are then played back
    // during the next run, causing glitchy behaviours
    const string autoCycleMenuPath = "OpenSoundLab/Play Mode/Auto-disable XR mode";
    const string jsonServerPathPrefKey = "OpenSoundLab.MetaXrSimulator.JsonServerPath";
    const string jsonServerEnabledPrefKey = "OpenSoundLab.MetaXrSimulator.JsonServerEnabled";
    const string autoCycleEnabledPrefKey = "OpenSoundLab.MetaXrSimulator.AutoCycleEnabled";
    const string simulatorEditorAssemblyName = "MetaXrSimulator.Editor";
    const string syntheticEnvironmentServerTypeName = "Meta.XR.Simulator.Editor.SyntheticEnvironments.SyntheticEnvironmentServer";
    const string jsonServerTypeName = "Meta.XR.Simulator.Editor.SyntheticEnvironments.JsonServer";
    const string localSharingServerTypeName = "Meta.XR.Simulator.Editor.SyntheticEnvironments.LocalSharingServer";
    const string registryTypeName = "Meta.XR.Simulator.Editor.SyntheticEnvironments.Registry";
    const string settingsTypeName = "Meta.XR.Simulator.Editor.Settings";
    const double manualDelaySeconds = 2.0d;
    static bool restartPending;
    static double restartAtTime;
    static bool logResultPending;
    static bool activateAfterRestart;

    static Editor_MetaXrSimulatorRefresh()
    {
        EditorApplication.playModeStateChanged += handlePlayModeStateChanged;
        EditorApplication.update += handleEditorUpdate;
    }

    [MenuItem(refreshMenuPath, false, 40)]
    public static void RefreshMetaXrSimulator()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Refresh Meta XR Simulator only works in Edit Mode. Exit Play Mode first.");
            return;
        }

        beginRefresh(manualDelaySeconds, true, true);
    }

    [MenuItem(refreshMenuPath, true, 40)]
    public static bool RefreshMetaXrSimulatorValidate()
    {
        return !EditorApplication.isPlaying;
    }

    [MenuItem(jsonServerPathMenuPath, false, 41)]
    public static void SetJsonServerPath()
    {
        string currentPath = EditorPrefs.GetString(jsonServerPathPrefKey, "");
        string startDirectory = string.IsNullOrEmpty(currentPath) ? "" : Path.GetDirectoryName(currentPath);
        string selectedPath = EditorUtility.OpenFilePanel("Select Meta XR JSON Environment", startDirectory, "json");
        if (string.IsNullOrEmpty(selectedPath))
        {
            return;
        }

        EditorPrefs.SetString(jsonServerPathPrefKey, selectedPath);
        Debug.Log($"Meta XR Simulator JSON server path set to {selectedPath}");
    }

    [MenuItem(jsonServerToggleMenuPath, false, 42)]
    public static void ToggleJsonServer()
    {
        bool enabled = !EditorPrefs.GetBool(jsonServerEnabledPrefKey, false);
        EditorPrefs.SetBool(jsonServerEnabledPrefKey, enabled);
        Menu.SetChecked(jsonServerToggleMenuPath, enabled);
    }

    [MenuItem(jsonServerToggleMenuPath, true, 42)]
    public static bool ToggleJsonServerValidate()
    {
        Menu.SetChecked(jsonServerToggleMenuPath, EditorPrefs.GetBool(jsonServerEnabledPrefKey, false));
        return true;
    }

    [MenuItem(autoCycleMenuPath, false, 43)]
    public static void ToggleAutoCycle()
    {
        bool enabled = !EditorPrefs.GetBool(autoCycleEnabledPrefKey, true);
        EditorPrefs.SetBool(autoCycleEnabledPrefKey, enabled);
        Menu.SetChecked(autoCycleMenuPath, enabled);
    }

    [MenuItem(autoCycleMenuPath, true, 43)]
    public static bool ToggleAutoCycleValidate()
    {
        Menu.SetChecked(autoCycleMenuPath, EditorPrefs.GetBool(autoCycleEnabledPrefKey, true));
        return true;
    }

    static void handlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (!EditorPrefs.GetBool(autoCycleEnabledPrefKey, true))
        {
            return;
        }

        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            stopServers(false);
            if (Enabler.Activated)
            {
                Enabler.DeactivateSimulator(false);
            }
            return;
        }

        // Manual refresh only; do not auto-start on play.
        return;
    }

    static void handleEditorUpdate()
    {
        if (!restartPending)
        {
            return;
        }

        if (EditorApplication.timeSinceStartup < restartAtTime)
        {
            return;
        }

        restartPending = false;

        if (activateAfterRestart)
        {
            Enabler.ActivateSimulator(false);
        }

        restartServers(logResultPending);
        logResultPending = false;
    }

    static void beginRefresh(double delaySeconds, bool logResult, bool forceActivate)
    {
        if (restartPending)
        {
            return;
        }

        if (!forceActivate && !Enabler.Activated)
        {
            return;
        }

        bool wasActivated = Enabler.Activated;
        bool shouldDeactivate = wasActivated && !EditorApplication.isPlaying;
        activateAfterRestart = forceActivate || shouldDeactivate;
        stopServers(logResult);

        if (shouldDeactivate)
        {
            Enabler.DeactivateSimulator(false);
        }

        restartAtTime = EditorApplication.timeSinceStartup + delaySeconds;
        restartPending = true;
        logResultPending = logResult;
    }

    static void stopServers(bool logResult)
    {
        invokeStaticMethod(syntheticEnvironmentServerTypeName, "Stop", null, logResult);
        invokeStaticMethod(jsonServerTypeName, "Stop", null, logResult);
        invokeStaticMethod(localSharingServerTypeName, "Stop", null, logResult);
    }

    static void restartServers(bool logResult)
    {
        bool displayServers = getDisplayServers();
        bool useJsonServer = EditorPrefs.GetBool(jsonServerEnabledPrefKey, false);

        bool environmentStarted = false;
        if (useJsonServer)
        {
            if (!startJsonServer(logResult))
            {
                if (logResult)
                {
                    Debug.LogWarning("Meta XR Simulator JSON server path not set. Skipping JSON server restart.");
                }
            }
        }
        else
        {
            environmentStarted = startEnvironmentServer(displayServers, logResult);
        }

        if (!environmentStarted)
        {
            startLocalSharingServer(displayServers, logResult);
        }

        if (logResult)
        {
            Debug.Log("Meta XR Simulator refresh requested.");
        }
    }

    static bool startEnvironmentServer(bool displayServers, bool logResult)
    {
        string lastEnvironment = getLastEnvironment(logResult);
        if (string.IsNullOrEmpty(lastEnvironment))
        {
            return false;
        }

        object environment = getEnvironmentByName(lastEnvironment, logResult);
        if (environment == null)
        {
            if (logResult)
            {
                Debug.LogWarning($"Meta XR Simulator environment not found: {lastEnvironment}");
            }
            return false;
        }

        return invokeInstanceMethod(environment, "Launch", new object[] { true, displayServers }, logResult);
    }

    static bool startJsonServer(bool logResult)
    {
        string jsonPath = EditorPrefs.GetString(jsonServerPathPrefKey, "");
        if (string.IsNullOrEmpty(jsonPath))
        {
            return false;
        }

        return invokeStaticMethod(jsonServerTypeName, "Start", new object[] { jsonPath }, logResult);
    }

    static bool startLocalSharingServer(bool displayServers, bool logResult)
    {
        return invokeStaticMethod(localSharingServerTypeName, "Start", new object[] { true, displayServers }, logResult);
    }

    static string getLastEnvironment(bool logResult)
    {
        return getStaticPropertyValue(settingsTypeName, "LastEnvironment", logResult) as string;
    }

    static bool getDisplayServers()
    {
        object value = getStaticPropertyValue(settingsTypeName, "DisplayServers", false);
        return value is bool displayServers && displayServers;
    }

    static object getEnvironmentByName(string environmentName, bool logResult)
    {
        Type registryType = getSimulatorType(registryTypeName);
        if (registryType == null)
        {
            if (logResult)
            {
                Debug.LogWarning("Meta XR Simulator registry type not found.");
            }
            return null;
        }

        MethodInfo getByInternalName = registryType.GetMethod("GetByInternalName", BindingFlags.Public | BindingFlags.Static);
        object environment = getByInternalName?.Invoke(null, new object[] { environmentName });
        if (environment != null)
        {
            return environment;
        }

        MethodInfo getByName = registryType.GetMethod("GetByName", BindingFlags.Public | BindingFlags.Static);
        return getByName?.Invoke(null, new object[] { environmentName });
    }

    static object getStaticPropertyValue(string typeName, string propertyName, bool logResult)
    {
        Type type = getSimulatorType(typeName);
        if (type == null)
        {
            if (logResult)
            {
                Debug.LogWarning($"Meta XR Simulator type not found: {typeName}");
            }
            return null;
        }

        PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (property == null)
        {
            if (logResult)
            {
                Debug.LogWarning($"Meta XR Simulator property not found: {typeName}.{propertyName}");
            }
            return null;
        }

        return property.GetValue(null);
    }

    static bool invokeStaticMethod(string typeName, string methodName, object[] args, bool logResult)
    {
        Type type = getSimulatorType(typeName);
        if (type == null)
        {
            if (logResult)
            {
                Debug.LogWarning($"Meta XR Simulator type not found: {typeName}");
            }
            return false;
        }

        MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null)
        {
            if (logResult)
            {
                Debug.LogWarning($"Meta XR Simulator method not found: {typeName}.{methodName}");
            }
            return false;
        }

        method.Invoke(null, args);
        return true;
    }

    static bool invokeInstanceMethod(object target, string methodName, object[] args, bool logResult)
    {
        if (target == null)
        {
            return false;
        }

        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
        {
            if (logResult)
            {
                Debug.LogWarning($"Meta XR Simulator method not found: {target.GetType().FullName}.{methodName}");
            }
            return false;
        }

        method.Invoke(target, args);
        return true;
    }

    static Type getSimulatorType(string typeName)
    {
        Type type = Type.GetType($"{typeName}, {simulatorEditorAssemblyName}");
        if (type != null)
        {
            return type;
        }

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetName().Name == simulatorEditorAssemblyName)
            {
                return assembly.GetType(typeName);
            }
        }

        return null;
    }
}
