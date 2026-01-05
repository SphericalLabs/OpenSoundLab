// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2026 OSLLv1 Sphericals OpenSoundLab
//
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
//
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
//

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.InputSystem;

/// <summary>
/// A flying camera controller that activates when XR isn't driving the rig.
/// Deactivates the OVR Rig's cameras/listeners and provides a manual WASD+Mouse control scheme.
/// </summary>
public class OpenXrFallbackFlyingCamera : MonoBehaviour
{
    [Header("References")]
    public GameObject ovrRig;

    [Header("Settings")]
    public float baseSpeed = 0.2f;
    public float fastMultiplier = 2.0f;
    public float mouseSensitivity = 3.0f;
    public float fieldOfView = 80f;
    public float zoomSensitivity = 10f;

    [Header("Debug Info")]
    [SerializeField] private bool isFlyingActive;
    [SerializeField] private Camera fallbackCamera;

    private float rotationX = 0f;
    private float rotationY = 0f;

    private const string PREF_POS_X = "OSL_FallbackCam_PosX";
    private const string PREF_POS_Y = "OSL_FallbackCam_PosY";
    private const string PREF_POS_Z = "OSL_FallbackCam_PosZ";
    private const string PREF_ROT_X = "OSL_FallbackCam_RotX";
    private const string PREF_ROT_Y = "OSL_FallbackCam_RotY";
    private const string PREF_FOV = "OSL_FallbackCam_FOV";

    private bool isStateDirty;
    private float saveCooldownTimer;
    private const float SAVE_COOLDOWN = 1.0f; // Save 1 second after last input

    private readonly List<Camera> rigCameras = new List<Camera>();
    private readonly List<AudioListener> rigListeners = new List<AudioListener>();

    private void Awake()
    {
        // Platform check: Only run in Desktop Standalone builds (Windows, Mac, Linux).
        // Explicitly exclude Editor and Android/Mobile.
#if !UNITY_STANDALONE || UNITY_EDITOR
        Destroy(this);
        return;
#endif

        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("OpenXrFallbackFlyingCamera: Cannot call DontDestroyOnLoad on a child GameObject. Please ensure this GameObject is at the root of the hierarchy.", this);
        }

        FindOvrRig();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        FindOvrRig();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Ensure rig is restored if this component is disabled
        if (ovrRig != null)
        {
            SetRigComponentsActive(true);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ovrRig = null;
        rigCameras.Clear();
        rigListeners.Clear();
        FindOvrRig();
    }

    private void FindOvrRig()
    {
        if (ovrRig == null)
        {
            ovrRig = GameObject.Find("OVRCameraRig-Variant");
            if (ovrRig == null) ovrRig = GameObject.Find("OVRCameraRig");
        }
    }

    private void Update()
    {
        isFlyingActive = ShouldBeActive();

        if (isFlyingActive)
        {
            // Fallback mode: Disable OVR rig rendering but keep tracking/scripts alive
            SetRigComponentsActive(false);

            EnsureFallbackCamera();
            HandleMovement();
            HandleRotation();
            HandleZoom();
            HandlePresets();
            UpdateSaving();
        }
        else
        {
            // XR mode: Re-enable OVR rig rendering
            SetRigComponentsActive(true);

            if (fallbackCamera != null)
            {
                fallbackCamera.gameObject.SetActive(false);
            }
        }
    }

    private bool ShouldBeActive()
    {
        // Only active if XR isn't actively driving the camera.
        return !XRSettings.isDeviceActive;
    }

    private void SetRigComponentsActive(bool active)
    {
        if (ovrRig == null) return;

        // Cache components if list is empty
        if (rigCameras.Count == 0)
        {
            ovrRig.GetComponentsInChildren(true, rigCameras);
            ovrRig.GetComponentsInChildren(true, rigListeners);
        }

        // Toggle Cameras
        foreach (var cam in rigCameras)
        {
            if (cam != null && cam.enabled != active)
            {
                cam.enabled = active;
            }
        }

        // Toggle AudioListeners
        foreach (var listener in rigListeners)
        {
            if (listener != null && listener.enabled != active)
            {
                listener.enabled = active;
            }
        }

        // Ensure the rig itself is active so scripts keep running
        if (!ovrRig.activeSelf) ovrRig.SetActive(true);
    }

    private void EnsureFallbackCamera()
    {
        if (fallbackCamera == null)
        {
            // Check if we already have one as child
            fallbackCamera = GetComponentInChildren<Camera>(true);

            if (fallbackCamera == null)
            {
                GameObject camGo = new GameObject("FallbackFlyingCamera");
                camGo.transform.SetParent(this.transform);
                camGo.transform.localPosition = Vector3.zero;
                camGo.transform.localRotation = Quaternion.identity;
                fallbackCamera = camGo.AddComponent<Camera>();
                fallbackCamera.nearClipPlane = 0.01f;
                fallbackCamera.farClipPlane = 1000f;
                camGo.AddComponent<AudioListener>(); // Keep audio working
            }

            // Initialize rotation values from current transform
            rotationX = transform.eulerAngles.y;
            rotationY = -transform.eulerAngles.x;

            LoadState();
        }

        if (!fallbackCamera.gameObject.activeSelf)
        {
            fallbackCamera.gameObject.SetActive(true);
            fallbackCamera.fieldOfView = fieldOfView;
        }
    }

    private void HandleMovement()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        float currentSpeed = baseSpeed;
        if (keyboard.shiftKey.isPressed)
        {
            currentSpeed *= fastMultiplier;
        }

        Vector3 moveDir = Vector3.zero;

        // WASD on plane (horizontal)
        Vector3 camForward = transform.forward;
        camForward.y = 0;
        if (camForward.sqrMagnitude < 0.001f) camForward = transform.up; // Fallback for looking straight up/down
        camForward.Normalize();

        Vector3 camRight = transform.right;
        camRight.y = 0;
        camRight.Normalize();

        if (keyboard.wKey.isPressed) moveDir += camForward;
        if (keyboard.sKey.isPressed) moveDir -= camForward;
        if (keyboard.aKey.isPressed) moveDir -= camRight;
        if (keyboard.dKey.isPressed) moveDir += camRight;

        // R/F for vertical
        if (keyboard.rKey.isPressed) moveDir += Vector3.up;
        if (keyboard.fKey.isPressed) moveDir -= Vector3.up;

        if (moveDir.sqrMagnitude > 0.001f)
        {
            transform.position += moveDir.normalized * currentSpeed * Time.deltaTime;
            MarkDirty();
        }
    }

    private void HandleRotation()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.rightButton.isPressed) // Right click
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Vector2 delta = mouse.delta.ReadValue() * 0.05f; // Scale down Delta for sensitivity
            rotationX += delta.x * mouseSensitivity;
            rotationY += delta.y * mouseSensitivity;
            rotationY = Mathf.Clamp(rotationY, -90f, 90f);

            transform.rotation = Quaternion.Euler(-rotationY, rotationX, 0);
            MarkDirty();
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void HandleZoom()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            // Scrolling up (positive) reduces FOV (zooms in)
            // Note: Scroll Delta in New Input System is usually 120 per notch or similar
            fieldOfView -= (scroll / 120f) * zoomSensitivity;
            fieldOfView = Mathf.Clamp(fieldOfView, 10f, 180f);

            if (fallbackCamera != null)
            {
                fallbackCamera.fieldOfView = fieldOfView;
            }
            MarkDirty();
        }
    }

    private void HandlePresets()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        bool isShift = keyboard.shiftKey.isPressed;

        for (int i = 0; i <= 9; i++)
        {
            Key key = i == 0 ? Key.Digit0 : (Key)((int)Key.Digit1 + (i - 1));
            if (keyboard[key].wasPressedThisFrame)
            {
                if (isShift)
                {
                    SavePreset(i);
                }
                else
                {
                    LoadPreset(i);
                }
            }
        }
    }

    private string GetPresetPrefix(int index) => $"OSL_FallbackCam_Preset{index}_";

    private void SavePreset(int index)
    {
        string prefix = GetPresetPrefix(index);
        PlayerPrefs.SetFloat(prefix + "PosX", transform.position.x);
        PlayerPrefs.SetFloat(prefix + "PosY", transform.position.y);
        PlayerPrefs.SetFloat(prefix + "PosZ", transform.position.z);
        PlayerPrefs.SetFloat(prefix + "RotX", rotationX);
        PlayerPrefs.SetFloat(prefix + "RotY", rotationY);
        PlayerPrefs.SetFloat(prefix + "FOV", fieldOfView);
        PlayerPrefs.Save();
        Debug.Log($"OpenXrFallbackFlyingCamera: Saved Preset {index}");
    }

    private void LoadPreset(int index)
    {
        string prefix = GetPresetPrefix(index);
        if (!PlayerPrefs.HasKey(prefix + "PosX"))
        {
            Debug.LogWarning($"OpenXrFallbackFlyingCamera: Preset {index} not found.");
            return;
        }

        Vector3 pos;
        pos.x = PlayerPrefs.GetFloat(prefix + "PosX");
        pos.y = PlayerPrefs.GetFloat(prefix + "PosY");
        pos.z = PlayerPrefs.GetFloat(prefix + "PosZ");
        transform.position = pos;

        rotationX = PlayerPrefs.GetFloat(prefix + "RotX");
        rotationY = PlayerPrefs.GetFloat(prefix + "RotY");
        transform.rotation = Quaternion.Euler(-rotationY, rotationX, 0);

        fieldOfView = PlayerPrefs.GetFloat(prefix + "FOV", fieldOfView);
        if (fallbackCamera != null)
        {
            fallbackCamera.fieldOfView = fieldOfView;
        }

        Debug.Log($"OpenXrFallbackFlyingCamera: Loaded Preset {index}");
        MarkDirty(); // Ensure this position is also saved as the last known state
    }

    private void MarkDirty()
    {
        isStateDirty = true;
        saveCooldownTimer = SAVE_COOLDOWN;
    }

    private void UpdateSaving()
    {
        if (isStateDirty)
        {
            saveCooldownTimer -= Time.deltaTime;
            if (saveCooldownTimer <= 0)
            {
                SaveState();
                isStateDirty = false;
            }
        }
    }

    private void SaveState()
    {
        PlayerPrefs.SetFloat(PREF_POS_X, transform.position.x);
        PlayerPrefs.SetFloat(PREF_POS_Y, transform.position.y);
        PlayerPrefs.SetFloat(PREF_POS_Z, transform.position.z);
        PlayerPrefs.SetFloat(PREF_ROT_X, rotationX);
        PlayerPrefs.SetFloat(PREF_ROT_Y, rotationY);
        PlayerPrefs.SetFloat(PREF_FOV, fieldOfView);
        PlayerPrefs.Save();
        // Debug.Log("OpenXrFallbackFlyingCamera: Saved camera state.");
    }

    private void LoadState()
    {
        if (PlayerPrefs.HasKey(PREF_POS_X))
        {
            Vector3 pos;
            pos.x = PlayerPrefs.GetFloat(PREF_POS_X);
            pos.y = PlayerPrefs.GetFloat(PREF_POS_Y);
            pos.z = PlayerPrefs.GetFloat(PREF_POS_Z);
            transform.position = pos;

            rotationX = PlayerPrefs.GetFloat(PREF_ROT_X);
            rotationY = PlayerPrefs.GetFloat(PREF_ROT_Y);
            transform.rotation = Quaternion.Euler(-rotationY, rotationX, 0);

            fieldOfView = PlayerPrefs.GetFloat(PREF_FOV, fieldOfView);
            if (fallbackCamera != null)
            {
                fallbackCamera.fieldOfView = fieldOfView;
            }
            // Debug.Log("OpenXrFallbackFlyingCamera: Loaded camera state.");
        }
    }
}
