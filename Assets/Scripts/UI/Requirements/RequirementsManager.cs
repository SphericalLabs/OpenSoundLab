using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;
using Meta.XR.EnvironmentDepth;

/// <summary>
/// Runtime-only requirements flow for Meta Quest: documents (EULA, safety, notices) followed by storage permission.
/// Assumes OVRCameraRig is present; falls back to the main camera if not found.
/// </summary>
public class RequirementsManager : MonoBehaviour
{
    [Header("Player Prefs")]
    [SerializeField] string consentKey = "requirements_consent_v1";

    [Header("Documents")]
    [SerializeField] RequirementDocument[] documents;
    [SerializeField] bool autoPopulateDefaultDocuments = true;
    [SerializeField, TextArea(2, 4)] string missingDocumentFallback = "Document unavailable.";

    [Header("Environment Depth Step")]
    [SerializeField] string depthPermissionTitle = "Spatial Depth Permission";
    [SerializeField, TextArea(6, 20)] string depthPermissionBody = "OpenSoundLab is a mixed-reality experience. Grant access to spatial depth data so occlusion and scene-aware devices can function.";
    [SerializeField, TextArea(6, 20)] string depthPermissionDeniedBody = "Spatial depth access is still blocked. Enable the Spatial Scene permission in the Meta Quest settings, then choose Agree again.";

    [Header("Microphone Step")]
    [SerializeField] string microphoneTitle = "Microphone Permission";
    [SerializeField, TextArea(6, 20)] string microphoneBody = "OpenSoundLab lets you record audio input. Please allow microphone access so recordings work.";
    [SerializeField, TextArea(6, 20)] string microphoneDeniedBody = "Microphone access is still blocked. Select Agree to continue without it, or enable it later in the Meta Quest settings.";

    [Header("Storage Step")]
    [SerializeField] string permissionTitle = "Storage Permission";
    [SerializeField, TextArea(6, 20)] string permissionBody = "OpenSoundLab needs file access to load and record patches and samples. The next step opens the Meta Quest settings where you can grant the permission.";
    [SerializeField] string agreeLabel = "Agree";
    [SerializeField] string openSettingsLabel = "Open Settings";
    [SerializeField] string backLabel = "Back";

    [Header("Layout")]
    [SerializeField] float panelDistance = 1.35f;
    [SerializeField] float verticalOffset = 0.15f;
    [SerializeField] Vector2 panelSizeMeters = new Vector2(1.5f, 0.94f);
    [SerializeField] float joystickScrollSpeed = 0.5f;
    [SerializeField] float mouseScrollMultiplier = 65f;
    [SerializeField] float orientationFollowSpeed = 2.5f;
    [SerializeField] int startupPositionDelayFrames = 10;
    [SerializeField] float verticalFollowSpeed = 2f;

    [Header("Rounded Corners")]
    [SerializeField] Shader roundedCornersShader;

    [Header("Controller Hints")]
    [SerializeField] string nextButtonHint = " (X/A)";
    [SerializeField] string backButtonHint = " (Y/B)";

    [Header("Events")]
    [SerializeField] UnityEvent onRequirementsAccepted;

    [Header("Startup")]
    [SerializeField] masterControl masterControlPrefab;
    [SerializeField] bool loadLocalSceneOnCompletion = true;

    const float TargetRenderScale = 1.4f;
    const float PanelTopAlignmentRatio = 0.25f;

    enum StepType
    {
        Document,
        ScenePermission,
        Microphone,
        Storage
    }

    RequirementsWizardView view;
    RequirementsPermissionService permissions;

    Transform headAnchor;
    Camera headCamera;
    Coroutine initialPositionRoutine;
    Coroutine sceneLoadRoutine;
    bool hasPositionedPanel;
    bool hasTrackingOriginOverride;
    bool recenterSubscribed;
    bool waitingForPermissionFocus;
    bool flowActive;
    int currentStepIndex;
    OVRManager.TrackingOrigin cachedTrackingOrigin = OVRManager.TrackingOrigin.Stage;

    EnvironmentDepthManager environmentDepthManager;
    bool depthStateCached;
    bool depthManagerInitiallyEnabled = true;
    bool depthManagerSuppressed;
    bool occlusionModeForcedNone;

    static bool mouseScrollAxisUnavailable;
    static bool hasLoggedMissingMouseAxis;

    void Awake()
    {
        permissions = new RequirementsPermissionService();
        prepareEnvironmentDepthSuppression();
    }

    void OnEnable()
    {
        subscribeToRecenterEvent(true);
    }

    IEnumerator Start()
    {
        applyRenderResolutionScaling();
        ensureDefaultDocuments();

        bool consentPresent = PlayerPrefs.GetInt(consentKey, 0) == 1;
        bool hasScenePermission = permissions.HasScenePermission();
        bool storageGranted = AndroidStorageAccess.HasManageAllFilesAccess();

        prepareEnvironmentDepthSuppression();

        if (consentPresent && storageGranted && hasScenePermission)
        {
            ensureMasterControl();
            if (loadLocalSceneOnCompletion)
            {
                yield return loadLocalSceneAfterSamplesReady();
            }
            onRequirementsAccepted?.Invoke();
            enabled = false;
            yield break;
        }

        resolveUserReference();
        prepareEnvironmentDepthSuppression();
        subscribeToRecenterEvent(true);
        applyWizardTrackingOrigin();
        createWizardView();
        initializeStartupPosition();

        yield return loadDocumentBodies();

        if (documents == null || documents.Length == 0)
        {
            Debug.LogWarning("RequirementsManager: no documents configured. The flow still runs but will jump to the permission step.");
        }

        flowActive = true;

        int startIndex = consentPresent ? getDocumentCount() : 0;
        if (consentPresent)
        {
            if (hasScenePermission)
            {
                startIndex += 1;
            }

            if (permissions.HasMicrophonePermission())
            {
                startIndex += 1;
            }
        }

        showStep(startIndex);
    }

    void Update()
    {
        if (!flowActive)
        {
            return;
        }

        handleScrollInput();
        handleButtonShortcuts();
    }

    void LateUpdate()
    {
        if (view == null || headAnchor == null)
        {
            return;
        }

        updatePanelVerticalAlignment(Time.deltaTime);
        orientCanvasTowardsUser(Time.deltaTime);
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!flowActive || !hasFocus)
        {
            return;
        }

        if (getStepType(currentStepIndex) == StepType.ScenePermission && permissions.HasScenePermission())
        {
            activateEnvironmentDepthManager();
            advanceStep();
            return;
        }

        if (waitingForPermissionFocus && getStepType(currentStepIndex) == StepType.Storage)
        {
            if (AndroidStorageAccess.HasManageAllFilesAccess())
            {
                completeFlow();
            }
            else
            {
                waitingForPermissionFocus = false;
                updateNavigationState();
            }
        }
    }

    void OnDisable()
    {
        if (initialPositionRoutine != null)
        {
            StopCoroutine(initialPositionRoutine);
            initialPositionRoutine = null;
        }

        if (sceneLoadRoutine != null)
        {
            StopCoroutine(sceneLoadRoutine);
            sceneLoadRoutine = null;
        }

        subscribeToRecenterEvent(false);
        restoreTrackingOrigin();
    }

    void OnDestroy()
    {
        permissions?.Dispose();
    }

    void applyRenderResolutionScaling()
    {
        float clampedTarget = Mathf.Clamp(TargetRenderScale, 1f, 2f);

        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urpAsset)
        {
            if (urpAsset.renderScale < clampedTarget - 0.001f)
            {
                urpAsset.renderScale = clampedTarget;
            }
        }

        if (XRSettings.enabled)
        {
            if (XRSettings.eyeTextureResolutionScale < clampedTarget - 0.001f)
            {
                XRSettings.eyeTextureResolutionScale = clampedTarget;
            }

            float viewportTarget = Mathf.Clamp(clampedTarget, 0.5f, 1f);
            if (XRSettings.renderViewportScale < viewportTarget - 0.001f)
            {
                XRSettings.renderViewportScale = viewportTarget;
            }
        }
    }

    void ensureDefaultDocuments()
    {
        if (!autoPopulateDefaultDocuments)
        {
            return;
        }

        bool needsDefaults = documents == null || documents.Length == 0;
        if (!needsDefaults)
        {
            for (int i = 0; i < documents.Length; i++)
            {
                var doc = documents[i];
                if (doc == null || string.IsNullOrEmpty(doc.streamingAssetRelativePath))
                {
                    needsDefaults = true;
                    break;
                }
            }
        }

        if (!needsDefaults)
        {
            return;
        }

        documents = new RequirementDocument[]
        {
            new RequirementDocument
            {
                title = "Welcome",
                streamingAssetRelativePath = "Requirements/welcome.txt"
            },
            new RequirementDocument
            {
                title = "End-User License Agreement",
                streamingAssetRelativePath = "Requirements/eula.txt"
            },
            new RequirementDocument
            {
                title = "Health & Safety",
                streamingAssetRelativePath = "Requirements/health_and_safety.txt"
            },
            new RequirementDocument
            {
                title = "Open-Source Notices",
                streamingAssetRelativePath = "Requirements/open_source_notices.txt"
            }
        };
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ensureDefaultDocuments();
        }
    }
#endif

    IEnumerator loadDocumentBodies()
    {
        if (documents == null)
        {
            yield break;
        }

        for (int i = 0; i < documents.Length; i++)
        {
            var doc = documents[i];
            if (doc != null)
            {
                yield return doc.loadBody(missingDocumentFallback);
            }
        }
    }

    void handleButtonShortcuts()
    {
        bool nextPressed = false;
        bool backPressed = false;

#if UNITY_ANDROID || UNITY_EDITOR
        nextPressed = OVRInput.GetDown(OVRInput.Button.One) || OVRInput.GetDown(OVRInput.Button.Three);
        backPressed = OVRInput.GetDown(OVRInput.Button.Two) || OVRInput.GetDown(OVRInput.Button.Four);
#endif
        if (!nextPressed)
        {
            nextPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
        }
        if (!nextPressed)
        {
            nextPressed = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.X);
        }
        if (!backPressed)
        {
            backPressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace);
        }
        if (!backPressed)
        {
            backPressed = Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Y);
        }

        var nextButton = view?.getNextButton();
        if (nextPressed && nextButton != null && nextButton.interactable)
        {
            handleNext();
        }

        var backButton = view?.getBackButton();
        if (backPressed && backButton != null && backButton.interactable)
        {
            handleBack();
        }
    }

    void handleScrollInput()
    {
        if (view == null || view.ScrollRect == null || getStepType(currentStepIndex) != StepType.Document)
        {
            return;
        }

        float axis = 0f;
#if UNITY_ANDROID || UNITY_EDITOR
        Vector2 left = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        Vector2 right = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
        axis = Mathf.Abs(left.y) > Mathf.Abs(right.y) ? left.y : right.y;
        if (Mathf.Abs(axis) < 0.2f)
        {
            axis = 0f;
        }
#endif
        if (Mathf.Approximately(axis, 0f))
        {
            float mouseScroll = 0f;
            if (!mouseScrollAxisUnavailable)
            {
                try
                {
                    mouseScroll = Input.GetAxis("Mouse ScrollWheel");
                }
                catch (System.ArgumentException)
                {
                    mouseScrollAxisUnavailable = true;
                    if (!hasLoggedMissingMouseAxis)
                    {
                        Debug.LogWarning("RequirementsManager: legacy Input axis 'Mouse ScrollWheel' is not configured; falling back to Input.mouseScrollDelta.");
                        hasLoggedMissingMouseAxis = true;
                    }
                }
            }

            if (Mathf.Abs(mouseScroll) > 0.01f)
            {
                axis = mouseScroll * mouseScrollMultiplier;
            }
            else
            {
                float rawMouseScroll = Input.mouseScrollDelta.y;
                if (Mathf.Abs(rawMouseScroll) > 0.01f)
                {
                    axis = rawMouseScroll * mouseScrollMultiplier;
                }
            }
        }

        if (!Mathf.Approximately(axis, 0f))
        {
            float normalizedDelta = axis * joystickScrollSpeed * Time.unscaledDeltaTime;
            ScrollRect scrollRect = view.ScrollRect;
            float newValue = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + normalizedDelta);
            scrollRect.verticalNormalizedPosition = newValue;
            view.tryMarkScrolledToEnd();
        }
    }

    void handleNext()
    {
        switch (getStepType(currentStepIndex))
        {
            case StepType.Document:
                advanceStep();
                break;
            case StepType.ScenePermission:
                if (permissions.HasScenePermission())
                {
                    activateEnvironmentDepthManager();
                    advanceStep();
                }
                else if (permissions.ScenePermissionDenied && !permissions.WaitingForScenePermissionResponse)
                {
                    advanceStep();
                }
                else
                {
                    permissions.RequestScenePermission(
                        () =>
                        {
                            activateEnvironmentDepthManager();
                            if (getStepType(currentStepIndex) == StepType.ScenePermission)
                            {
                                advanceStep();
                            }
                        },
                        () =>
                        {
                            if (getStepType(currentStepIndex) == StepType.ScenePermission)
                            {
                                showScenePermissionStep();
                            }
                        });
                    updateNavigationState();
                }
                break;
            case StepType.Microphone:
                if (permissions.HasMicrophonePermission())
                {
                    advanceStep();
                }
                else if (permissions.MicrophoneDenied && !permissions.WaitingForMicrophoneResponse)
                {
                    advanceStep();
                }
                else
                {
                    permissions.RequestMicrophonePermission(
                        () =>
                        {
                            if (getStepType(currentStepIndex) == StepType.Microphone)
                            {
                                advanceStep();
                            }
                        },
                        () =>
                        {
                            if (getStepType(currentStepIndex) == StepType.Microphone)
                            {
                                showMicrophoneStep();
                            }
                        });
                    updateNavigationState();
                }
                break;
            case StepType.Storage:
                if (AndroidStorageAccess.HasManageAllFilesAccess())
                {
                    completeFlow();
                    return;
                }

                bool launched = AndroidStorageAccess.TryOpenManageAllFilesSettings();
                if (!launched)
                {
                    Debug.LogWarning("RequirementsManager: could not open Manage All Files settings.");
                }
                waitingForPermissionFocus = true;
                break;
        }
    }

    void handleBack()
    {
        int previous = currentStepIndex - 1;
        if (previous < 0)
        {
            return;
        }

        showStep(previous);
    }

    void advanceStep()
    {
        showStep(currentStepIndex + 1);
    }

    void showStep(int index)
    {
        int maxIndex = getDocumentCount() + 2;
        currentStepIndex = Mathf.Clamp(index, 0, maxIndex);
        waitingForPermissionFocus = false;
        permissions.ResetRequestFlags();

        switch (getStepType(currentStepIndex))
        {
            case StepType.Document:
                showDocumentStep(currentStepIndex);
                break;
            case StepType.ScenePermission:
                showScenePermissionStep();
                break;
            case StepType.Microphone:
                showMicrophoneStep();
                break;
            case StepType.Storage:
                showStorageStep();
                break;
        }
    }

    void showDocumentStep(int index)
    {
        var doc = documents != null && index >= 0 && index < documents.Length ? documents[index] : null;
        if (doc == null)
        {
            doc = new RequirementDocument();
            Debug.LogWarning($"RequirementsManager: document at index {index} is null.");
        }

        view.resetScrollState();
        view.setTitle(doc.title);
        view.setBody(doc.resolveBody());
        view.setNextLabel(agreeLabel);
        updateNavigationState();
    }

    void showScenePermissionStep()
    {
        if (permissions.HasScenePermission())
        {
            activateEnvironmentDepthManager();
            advanceStep();
            return;
        }

        view.resetScrollState();
        view.setTitle(depthPermissionTitle);
        view.setBody(permissions.ScenePermissionDenied ? depthPermissionDeniedBody : depthPermissionBody);
        view.setNextLabel(agreeLabel);
        updateNavigationState();
    }

    void showMicrophoneStep()
    {
        if (permissions.HasMicrophonePermission())
        {
            advanceStep();
            return;
        }

        view.resetScrollState();
        view.setTitle(microphoneTitle);
        view.setBody(permissions.MicrophoneDenied ? microphoneDeniedBody : microphoneBody);
        view.setNextLabel(agreeLabel);
        updateNavigationState();
    }

    void showStorageStep()
    {
        if (AndroidStorageAccess.HasManageAllFilesAccess())
        {
            completeFlow();
            return;
        }

        view.resetScrollState();
        view.setTitle(permissionTitle);
        view.setBody(permissionBody);
        view.setNextLabel(openSettingsLabel);
        updateNavigationState();
    }

    void updateNavigationState()
    {
        if (view == null)
        {
            return;
        }

        view.setBackInteractable(currentStepIndex > 0);

        bool showScrollHint = false;
        bool nextInteractable = true;

        switch (getStepType(currentStepIndex))
        {
            case StepType.Document:
                nextInteractable = view.HasScrolledToEnd;
                showScrollHint = !view.HasScrolledToEnd;
                break;
            case StepType.ScenePermission:
                nextInteractable = !permissions.WaitingForScenePermissionResponse;
                break;
            case StepType.Microphone:
                nextInteractable = !permissions.WaitingForMicrophoneResponse;
                break;
            case StepType.Storage:
                nextInteractable = true;
                break;
        }

        view.setNextInteractable(nextInteractable);
        view.setScrollHintVisible(showScrollHint);
    }

    int getDocumentCount()
    {
        return documents != null ? documents.Length : 0;
    }

    StepType getStepType(int index)
    {
        int docCount = getDocumentCount();
        if (index < docCount)
        {
            return StepType.Document;
        }

        if (index == docCount)
        {
            return StepType.ScenePermission;
        }

        if (index == docCount + 1)
        {
            return StepType.Microphone;
        }

        return StepType.Storage;
    }

    void prepareEnvironmentDepthSuppression()
    {
        if (permissions != null && permissions.HasScenePermission())
        {
            return;
        }

        var depthManager = resolveEnvironmentDepthManager();
        if (depthManager == null)
        {
            return;
        }

        if (depthManager.enabled)
        {
            depthManagerSuppressed = true;
            depthManager.enabled = false;
        }

        if (depthManager.OcclusionShadersMode != OcclusionShadersMode.None)
        {
            occlusionModeForcedNone = true;
            depthManager.OcclusionShadersMode = OcclusionShadersMode.None;
        }
    }

    EnvironmentDepthManager resolveEnvironmentDepthManager()
    {
        if (environmentDepthManager == null)
        {
            environmentDepthManager = FindObjectOfType<EnvironmentDepthManager>(true);
            cacheDepthManagerState();
        }

        return environmentDepthManager;
    }

    void cacheDepthManagerState()
    {
        if (environmentDepthManager == null || depthStateCached)
        {
            return;
        }

        depthManagerInitiallyEnabled = environmentDepthManager.enabled;
        depthStateCached = true;
    }

    void activateEnvironmentDepthManager()
    {
        var depthManager = resolveEnvironmentDepthManager();
        if (depthManager == null)
        {
            return;
        }

        if (occlusionModeForcedNone)
        {
            depthManager.OcclusionShadersMode = OcclusionShadersMode.SoftOcclusion;
            occlusionModeForcedNone = false;
        }

        if (depthManagerSuppressed && depthManagerInitiallyEnabled)
        {
            depthManager.enabled = true;
            depthManagerSuppressed = false;
        }
    }

    void createWizardView()
    {
        if (view != null)
        {
            view.contentUpdated -= updateNavigationState;
            view.tearDown();
        }

        view = new RequirementsWizardView(this, panelSizeMeters, roundedCornersShader, nextButtonHint, backButtonHint);
        view.build(transform, headCamera, headAnchor, handleNext, handleBack);
        view.contentUpdated += updateNavigationState;
        view.setBackLabel(backLabel);
        view.setNextLabel(agreeLabel);
    }

    void initializeStartupPosition()
    {
        if (startupPositionDelayFrames <= 0)
        {
            resolveUserReference();
            applyWizardTrackingOrigin();
            positionUiInFrontOfUser();
            return;
        }

        if (initialPositionRoutine != null)
        {
            StopCoroutine(initialPositionRoutine);
        }

        initialPositionRoutine = StartCoroutine(delayInitialPosition());
    }

    IEnumerator delayInitialPosition()
    {
        int remaining = Mathf.Max(0, startupPositionDelayFrames);
        while (remaining > 0)
        {
            remaining--;
            yield return null;
        }

        resolveUserReference();
        applyWizardTrackingOrigin();
        positionUiInFrontOfUser();
        initialPositionRoutine = null;
    }

    void applyWizardTrackingOrigin()
    {
#if UNITY_ANDROID || UNITY_EDITOR
        OVRManager manager = OVRManager.instance;
        if (manager == null)
        {
            return;
        }

        if (!hasTrackingOriginOverride)
        {
            cachedTrackingOrigin = manager.trackingOriginType;
            hasTrackingOriginOverride = true;
        }

        if (manager.trackingOriginType != OVRManager.TrackingOrigin.EyeLevel)
        {
            manager.trackingOriginType = OVRManager.TrackingOrigin.EyeLevel;
        }
#endif
    }

    void restoreTrackingOrigin()
    {
#if UNITY_ANDROID || UNITY_EDITOR
        if (!hasTrackingOriginOverride)
        {
            return;
        }

        OVRManager manager = OVRManager.instance;
        if (manager == null)
        {
            return;
        }

        manager.trackingOriginType = cachedTrackingOrigin;
        hasTrackingOriginOverride = false;
#endif
    }

    void resolveUserReference()
    {
        OVRCameraRig ovrCameraRig = FindObjectOfType<OVRCameraRig>();

        if (environmentDepthManager == null && ovrCameraRig != null)
        {
            environmentDepthManager = ovrCameraRig.GetComponent<EnvironmentDepthManager>();
            cacheDepthManagerState();
        }

        if (headAnchor == null && ovrCameraRig != null && ovrCameraRig.centerEyeAnchor != null)
        {
            headAnchor = ovrCameraRig.centerEyeAnchor;
        }

        if (headCamera == null)
        {
            if (ovrCameraRig != null)
            {
                headCamera = ovrCameraRig.GetComponentInChildren<Camera>();
            }

            if (headCamera == null)
            {
                headCamera = Camera.main;
            }

            if (headCamera == null)
            {
                headCamera = FindObjectOfType<Camera>();
            }
        }

        if (headAnchor == null && headCamera != null)
        {
            headAnchor = headCamera.transform;
        }

        if (headAnchor == null)
        {
            Debug.LogWarning("RequirementsManager: Cannot locate a head anchor; world-space UI will spawn at the origin.", this);
        }
    }

    void subscribeToRecenterEvent(bool subscribe)
    {
#if UNITY_ANDROID || UNITY_EDITOR
        OVRDisplay display = OVRManager.display;
        if (display == null)
        {
            return;
        }

        if (subscribe && !recenterSubscribed)
        {
            display.RecenteredPose += onDisplayRecenteredPose;
            recenterSubscribed = true;
        }
        else if (!subscribe && recenterSubscribed)
        {
            display.RecenteredPose -= onDisplayRecenteredPose;
            recenterSubscribed = false;
        }
#endif
    }

#if UNITY_ANDROID || UNITY_EDITOR
    void onDisplayRecenteredPose()
    {
        if (view == null || !isActiveAndEnabled)
        {
            return;
        }

        resolveUserReference();
        positionUiInFrontOfUser();
        forceVerticalAlignmentToTarget();
    }
#endif

    void positionUiInFrontOfUser()
    {
        if (view == null)
        {
            return;
        }

        Transform canvasTransform = view.CanvasTransform;
        if (canvasTransform == null)
        {
            return;
        }

        if (headAnchor == null)
        {
            canvasTransform.position = Vector3.forward * panelDistance;
            canvasTransform.rotation = Quaternion.identity;
            hasPositionedPanel = false;
            return;
        }

        float verticalShift = getVerticalAlignmentOffset();
        Vector3 targetPosition = headAnchor.position + headAnchor.forward * panelDistance;
        targetPosition.y -= verticalShift;
        canvasTransform.position = targetPosition;
        hasPositionedPanel = true;

        Vector3 lookDirection = headAnchor.position - canvasTransform.position;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            canvasTransform.rotation = Quaternion.LookRotation(-lookDirection, Vector3.up);
        }
        else
        {
            Vector3 fallbackForward = -headAnchor.forward;
            fallbackForward.y = 0f;
            if (fallbackForward.sqrMagnitude < 0.0001f)
            {
                fallbackForward = Vector3.forward;
            }
            canvasTransform.rotation = Quaternion.LookRotation(fallbackForward, Vector3.up);
        }
    }

    void updatePanelVerticalAlignment(float deltaTime)
    {
        if (!hasPositionedPanel || view == null || headAnchor == null)
        {
            return;
        }

        Transform canvasTransform = view.CanvasTransform;
        if (canvasTransform == null)
        {
            return;
        }

        float targetY = headAnchor.position.y - getVerticalAlignmentOffset();
        Vector3 current = canvasTransform.position;

        if (verticalFollowSpeed <= 0f || deltaTime <= Mathf.Epsilon)
        {
            current.y = targetY;
        }
        else
        {
            float t = 1f - Mathf.Exp(-verticalFollowSpeed * deltaTime);
            current.y = Mathf.Lerp(current.y, targetY, t);
        }

        canvasTransform.position = current;
    }

    void forceVerticalAlignmentToTarget()
    {
        if (view == null || headAnchor == null)
        {
            return;
        }

        Transform canvasTransform = view.CanvasTransform;
        if (canvasTransform == null)
        {
            return;
        }

        Vector3 snapped = canvasTransform.position;
        snapped.y = headAnchor.position.y - getVerticalAlignmentOffset();
        canvasTransform.position = snapped;
    }

    void orientCanvasTowardsUser(float deltaTime)
    {
        if (view == null || headAnchor == null)
        {
            return;
        }

        Transform canvasTransform = view.CanvasTransform;
        if (canvasTransform == null)
        {
            return;
        }

        Vector3 flatDirection = headAnchor.position - canvasTransform.position;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion target = Quaternion.LookRotation(-flatDirection, Vector3.up);

        if (!hasPositionedPanel || orientationFollowSpeed <= 0f || deltaTime <= Mathf.Epsilon)
        {
            canvasTransform.rotation = target;
            return;
        }

        float angle = Quaternion.Angle(canvasTransform.rotation, target);
        float t = Mathf.Clamp01(orientationFollowSpeed * deltaTime);

        if (angle > 135f)
        {
            canvasTransform.rotation = target;
        }
        else
        {
            canvasTransform.rotation = Quaternion.Slerp(canvasTransform.rotation, target, t);
        }
    }

    float getVerticalAlignmentOffset()
    {
        return verticalOffset + getPanelWorldHeight() * PanelTopAlignmentRatio;
    }

    float getPanelWorldHeight()
    {
        return view != null ? view.PanelWorldHeight : 0.9f;
    }

    void completeFlow()
    {
        if (!flowActive)
        {
            return;
        }

        flowActive = false;

        PlayerPrefs.SetInt(consentKey, 1);
        PlayerPrefs.Save();
        onRequirementsAccepted?.Invoke();
        restoreTrackingOrigin();
        if (permissions.HasScenePermission())
        {
            activateEnvironmentDepthManager();
        }

        tearDownWizardUi();
        ensureMasterControl();
        if (loadLocalSceneOnCompletion)
        {
            beginSceneLoadAfterSamplesReady();
            return;
        }

        finalizeFlow();
    }

    void beginSceneLoadAfterSamplesReady()
    {
        if (sceneLoadRoutine != null)
        {
            StopCoroutine(sceneLoadRoutine);
        }

        sceneLoadRoutine = StartCoroutine(loadLocalSceneAfterSamplesReadyAndFinalize());
    }

    IEnumerator loadLocalSceneAfterSamplesReadyAndFinalize()
    {
        yield return loadLocalSceneAfterSamplesReady();
        finalizeFlow();
    }

    IEnumerator loadLocalSceneAfterSamplesReady()
    {
        sampleManager manager = resolveSampleManager();
        while (manager != null && !manager.IsReady)
        {
            yield return null;
        }

        tryLoadLocalScene();
    }

    sampleManager resolveSampleManager()
    {
        if (masterControl.instance == null)
        {
            return null;
        }

        return masterControl.instance.GetComponent<sampleManager>();
    }

    void tearDownWizardUi()
    {
        if (view != null)
        {
            view.contentUpdated -= updateNavigationState;
            view.tearDown();
            view = null;
        }

        hasPositionedPanel = false;
    }

    void finalizeFlow()
    {
        tearDownWizardUi();
        sceneLoadRoutine = null;
        flowActive = false;
        enabled = false;
    }

    void ensureMasterControl()
    {
        if (masterControl.instance != null)
        {
            return;
        }

        if (masterControlPrefab == null)
        {
            Debug.LogWarning("RequirementsManager: MasterControl prefab not assigned; cannot instantiate.");
            return;
        }

        Instantiate(masterControlPrefab);
    }

    void tryLoadLocalScene()
    {
        int localIndex = (int)masterControl.Scenes.Local;
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex == localIndex)
        {
            return;
        }

        SceneManager.LoadScene(localIndex);
    }
}
