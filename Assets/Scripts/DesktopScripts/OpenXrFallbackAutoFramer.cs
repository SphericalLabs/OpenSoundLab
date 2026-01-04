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
//
// Copyright © 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright © 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright © 2017 Apache 2.0 Google LLC SoundStage VR
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

/// <summary>
/// Auto-frames the scene to keep everything visible when XR isn't driving the rig (Editor + macOS player).
/// Disables the OVR Rig and uses a dedicated fallback camera if active.
/// </summary>
public class OpenXrFallbackAutoFramer : MonoBehaviour
{
    [Header("References")]
    public GameObject ovrRig;
    public string patchAnchorName = "PatchAnchor";

    [Header("Settings")]
    public LayerMask includeLayers = ~0;
    public float boundsPadding = 1.2f;
    public float minDistance = 1.0f;
    public float smoothing = 5f;
    public float fieldOfView = 60f;

    [Header("Debug Info")]
    [SerializeField] private bool isAutoFramingActive;
    [SerializeField] private Camera fallbackCamera;
    [SerializeField] private Transform currentContentRoot;

    private readonly List<Renderer> rendererBuffer = new List<Renderer>(512);
    private readonly List<GameObject> rootBuffer = new List<GameObject>(128);

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
            Debug.LogWarning("OpenXrFallbackAutoFramer: Cannot call DontDestroyOnLoad on a child GameObject. Please ensure this GameObject is at the root of the hierarchy.", this);
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

    private readonly List<Camera> rigCameras = new List<Camera>();
    private readonly List<AudioListener> rigListeners = new List<AudioListener>();

    private void Update()
    {
        isAutoFramingActive = ShouldAutoFrame();

        if (isAutoFramingActive)
        {
            // Fallback mode: Disable OVR rig rendering but keep tracking/scripts alive
            SetRigComponentsActive(false);

            EnsureFallbackCamera();

            if (TryGetContentBounds(out Bounds bounds))
            {
                ApplyFraming(bounds);
            }
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
                GameObject camGo = new GameObject("FallbackAutoFrameCamera");
                camGo.transform.SetParent(this.transform);
                camGo.transform.localPosition = Vector3.zero;
                camGo.transform.localRotation = Quaternion.identity;
                fallbackCamera = camGo.AddComponent<Camera>();
                fallbackCamera.nearClipPlane = 0.01f;
                fallbackCamera.farClipPlane = 1000f;
                camGo.AddComponent<AudioListener>(); // Keep audio working
            }
        }

        if (!fallbackCamera.gameObject.activeSelf)
        {
            fallbackCamera.gameObject.SetActive(true);
            fallbackCamera.fieldOfView = fieldOfView;
        }
    }

    private bool ShouldAutoFrame()
    {
        // If we survived Awake, we are in a Desktop Standalone build (not Editor/Android).
        // Only auto-frame if XR isn't actively driving the camera.
        return !XRSettings.isDeviceActive;
    }

    private bool TryGetContentBounds(out Bounds bounds)
    {
        bounds = new Bounds();
        bool hasBounds = false;

        rendererBuffer.Clear();

        // 1. Prefer PatchAnchor
        GameObject patchAnchor = GameObject.Find(patchAnchorName);
        if (patchAnchor != null)
        {
            currentContentRoot = patchAnchor.transform;
            patchAnchor.GetComponentsInChildren(false, rendererBuffer);
        }
        else
        {
            // 2. Scan all loaded scene renderers
            currentContentRoot = null;
            rootBuffer.Clear();
            SceneManager.GetActiveScene().GetRootGameObjects(rootBuffer);
            foreach (var root in rootBuffer)
            {
                // Don't include the rig or ourselves in the bounds calculation
                if (root == ovrRig || root == gameObject) continue;
                root.GetComponentsInChildren(false, rendererBuffer);
            }
        }

        foreach (var renderer in rendererBuffer)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            if (((1 << renderer.gameObject.layer) & includeLayers.value) == 0) continue;

            // Strict check: ignore renderers that are part of the rig or this framer GO
            if (ovrRig != null && renderer.transform.IsChildOf(ovrRig.transform)) continue;
            if (renderer.transform.IsChildOf(transform)) continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void ApplyFraming(Bounds bounds)
    {
        if (fallbackCamera == null) return;

        Vector3 targetCenter = bounds.center;
        float radius = bounds.extents.magnitude;

        if (radius < 0.001f) return;

        float fov = fallbackCamera.fieldOfView;
        float aspect = fallbackCamera.aspect;

        float distance = (radius * boundsPadding) / Mathf.Sin(fov * 0.5f * Mathf.Deg2Rad);

        if (aspect < 1f)
        {
            float horizontalFov = 2.0f * Mathf.Atan(Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * aspect);
            float distHorizontal = (radius * boundsPadding) / Mathf.Sin(horizontalFov * 0.5f);
            distance = Mathf.Max(distance, distHorizontal);
        }

        distance = Mathf.Max(distance, minDistance);

        // Position this GO (which parents the camera)
        Vector3 backDir = -transform.forward;
        backDir.y = 0;
        if (backDir.sqrMagnitude < 0.001f) backDir = -Vector3.forward;
        backDir.Normalize();

        Vector3 viewOffset = (backDir + Vector3.up * 0.4f).normalized * distance;
        Vector3 targetPos = targetCenter + viewOffset;

        // Since fallbackCamera is local 0,0,0, we just move 'this'
        if (smoothing > 0 && Application.isPlaying)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothing);

            Vector3 lookDir = targetCenter - transform.position;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * smoothing);
            }
        }
        else
        {
            transform.position = targetPos;
            Vector3 lookDir = targetCenter - transform.position;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
            }
        }

        Vector3 angles = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(angles.x, angles.y, 0);
    }
}
