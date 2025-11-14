using System;
using UnityEngine;
#if UNITY_ANDROID
using AndroidPermission = UnityEngine.Android.Permission;
using AndroidPermissionCallbacks = UnityEngine.Android.PermissionCallbacks;
#endif

/// <summary>
/// Handles microphone and scene permission requests for the requirements wizard.
/// </summary>
public class RequirementsPermissionService : IDisposable
{
    public const string MicrophonePermissionName = "android.permission.RECORD_AUDIO";
    public const string ScenePermissionName = OVRPermissionsRequester.ScenePermission;

#if UNITY_ANDROID
    AndroidPermissionCallbacks microphoneCallbacks;
    AndroidPermissionCallbacks scenePermissionCallbacks;
#endif

    Action onMicrophoneGranted;
    Action onMicrophoneDenied;
    Action onSceneGranted;
    Action onSceneDenied;

    public bool WaitingForMicrophoneResponse { get; private set; }
    public bool MicrophoneDenied { get; private set; }
    public bool WaitingForScenePermissionResponse { get; private set; }
    public bool ScenePermissionDenied { get; private set; }

    public bool HasMicrophonePermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return AndroidPermission.HasUserAuthorizedPermission(MicrophonePermissionName);
#else
        return true;
#endif
    }

    public bool HasScenePermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return AndroidPermission.HasUserAuthorizedPermission(ScenePermissionName);
#else
        return true;
#endif
    }

    public void RequestMicrophonePermission(Action granted, Action deniedOrPending)
    {
        WaitingForMicrophoneResponse = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (HasMicrophonePermission())
        {
            WaitingForMicrophoneResponse = false;
            MicrophoneDenied = false;
            granted?.Invoke();
            return;
        }

        onMicrophoneGranted = granted;
        onMicrophoneDenied = deniedOrPending;
        ensureMicrophoneCallbacks();
        AndroidPermission.RequestUserPermission(MicrophonePermissionName, microphoneCallbacks);
#else
        WaitingForMicrophoneResponse = false;
        MicrophoneDenied = false;
        granted?.Invoke();
#endif
    }

    public void RequestScenePermission(Action granted, Action deniedOrPending)
    {
        WaitingForScenePermissionResponse = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (HasScenePermission())
        {
            WaitingForScenePermissionResponse = false;
            ScenePermissionDenied = false;
            granted?.Invoke();
            return;
        }

        onSceneGranted = granted;
        onSceneDenied = deniedOrPending;
        ensureSceneCallbacks();
        AndroidPermission.RequestUserPermission(ScenePermissionName, scenePermissionCallbacks);
#else
        WaitingForScenePermissionResponse = false;
        ScenePermissionDenied = false;
        granted?.Invoke();
#endif
    }

    public void ResetRequestFlags()
    {
        WaitingForMicrophoneResponse = false;
        WaitingForScenePermissionResponse = false;
    }

    public void Dispose()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (microphoneCallbacks != null)
        {
            microphoneCallbacks.PermissionGranted -= handleMicrophoneGranted;
            microphoneCallbacks.PermissionDenied -= handleMicrophoneDenied;
            microphoneCallbacks.PermissionDeniedAndDontAskAgain -= handleMicrophoneDenied;
        }

        if (scenePermissionCallbacks != null)
        {
            scenePermissionCallbacks.PermissionGranted -= handleSceneGranted;
            scenePermissionCallbacks.PermissionDenied -= handleSceneDenied;
            scenePermissionCallbacks.PermissionDeniedAndDontAskAgain -= handleSceneDenied;
        }
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    void ensureMicrophoneCallbacks()
    {
        if (microphoneCallbacks != null)
        {
            return;
        }

        microphoneCallbacks = new AndroidPermissionCallbacks();
        microphoneCallbacks.PermissionGranted += handleMicrophoneGranted;
        microphoneCallbacks.PermissionDenied += handleMicrophoneDenied;
        microphoneCallbacks.PermissionDeniedAndDontAskAgain += handleMicrophoneDenied;
    }

    void ensureSceneCallbacks()
    {
        if (scenePermissionCallbacks != null)
        {
            return;
        }

        scenePermissionCallbacks = new AndroidPermissionCallbacks();
        scenePermissionCallbacks.PermissionGranted += handleSceneGranted;
        scenePermissionCallbacks.PermissionDenied += handleSceneDenied;
        scenePermissionCallbacks.PermissionDeniedAndDontAskAgain += handleSceneDenied;
    }

    void handleMicrophoneGranted(string permission)
    {
        if (permission != MicrophonePermissionName)
        {
            return;
        }

        WaitingForMicrophoneResponse = false;
        MicrophoneDenied = false;
        onMicrophoneGranted?.Invoke();
    }

    void handleMicrophoneDenied(string permission)
    {
        if (permission != MicrophonePermissionName)
        {
            return;
        }

        WaitingForMicrophoneResponse = false;
        MicrophoneDenied = true;
        onMicrophoneDenied?.Invoke();
    }

    void handleSceneGranted(string permission)
    {
        if (permission != ScenePermissionName)
        {
            return;
        }

        WaitingForScenePermissionResponse = false;
        ScenePermissionDenied = false;
        onSceneGranted?.Invoke();
    }

    void handleSceneDenied(string permission)
    {
        if (permission != ScenePermissionName)
        {
            return;
        }

        WaitingForScenePermissionResponse = false;
        ScenePermissionDenied = true;
        onSceneDenied?.Invoke();
    }
#endif
}
