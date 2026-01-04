using UnityEngine;
using Mirror;
using System.Runtime.InteropServices;

public class ClientServerPanorama : MonoBehaviour
{
    [Header("Platform Settings")]
    public bool activeInEditor = true;
    public bool activeInMac = true;
    public bool activeInWindows = true;
    public bool activeInLinux = true;

    [Header("Panorama Settings")]
    [Range(-1f, 1f)]
    public float serverPan = -1f; // Default Left
    [Range(-1f, 1f)]
    public float clientPan = 1f;  // Default Right

    [DllImport("OSLNative")]
    static extern void MasterBusRecorder_SetPanorama(float pan);
    [DllImport("OSLNative")]
    static extern bool MasterBusRecorder_IsReady();
    [DllImport("OSLNative")]
    static extern float MasterBusRecorder_GetPanorama();

    bool hasLoggedMissingEntryPoint;
    bool hasLoggedNotReady;
    bool hasLoggedMismatch;

    private void Awake()
    {
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("ClientServerPanorama: Cannot call DontDestroyOnLoad on a child GameObject. Please ensure this GameObject is at the root of the hierarchy.", this);
        }
    }

    void Update()
    {
        if (!ShouldRun()) return;

        float pan = NetworkServer.active ? serverPan : clientPan;
        try
        {
            if (!MasterBusRecorder_IsReady())
            {
                if (!hasLoggedNotReady)
                {
                    Debug.LogWarning("ClientServerPanorama: MasterBusRecorder instance not ready. Check plugin load and mixer effect.", this);
                    hasLoggedNotReady = true;
                }
                return;
            }

            MasterBusRecorder_SetPanorama(pan);

            if (!hasLoggedMismatch)
            {
                float appliedPan = MasterBusRecorder_GetPanorama();
                if (Mathf.Abs(appliedPan - pan) > 0.001f)
                {
                    Debug.LogWarning("ClientServerPanorama: Panorama write mismatch. Native pan is " + appliedPan + " while desired is " + pan + ".", this);
                    hasLoggedMismatch = true;
                }
            }
        }
        catch (System.EntryPointNotFoundException)
        {
            if (!hasLoggedMissingEntryPoint)
            {
                Debug.LogWarning("ClientServerPanorama: OSLNative entry points missing. Check plugin import settings and rebuild.", this);
                hasLoggedMissingEntryPoint = true;
            }
        }
    }

    bool ShouldRun()
    {
        if (Application.isEditor) return activeInEditor;

        switch (Application.platform)
        {
            case RuntimePlatform.OSXPlayer:
                return activeInMac;
            case RuntimePlatform.WindowsPlayer:
                return activeInWindows;
            case RuntimePlatform.LinuxPlayer:
                return activeInLinux;
            default:
                return false;
        }
    }
}
