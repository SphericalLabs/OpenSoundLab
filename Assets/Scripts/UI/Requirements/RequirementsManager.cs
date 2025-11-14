using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;
using Nobi.UiRoundedCorners;
using Meta.XR.EnvironmentDepth;
#if UNITY_ANDROID
using AndroidPermission = UnityEngine.Android.Permission;
using AndroidPermissionCallbacks = UnityEngine.Android.PermissionCallbacks;
#endif

/// <summary>
/// Runtime-only requirements flow for Meta Quest: documents (EULA, safety, notices) followed by storage permission.
/// Assumes OVRCameraRig is present; falls back to the main camera if not found.
/// </summary>
public class RequirementsManager : MonoBehaviour
{
    [System.Serializable]
    public class RequirementDocument
    {
        public string title = "Document";
        [Tooltip("Relative path under StreamingAssets to the document text file, e.g. Requirements/eula.txt")]
        public string streamingAssetRelativePath;

        string _cachedBody;
        bool _isLoaded;

        public IEnumerator LoadBody(string fallback)
        {
            if (_isLoaded)
            {
                yield break;
            }

            string resolved = string.Empty;

            if (!string.IsNullOrEmpty(streamingAssetRelativePath))
            {
                string fullPath = Path.Combine(Application.streamingAssetsPath, streamingAssetRelativePath);
#if UNITY_ANDROID && !UNITY_EDITOR
                using (var request = UnityWebRequest.Get(fullPath))
                {
                    yield return request.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
                    bool success = request.result == UnityWebRequest.Result.Success;
#else
                    bool success = !request.isNetworkError && !request.isHttpError;
#endif
                    if (success)
                    {
                        resolved = request.downloadHandler.text;
                    }
                    else
                    {
                        Debug.LogWarning($"RequirementsManager: failed to load streaming asset '{fullPath}': {request.error}");
                    }
                }
#else
                try
                {
                    if (File.Exists(fullPath))
                    {
                        resolved = File.ReadAllText(fullPath);
                    }
                    else
                    {
                        Debug.LogWarning($"RequirementsManager: streaming asset missing at '{fullPath}'.");
                    }
                }
                catch (IOException ex)
                {
                    Debug.LogWarning($"RequirementsManager: error reading '{fullPath}': {ex.Message}");
                }
#endif
            }
            else
            {
                Debug.LogWarning($"RequirementsManager: no streaming asset path configured for '{title}'.");
            }

            if (string.IsNullOrEmpty(resolved))
            {
                resolved = fallback;
            }

            _cachedBody = NormalizeLineEndings(resolved);
            _isLoaded = true;
        }

        public string ResolveBody()
        {
            return _cachedBody ?? string.Empty;
        }

        static string NormalizeLineEndings(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            value = value.Replace("\r\n", "\n");
            value = value.Replace('\r', '\n');
            return value;
        }
    }

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

    const float CanvasScale = 0.001f;
    const float TitleHeight = 140f;
    const float ButtonAreaHeight = 140f;
    const float SidePadding = 50f;
    const float BodyPadding = 40f;
    const float ScrollBottomThreshold = 0.01f;
    const float PanelSizeScale = 0.9f;
    const float HintAreaHeight = 70f;
    const float PanelHeightPixels = 900f;
    const float TargetRenderScale = 1.4f;
    const float PanelCornerRadius = 36f;
    const float ButtonCornerRadius = 24f;
    const float ScrollAreaCornerRadius = 28f;
    const float PanelTopAlignmentRatio = 0.25f;
    const string MicrophonePermissionName = "android.permission.RECORD_AUDIO";
    const string ScenePermissionName = OVRPermissionsRequester.ScenePermission;

    Canvas _canvas;
    Transform _canvasTransform;
    RectTransform _panelRect;
    Transform _headAnchor;
    Camera _headCamera;
    TextMeshProUGUI _titleLabel;
    TextMeshProUGUI _bodyLabel;
    Button _nextButton;
    Button _backButton;
    TextMeshProUGUI _nextButtonLabel;
    TextMeshProUGUI _backButtonLabel;
    GameObject _scrollHintRoot;
    ScrollRect _scrollRect;
    RectTransform _contentRect;
    Coroutine _postLayoutRoutine;
    Coroutine _initialPositionRoutine;
    Coroutine _sceneLoadRoutine;
    bool _hasPositionedPanel;
    bool _hasTrackingOriginOverride;
    bool _recenterSubscribed;
    OVRManager.TrackingOrigin _cachedTrackingOrigin = OVRManager.TrackingOrigin.Stage;

    bool _flowActive;
    int _currentStepIndex;
    bool _hasScrolledToEnd;
    bool _waitingForPermissionFocus;
    bool _waitingForMicrophoneResponse;
    bool _microphoneDenied;
    bool _waitingForScenePermissionResponse;
    bool _scenePermissionDenied;

    static bool s_MouseScrollAxisUnavailable;
    static bool s_HasLoggedMissingMouseAxis;

    TMP_FontAsset _uiFont;

    EnvironmentDepthManager _environmentDepthManager;
    bool _depthStateCached;
    bool _depthManagerInitiallyEnabled = true;
    bool _depthManagerSuppressed;
    bool _occlusionModeForcedNone;

#if UNITY_ANDROID
    AndroidPermissionCallbacks _microphoneCallbacks;
    AndroidPermissionCallbacks _scenePermissionCallbacks;
#endif

    enum StepType
    {
        Document,
        ScenePermission,
        Microphone,
        Storage
    }

    int GetDocumentCount()
    {
        return documents != null ? documents.Length : 0;
    }

    StepType GetStepType(int index)
    {
        int docCount = GetDocumentCount();
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

    bool HasMicrophonePermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return AndroidPermission.HasUserAuthorizedPermission(MicrophonePermissionName);
#else
        return true;
#endif
    }

    void RequestMicrophonePermission()
    {
        _waitingForMicrophoneResponse = true;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (HasMicrophonePermission())
        {
            _waitingForMicrophoneResponse = false;
            OnMicrophonePermissionGranted(MicrophonePermissionName);
            return;
        }

        if (_microphoneCallbacks == null)
        {
            _microphoneCallbacks = new AndroidPermissionCallbacks();
            _microphoneCallbacks.PermissionGranted += OnMicrophonePermissionGranted;
            _microphoneCallbacks.PermissionDenied += OnMicrophonePermissionDenied;
            _microphoneCallbacks.PermissionDeniedAndDontAskAgain += OnMicrophonePermissionDontAskAgain;
        }

        _waitingForMicrophoneResponse = true;
        AndroidPermission.RequestUserPermission(MicrophonePermissionName, _microphoneCallbacks);
#else
        OnMicrophonePermissionGranted(MicrophonePermissionName);
#endif
    }

    void OnMicrophonePermissionGranted(string permission)
    {
        if (permission != MicrophonePermissionName)
        {
            return;
        }

        _waitingForMicrophoneResponse = false;
        _microphoneDenied = false;

        if (GetStepType(_currentStepIndex) == StepType.Microphone)
        {
            AdvanceStep();
        }
    }

    void OnMicrophonePermissionDenied(string permission)
    {
        if (permission != MicrophonePermissionName)
        {
            return;
        }

        _waitingForMicrophoneResponse = false;
        _microphoneDenied = true;
        ShowStep(_currentStepIndex);
    }

    void OnMicrophonePermissionDontAskAgain(string permission)
    {
        if (permission != MicrophonePermissionName)
        {
            return;
        }

        _waitingForMicrophoneResponse = false;
        _microphoneDenied = true;
        ShowStep(_currentStepIndex);
    }

    bool HasScenePermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return AndroidPermission.HasUserAuthorizedPermission(ScenePermissionName);
#else
        return true;
#endif
    }

    void RequestScenePermission()
    {
        _waitingForScenePermissionResponse = true;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (HasScenePermission())
        {
            _waitingForScenePermissionResponse = false;
            OnScenePermissionGranted(ScenePermissionName);
            return;
        }

        if (_scenePermissionCallbacks == null)
        {
            _scenePermissionCallbacks = new AndroidPermissionCallbacks();
            _scenePermissionCallbacks.PermissionGranted += OnScenePermissionGranted;
            _scenePermissionCallbacks.PermissionDenied += OnScenePermissionDenied;
            _scenePermissionCallbacks.PermissionDeniedAndDontAskAgain += OnScenePermissionDontAskAgain;
        }

        AndroidPermission.RequestUserPermission(ScenePermissionName, _scenePermissionCallbacks);
#else
        OnScenePermissionGranted(ScenePermissionName);
#endif
    }

    void OnScenePermissionGranted(string permission)
    {
        if (permission != ScenePermissionName)
        {
            return;
        }

        _waitingForScenePermissionResponse = false;
        _scenePermissionDenied = false;
        ActivateEnvironmentDepthManager();

        if (GetStepType(_currentStepIndex) == StepType.ScenePermission)
        {
            AdvanceStep();
        }
    }

    void OnScenePermissionDenied(string permission)
    {
        if (permission != ScenePermissionName)
        {
            return;
        }

        _waitingForScenePermissionResponse = false;
        _scenePermissionDenied = true;
        ShowStep(_currentStepIndex);
    }

    void OnScenePermissionDontAskAgain(string permission)
    {
        if (permission != ScenePermissionName)
        {
            return;
        }

        _waitingForScenePermissionResponse = false;
        _scenePermissionDenied = true;
        ShowStep(_currentStepIndex);
    }

    void Awake()
    {
        PrepareEnvironmentDepthSuppression();
    }

    void PrepareEnvironmentDepthSuppression()
    {
        if (HasScenePermission())
        {
            return;
        }

        var depthManager = ResolveEnvironmentDepthManager();
        if (depthManager == null)
        {
            return;
        }

        if (depthManager.enabled)
        {
            _depthManagerSuppressed = true;
            depthManager.enabled = false;
        }

        if (depthManager.OcclusionShadersMode != OcclusionShadersMode.None)
        {
            _occlusionModeForcedNone = true;
            depthManager.OcclusionShadersMode = OcclusionShadersMode.None;
        }
    }

    EnvironmentDepthManager ResolveEnvironmentDepthManager()
    {
        if (_environmentDepthManager == null)
        {
            _environmentDepthManager = FindObjectOfType<EnvironmentDepthManager>(true);
            CacheDepthManagerState();
        }

        return _environmentDepthManager;
    }

    void CacheDepthManagerState()
    {
        if (_environmentDepthManager == null || _depthStateCached)
        {
            return;
        }

        _depthManagerInitiallyEnabled = _environmentDepthManager.enabled;
        _depthStateCached = true;
    }

    void ActivateEnvironmentDepthManager()
    {
        var depthManager = ResolveEnvironmentDepthManager();
        if (depthManager == null)
        {
            return;
        }

        if (_occlusionModeForcedNone)
        {
            depthManager.OcclusionShadersMode = OcclusionShadersMode.SoftOcclusion;
            _occlusionModeForcedNone = false;
        }

        if (_depthManagerSuppressed && _depthManagerInitiallyEnabled)
        {
            depthManager.enabled = true;
            _depthManagerSuppressed = false;
        }
    }


    void OnEnable()
    {
        SubscribeToRecenterEvent(true);
    }

    IEnumerator Start()
    {
        ApplyRenderResolutionScaling();
        EnsureDefaultDocuments();

        bool consentPresent = PlayerPrefs.GetInt(consentKey, 0) == 1;
        bool hasScenePermission = HasScenePermission();
        bool storageGranted = AndroidStorageAccess.HasManageAllFilesAccess();

        PrepareEnvironmentDepthSuppression();

        if (consentPresent && storageGranted && hasScenePermission)
        {
            EnsureMasterControl();
            if (loadLocalSceneOnCompletion)
            {
                yield return LoadLocalSceneAfterSamplesReady();
            }
            onRequirementsAccepted?.Invoke();
            enabled = false;
            yield break;
        }

        ResolveUserReference();
        PrepareEnvironmentDepthSuppression();
        SubscribeToRecenterEvent(true);
        ApplyWizardTrackingOrigin();
        BuildUi();
        InitializeStartupPosition();

        yield return LoadDocumentBodies();

        if (documents == null || documents.Length == 0)
        {
            Debug.LogWarning("RequirementsManager: no documents configured. The flow still runs but will jump to the permission step.");
        }

        _flowActive = true;

        int startIndex = consentPresent ? GetDocumentCount() : 0;
        if (consentPresent)
        {
            if (hasScenePermission)
            {
                startIndex += 1;
            }

            if (HasMicrophonePermission())
            {
                startIndex += 1;
            }
        }

        ShowStep(startIndex);
    }

    void ApplyRenderResolutionScaling()
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

    IEnumerator LoadDocumentBodies()
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
                yield return doc.LoadBody(missingDocumentFallback);
            }
        }
    }

    void EnsureDefaultDocuments()
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
            EnsureDefaultDocuments();
        }
    }
#endif

    void Update()
    {
        if (!_flowActive)
        {
            return;
        }

        HandleScrollInput();
        HandleButtonShortcuts();
    }

    void LateUpdate()
    {
        if (_canvasTransform == null || _headAnchor == null)
        {
            return;
        }

        UpdatePanelVerticalAlignment(Time.deltaTime);
        OrientCanvasTowardsUser(Time.deltaTime);
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!_flowActive || !hasFocus)
        {
            return;
        }

        if (GetStepType(_currentStepIndex) == StepType.ScenePermission && HasScenePermission())
        {
            ActivateEnvironmentDepthManager();
            AdvanceStep();
            return;
        }

        if (_waitingForPermissionFocus && GetStepType(_currentStepIndex) == StepType.Storage)
        {
            if (AndroidStorageAccess.HasManageAllFilesAccess())
            {
                CompleteFlow();
            }
            else
            {
                _waitingForPermissionFocus = false;
                UpdateNavigationState();
            }
        }

    }

    void HandleButtonShortcuts()
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

        if (nextPressed && _nextButton != null && _nextButton.interactable)
        {
            HandleNext();
        }

        if (backPressed && _backButton != null && _backButton.interactable)
        {
            HandleBack();
        }
    }

    void HandleScrollInput()
    {
        if (_scrollRect == null || GetStepType(_currentStepIndex) != StepType.Document)
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
            if (!s_MouseScrollAxisUnavailable)
            {
                try
                {
                    mouseScroll = Input.GetAxis("Mouse ScrollWheel");
                }
                catch (System.ArgumentException)
                {
                    s_MouseScrollAxisUnavailable = true;
                    if (!s_HasLoggedMissingMouseAxis)
                    {
                        Debug.LogWarning("RequirementsManager: legacy Input axis 'Mouse ScrollWheel' is not configured; falling back to Input.mouseScrollDelta.");
                        s_HasLoggedMissingMouseAxis = true;
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
            float newValue = Mathf.Clamp01(_scrollRect.verticalNormalizedPosition + normalizedDelta);
            _scrollRect.verticalNormalizedPosition = newValue;
        }

        TryMarkScrolledToEnd();
    }

    void HandleNext()
    {
        switch (GetStepType(_currentStepIndex))
        {
            case StepType.Document:
                AdvanceStep();
                break;
            case StepType.ScenePermission:
                if (HasScenePermission())
                {
                    ActivateEnvironmentDepthManager();
                    AdvanceStep();
                }
                else if (_scenePermissionDenied && !_waitingForScenePermissionResponse)
                {
                    AdvanceStep();
                }
                else
                {
                    RequestScenePermission();
                    UpdateNavigationState();
                }
                break;
            case StepType.Microphone:
                if (HasMicrophonePermission())
                {
                    AdvanceStep();
                }
                else if (_microphoneDenied && !_waitingForMicrophoneResponse)
                {
                    AdvanceStep();
                }
                else
                {
                    RequestMicrophonePermission();
                    UpdateNavigationState();
                }
                break;
            case StepType.Storage:
                if (AndroidStorageAccess.HasManageAllFilesAccess())
                {
                    CompleteFlow();
                    return;
                }

                bool launched = AndroidStorageAccess.TryOpenManageAllFilesSettings();
                if (!launched)
                {
                    Debug.LogWarning("RequirementsManager: could not open Manage All Files settings.");
                }
                _waitingForPermissionFocus = true;
                break;
        }
    }

    void HandleBack()
    {
        int previous = _currentStepIndex - 1;
        if (previous < 0)
        {
            return;
        }

        ShowStep(previous);
    }

    void AdvanceStep()
    {
        int nextIndex = _currentStepIndex + 1;
        ShowStep(nextIndex);
    }

    void ShowStep(int index)
    {
        int maxIndex = GetDocumentCount() + 2;
        _currentStepIndex = Mathf.Clamp(index, 0, maxIndex);

        _waitingForPermissionFocus = false;
        _waitingForMicrophoneResponse = false;
        _waitingForScenePermissionResponse = false;

        switch (GetStepType(_currentStepIndex))
        {
            case StepType.Document:
                ShowDocumentStep(_currentStepIndex);
                break;
            case StepType.ScenePermission:
                ShowScenePermissionStep();
                break;
            case StepType.Microphone:
                ShowMicrophoneStep();
                break;
            case StepType.Storage:
                ShowStorageStep();
                break;
        }
    }

    void ShowDocumentStep(int index)
    {
        RequirementDocument doc = documents[index];
        if (doc == null)
        {
            doc = new RequirementDocument();
            Debug.LogWarning($"RequirementsManager: document at index {index} is null.");
        }

        _hasScrolledToEnd = false;
        _titleLabel.text = doc.title;
        SetBodyText(doc.ResolveBody());
        SetNextLabel(agreeLabel);
        UpdateNavigationState();
    }

    void ShowScenePermissionStep()
    {
        if (HasScenePermission())
        {
            ActivateEnvironmentDepthManager();
            AdvanceStep();
            return;
        }

        _titleLabel.text = depthPermissionTitle;
        SetBodyText(_scenePermissionDenied ? depthPermissionDeniedBody : depthPermissionBody);
        _hasScrolledToEnd = true;
        SetNextLabel(agreeLabel);
        UpdateNavigationState();
    }

    void ShowMicrophoneStep()
    {
        if (HasMicrophonePermission())
        {
            AdvanceStep();
            return;
        }

        _titleLabel.text = microphoneTitle;
        SetBodyText(_microphoneDenied ? microphoneDeniedBody : microphoneBody);
        _hasScrolledToEnd = true;
        SetNextLabel(agreeLabel);
        UpdateNavigationState();
    }

    void ShowStorageStep()
    {
        if (AndroidStorageAccess.HasManageAllFilesAccess())
        {
            CompleteFlow();
            return;
        }

        _titleLabel.text = permissionTitle;
        SetBodyText(permissionBody);
        SetNextLabel(openSettingsLabel);
        _hasScrolledToEnd = true;
        UpdateNavigationState();
    }

    void SetBodyText(string body)
    {
        if (_bodyLabel == null || _contentRect == null || _scrollRect == null)
        {
            return;
        }

        _bodyLabel.text = body ?? string.Empty;
        _bodyLabel.margin = new Vector4(BodyPadding, BodyPadding, BodyPadding, BodyPadding);

        ApplyBodyLayout();

        if (isActiveAndEnabled)
        {
            if (_postLayoutRoutine != null)
            {
                StopCoroutine(_postLayoutRoutine);
            }
            _postLayoutRoutine = StartCoroutine(DeferredBodyLayout());
        }
    }

    void ApplyBodyLayout()
    {
        Canvas.ForceUpdateCanvases();
        _bodyLabel.ForceMeshUpdate();

        float preferredHeight = _bodyLabel.preferredHeight + BodyPadding * 2f;
        float viewportHeight = _scrollRect.viewport.rect.height;
        if (viewportHeight <= 0f)
        {
            viewportHeight = preferredHeight;
        }
        float targetHeight = Mathf.Max(preferredHeight, viewportHeight);

        _contentRect.sizeDelta = new Vector2(_contentRect.sizeDelta.x, targetHeight);
        RectTransform textRect = _bodyLabel.rectTransform;
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.sizeDelta = new Vector2(0f, targetHeight);
        textRect.anchoredPosition = Vector2.zero;

        _scrollRect.verticalNormalizedPosition = 1f;
        _scrollRect.velocity = Vector2.zero;
        _hasScrolledToEnd = targetHeight <= viewportHeight + 0.5f;
        UpdateNavigationState();
    }

    IEnumerator DeferredBodyLayout()
    {
        yield return null;
        ApplyBodyLayout();
        TryMarkScrolledToEnd();
        UpdateNavigationState();
        _postLayoutRoutine = null;
    }

    void TryMarkScrolledToEnd()
    {
        if (_hasScrolledToEnd || _scrollRect == null)
        {
            return;
        }

        float viewportHeight = _scrollRect.viewport.rect.height;
        float contentHeight = _scrollRect.content.rect.height;
        if (contentHeight <= viewportHeight + 0.5f)
        {
            _hasScrolledToEnd = true;
        }
        else if (_scrollRect.verticalNormalizedPosition <= ScrollBottomThreshold)
        {
            _hasScrolledToEnd = true;
        }

        if (_hasScrolledToEnd)
        {
            UpdateNavigationState();
        }
    }

    void UpdateNavigationState()
    {
        if (_backButton != null)
        {
            _backButton.interactable = _currentStepIndex > 0;
        }

        if (_nextButton != null)
        {
            switch (GetStepType(_currentStepIndex))
            {
                case StepType.Document:
                    _nextButton.interactable = _hasScrolledToEnd;
                    break;
                case StepType.ScenePermission:
                    _nextButton.interactable = !_waitingForScenePermissionResponse;
                    break;
                case StepType.Microphone:
                    _nextButton.interactable = !_waitingForMicrophoneResponse;
                    break;
                case StepType.Storage:
                    _nextButton.interactable = true;
                    break;
            }
        }

        UpdateScrollHintVisibility();
    }

    void UpdateScrollHintVisibility()
    {
        if (_scrollHintRoot == null)
        {
            return;
        }

        bool shouldShow = GetStepType(_currentStepIndex) == StepType.Document && !_hasScrolledToEnd;
        if (_scrollHintRoot.activeSelf != shouldShow)
        {
            _scrollHintRoot.SetActive(shouldShow);
        }
    }

    void SetNextLabel(string label)
    {
        if (_nextButtonLabel != null)
        {
            _nextButtonLabel.text = AppendHint(label, nextButtonHint);
        }
    }

    static string AppendHint(string label, string hint)
    {
        if (string.IsNullOrEmpty(hint))
        {
            return label;
        }

        return string.Concat(label, hint);
    }

    void BuildUi()
    {
        Transform parent = transform;
        GameObject canvasGo = new GameObject("RequirementsCanvas", typeof(RectTransform));
        canvasGo.layer = _headAnchor != null ? _headAnchor.gameObject.layer : parent.gameObject.layer;
        canvasGo.transform.SetParent(parent, false);

        _canvasTransform = canvasGo.transform;
        _canvasTransform.localScale = Vector3.one * CanvasScale;

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 5000;
        _canvas.worldCamera = _headCamera;
        _canvas.pixelPerfect = true;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;
        scaler.dynamicPixelsPerUnit = 8000f;

        canvasGo.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = _canvas.transform as RectTransform;
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.pivot = new Vector2(0.5f, 0.5f);
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        GameObject panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(canvasRect, false);
        _panelRect = panelGo.AddComponent<RectTransform>();
        _panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        _panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        _panelRect.pivot = new Vector2(0.5f, 0.5f);
        Vector2 panelPixelSize = panelSizeMeters * (PanelSizeScale / CanvasScale);
        panelPixelSize.y = PanelHeightPixels;
        _panelRect.sizeDelta = panelPixelSize;
        _panelRect.anchoredPosition = Vector2.zero;

        Image panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0.02f, 0.02f, 0.02f, 1f);
        var panelRounded = panelGo.AddComponent<ImageWithRoundedCorners>();
        panelRounded.radius = PanelCornerRadius;
        ConfigureRoundedCorners(panelRounded);

        BuildTitle(_panelRect);
        BuildScrollArea(_panelRect);
        BuildButtons(_panelRect);
        BuildScrollHint(_panelRect);

        SetLayerRecursively(canvasGo.transform, canvasGo.layer);
    }

    void InitializeStartupPosition()
    {
        if (startupPositionDelayFrames <= 0)
        {
            ResolveUserReference();
            ApplyWizardTrackingOrigin();
            PositionUiInFrontOfUser();
            return;
        }

        if (_initialPositionRoutine != null)
        {
            StopCoroutine(_initialPositionRoutine);
        }

        _initialPositionRoutine = StartCoroutine(DelayInitialPosition());
    }

    IEnumerator DelayInitialPosition()
    {
        int remaining = Mathf.Max(0, startupPositionDelayFrames);
        while (remaining > 0)
        {
            remaining--;
            yield return null;
        }

        ResolveUserReference();
        ApplyWizardTrackingOrigin();
        PositionUiInFrontOfUser();
        _initialPositionRoutine = null;
    }

    void ApplyWizardTrackingOrigin()
    {
#if UNITY_ANDROID || UNITY_EDITOR
        OVRManager manager = OVRManager.instance;
        if (manager == null)
        {
            return;
        }

        if (!_hasTrackingOriginOverride)
        {
            _cachedTrackingOrigin = manager.trackingOriginType;
            _hasTrackingOriginOverride = true;
        }

        if (manager.trackingOriginType != OVRManager.TrackingOrigin.EyeLevel)
        {
            manager.trackingOriginType = OVRManager.TrackingOrigin.EyeLevel;
        }
#endif
    }

    void RestoreTrackingOrigin()
    {
#if UNITY_ANDROID || UNITY_EDITOR
        if (!_hasTrackingOriginOverride)
        {
            return;
        }

        OVRManager manager = OVRManager.instance;
        if (manager == null)
        {
            return;
        }

        manager.trackingOriginType = _cachedTrackingOrigin;
        _hasTrackingOriginOverride = false;
#endif
    }

    void BuildScrollHint(RectTransform parent)
    {
        GameObject hintGo = new GameObject("ScrollHint");
        hintGo.transform.SetParent(parent, false);
        RectTransform rect = hintGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, SidePadding * 0.35f);
        rect.sizeDelta = new Vector2(0f, HintAreaHeight);

        TextMeshProUGUI hintLabel = hintGo.AddComponent<TextMeshProUGUI>();
        hintLabel.fontSize = 28f;
        hintLabel.alignment = TextAlignmentOptions.Center;
        hintLabel.color = new Color(0.8f, 0.8f, 0.8f, 0.95f);
        hintLabel.text = "Scroll to the end via joystick before agreeing";
        ApplyUiFont(hintLabel);
        _scrollHintRoot = hintGo;
        _scrollHintRoot.SetActive(false);
    }

    void ResolveUserReference()
    {
        OVRCameraRig ovrCameraRig = FindObjectOfType<OVRCameraRig>();

        if (_environmentDepthManager == null && ovrCameraRig != null)
        {
            _environmentDepthManager = ovrCameraRig.GetComponent<EnvironmentDepthManager>();
            CacheDepthManagerState();
        }

        if (_headAnchor == null && ovrCameraRig != null && ovrCameraRig.centerEyeAnchor != null)
        {
            _headAnchor = ovrCameraRig.centerEyeAnchor;
        }

        if (_headCamera == null)
        {
            if (ovrCameraRig != null)
            {
                _headCamera = ovrCameraRig.GetComponentInChildren<Camera>();
            }

            if (_headCamera == null)
            {
                _headCamera = Camera.main;
            }

            if (_headCamera == null)
            {
                _headCamera = FindObjectOfType<Camera>();
            }
        }

        if (_headAnchor == null && _headCamera != null)
        {
            _headAnchor = _headCamera.transform;
        }

        if (_headAnchor == null)
        {
            Debug.LogWarning("RequirementsManager: Cannot locate a head anchor; world-space UI will spawn at the origin.", this);
        }
    }

    void SubscribeToRecenterEvent(bool subscribe)
    {
#if UNITY_ANDROID || UNITY_EDITOR
        OVRDisplay display = OVRManager.display;
        if (display == null)
        {
            return;
        }

        if (subscribe && !_recenterSubscribed)
        {
            display.RecenteredPose += OnDisplayRecenteredPose;
            _recenterSubscribed = true;
        }
        else if (!subscribe && _recenterSubscribed)
        {
            display.RecenteredPose -= OnDisplayRecenteredPose;
            _recenterSubscribed = false;
        }
#endif
    }

#if UNITY_ANDROID || UNITY_EDITOR
    void OnDisplayRecenteredPose()
    {
        if (_canvasTransform == null || !isActiveAndEnabled)
        {
            return;
        }

        ResolveUserReference();
        PositionUiInFrontOfUser();
        ForceVerticalAlignmentToTarget();
    }
#endif

    void PositionUiInFrontOfUser()
    {
        if (_canvasTransform == null)
        {
            return;
        }

        if (_headAnchor == null)
        {
            _canvasTransform.position = Vector3.forward * panelDistance;
            _canvasTransform.rotation = Quaternion.identity;
            _hasPositionedPanel = false;
            return;
        }

        float verticalShift = GetVerticalAlignmentOffset();
        Vector3 targetPosition = _headAnchor.position + _headAnchor.forward * panelDistance;
        targetPosition.y -= verticalShift;
        _canvasTransform.position = targetPosition;
        _hasPositionedPanel = true;

        Vector3 lookDirection = _headAnchor.position - _canvasTransform.position;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            _canvasTransform.rotation = Quaternion.LookRotation(-lookDirection, Vector3.up);
        }
        else
        {
            Vector3 fallbackForward = -_headAnchor.forward;
            fallbackForward.y = 0f;
            if (fallbackForward.sqrMagnitude < 0.0001f)
            {
                fallbackForward = Vector3.forward;
            }
            _canvasTransform.rotation = Quaternion.LookRotation(fallbackForward, Vector3.up);
        }
    }

    void UpdatePanelVerticalAlignment(float deltaTime)
    {
        if (!_hasPositionedPanel || _canvasTransform == null || _headAnchor == null)
        {
            return;
        }

        float targetY = _headAnchor.position.y - GetVerticalAlignmentOffset();
        Vector3 current = _canvasTransform.position;

        if (verticalFollowSpeed <= 0f || deltaTime <= Mathf.Epsilon)
        {
            current.y = targetY;
        }
        else
        {
            float t = 1f - Mathf.Exp(-verticalFollowSpeed * deltaTime);
            current.y = Mathf.Lerp(current.y, targetY, t);
        }

        _canvasTransform.position = current;
    }

    float GetVerticalAlignmentOffset()
    {
        return verticalOffset + GetPanelWorldHeight() * PanelTopAlignmentRatio;
    }

    float GetPanelWorldHeight()
    {
        return PanelHeightPixels * CanvasScale;
    }

    void ForceVerticalAlignmentToTarget()
    {
        if (_canvasTransform == null || _headAnchor == null)
        {
            return;
        }

        Vector3 snapped = _canvasTransform.position;
        snapped.y = _headAnchor.position.y - GetVerticalAlignmentOffset();
        _canvasTransform.position = snapped;
    }

    void OrientCanvasTowardsUser(float deltaTime)
    {
        if (_canvasTransform == null || _headAnchor == null)
        {
            return;
        }

        Vector3 flatDirection = _headAnchor.position - _canvasTransform.position;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion target = Quaternion.LookRotation(-flatDirection, Vector3.up);

        if (!_hasPositionedPanel || orientationFollowSpeed <= 0f || deltaTime <= Mathf.Epsilon)
        {
            _canvasTransform.rotation = target;
            return;
        }

        float angle = Quaternion.Angle(_canvasTransform.rotation, target);
        float t = Mathf.Clamp01(orientationFollowSpeed * deltaTime);

        if (angle > 135f)
        {
            _canvasTransform.rotation = target;
        }
        else
        {
            _canvasTransform.rotation = Quaternion.Slerp(_canvasTransform.rotation, target, t);
        }
    }

    static void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null)
        {
            return;
        }

        root.gameObject.layer = layer;
        foreach (Transform child in root)
        {
            SetLayerRecursively(child, layer);
        }
    }

    void BuildTitle(RectTransform parent)
    {
        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(parent, false);
        RectTransform rect = titleGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, TitleHeight);
        rect.anchoredPosition = Vector2.zero;

        _titleLabel = titleGo.AddComponent<TextMeshProUGUI>();
        _titleLabel.fontSize = 48f;
        _titleLabel.alignment = TextAlignmentOptions.Center;
        _titleLabel.enableWordWrapping = false;
        _titleLabel.color = Color.white;
        ApplyUiFont(_titleLabel);
    }

    void BuildScrollArea(RectTransform parent)
    {
        GameObject scrollRoot = new GameObject("ScrollArea");
        scrollRoot.transform.SetParent(parent, false);
        RectTransform rect = scrollRoot.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(SidePadding, SidePadding + ButtonAreaHeight + HintAreaHeight);
        rect.offsetMax = new Vector2(-SidePadding, -SidePadding - TitleHeight);

        Image background = scrollRoot.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.05f);
        var scrollRounded = scrollRoot.AddComponent<ImageWithRoundedCorners>();
        scrollRounded.radius = ScrollAreaCornerRadius;
        ConfigureRoundedCorners(scrollRounded);

        _scrollRect = scrollRoot.AddComponent<ScrollRect>();
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.inertia = false;
        _scrollRect.scrollSensitivity = 0f;

        GameObject viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(scrollRoot.transform, false);
        RectTransform viewportRect = viewportGo.AddComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0f, 0f);
        viewportRect.anchorMax = new Vector2(1f, 1f);
        viewportRect.pivot = new Vector2(0.5f, 0.5f);
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        Image viewportImage = viewportGo.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0f);
        viewportGo.AddComponent<RectMask2D>();

        GameObject contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        _contentRect = contentGo.AddComponent<RectTransform>();
        _contentRect.anchorMin = new Vector2(0f, 1f);
        _contentRect.anchorMax = new Vector2(1f, 1f);
        _contentRect.pivot = new Vector2(0.5f, 1f);
        _contentRect.anchoredPosition = Vector2.zero;
        _contentRect.sizeDelta = new Vector2(0f, 400f);

        GameObject textGo = new GameObject("Body");
        textGo.transform.SetParent(contentGo.transform, false);
        _bodyLabel = textGo.AddComponent<TextMeshProUGUI>();
        _bodyLabel.fontSize = 32f;
        _bodyLabel.alignment = TextAlignmentOptions.TopLeft;
        _bodyLabel.enableWordWrapping = true;
        _bodyLabel.color = Color.white;
        ApplyUiFont(_bodyLabel);

        _scrollRect.viewport = viewportRect;
        _scrollRect.content = _contentRect;
    }

    void BuildButtons(RectTransform parent)
    {
        Color backNormal = new Color(0.25f, 0.25f, 0.28f, 1f);
        Color backHighlight = new Color(0.35f, 0.35f, 0.38f, 1f);
        Color nextNormal = new Color(0.1f, 0.6f, 0.18f, 1f);
        Color nextHighlight = new Color(0.18f, 0.72f, 0.28f, 1f);
        Color nextPressed = new Color(0.08f, 0.45f, 0.14f, 1f);
        Color disabledGray = new Color(0.32f, 0.32f, 0.34f, 1f);

        float buttonBaseY = SidePadding + HintAreaHeight;

        _backButton = CreateButton(parent, "BackButton", new Vector2(SidePadding, buttonBaseY), TextAlignmentOptions.Center, false, backNormal, backHighlight, backHighlight, backNormal);
        _backButton.onClick.AddListener(HandleBack);
        _backButtonLabel = _backButton.GetComponentInChildren<TextMeshProUGUI>();
        if (_backButtonLabel != null)
        {
            ApplyUiFont(_backButtonLabel);
            _backButtonLabel.text = AppendHint(backLabel, backButtonHint);
        }
        _backButton.interactable = false;

        _nextButton = CreateButton(parent, "NextButton", new Vector2(-SidePadding, buttonBaseY), TextAlignmentOptions.Center, true, nextNormal, nextHighlight, nextPressed, disabledGray);
        _nextButton.onClick.AddListener(HandleNext);
        _nextButtonLabel = _nextButton.GetComponentInChildren<TextMeshProUGUI>();
        if (_nextButtonLabel != null)
        {
            ApplyUiFont(_nextButtonLabel);
            _nextButtonLabel.text = AppendHint(agreeLabel, nextButtonHint);
        }

        var colors = _nextButton.colors;
        colors.disabledColor = disabledGray;
        colors.normalColor = nextNormal;
        colors.highlightedColor = nextHighlight;
        colors.pressedColor = nextPressed;
        colors.colorMultiplier = 1f;
        _nextButton.colors = colors;
    }

    Button CreateButton(RectTransform parent, string name, Vector2 anchoredPosition, TextAlignmentOptions alignment, bool anchorRight, Color normalColor, Color highlightColor, Color pressedColor, Color disabledColor)
    {
        GameObject buttonGo = new GameObject(name);
        buttonGo.transform.SetParent(parent, false);
        RectTransform rect = buttonGo.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280f, 110f);
        rect.anchorMin = new Vector2(anchorRight ? 1f : 0f, 0f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(anchorRight ? 1f : 0f, 0f);
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonGo.AddComponent<Image>();
        image.color = normalColor;
        var rounded = buttonGo.AddComponent<ImageWithRoundedCorners>();
        rounded.radius = ButtonCornerRadius;
        ConfigureRoundedCorners(rounded);

        Button button = buttonGo.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = highlightColor;
        colors.pressedColor = pressedColor;
        colors.disabledColor = disabledColor;
        colors.selectedColor = highlightColor;
        colors.colorMultiplier = 1f;
        button.colors = colors;

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(buttonGo.transform, false);
        RectTransform labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.offsetMin = new Vector2(10f, 10f);
        labelRect.offsetMax = new Vector2(-10f, -10f);

        TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
        label.fontSize = 32f;
        label.alignment = alignment;
        label.color = Color.white;
        label.text = name;
        ApplyUiFont(label);

        return button;
    }

    void ApplyUiFont(TextMeshProUGUI label)
    {
        if (label == null)
        {
            return;
        }

        TMP_FontAsset font = ResolveUiFont();
        if (font != null)
        {
            label.font = font;
        }
    }

    void ConfigureRoundedCorners(ImageWithRoundedCorners rounded)
    {
        if (rounded == null)
        {
            return;
        }

        if (roundedCornersShader != null)
        {
            rounded.SetShaderOverride(roundedCornersShader);
        }

        rounded.Validate();
        rounded.Refresh();
    }

    TMP_FontAsset ResolveUiFont()
    {
        if (_uiFont == null)
        {
            _uiFont = UIFontProvider.GetFont();
        }

        return _uiFont;
    }

    void CompleteFlow()
    {
        if (!_flowActive)
        {
            return;
        }

        _flowActive = false;

        PlayerPrefs.SetInt(consentKey, 1);
        PlayerPrefs.Save();
        onRequirementsAccepted?.Invoke();
        RestoreTrackingOrigin();
        if (HasScenePermission())
        {
            ActivateEnvironmentDepthManager();
        }

        TearDownWizardUi();
        EnsureMasterControl();
        if (loadLocalSceneOnCompletion)
        {
            BeginSceneLoadAfterSamplesReady();
            return;
        }

        FinalizeFlow();
    }

    void BeginSceneLoadAfterSamplesReady()
    {
        if (_sceneLoadRoutine != null)
        {
            StopCoroutine(_sceneLoadRoutine);
        }

        _sceneLoadRoutine = StartCoroutine(LoadLocalSceneAfterSamplesReadyAndFinalize());
    }

    IEnumerator LoadLocalSceneAfterSamplesReadyAndFinalize()
    {
        yield return LoadLocalSceneAfterSamplesReady();
        FinalizeFlow();
    }

    IEnumerator LoadLocalSceneAfterSamplesReady()
    {
        sampleManager manager = ResolveSampleManager();
        while (manager != null && !manager.IsReady)
        {
            yield return null;
        }

        TryLoadLocalScene();
    }

    sampleManager ResolveSampleManager()
    {
        if (masterControl.instance == null)
        {
            return null;
        }

        return masterControl.instance.GetComponent<sampleManager>();
    }

    void TearDownWizardUi()
    {
        if (_canvas != null)
        {
            Destroy(_canvas.gameObject);
        }

        _canvas = null;
        _canvasTransform = null;
        _panelRect = null;
        _titleLabel = null;
        _bodyLabel = null;
        _nextButton = null;
        _backButton = null;
        _nextButtonLabel = null;
        _backButtonLabel = null;
        _scrollRect = null;
        _contentRect = null;
        _scrollHintRoot = null;
    }

    void FinalizeFlow()
    {
        TearDownWizardUi();
        _sceneLoadRoutine = null;
        _flowActive = false;
        enabled = false;
    }

    void OnDisable()
    {
        if (_postLayoutRoutine != null)
        {
            StopCoroutine(_postLayoutRoutine);
            _postLayoutRoutine = null;
        }
        if (_initialPositionRoutine != null)
        {
            StopCoroutine(_initialPositionRoutine);
            _initialPositionRoutine = null;
        }
        if (_sceneLoadRoutine != null)
        {
            StopCoroutine(_sceneLoadRoutine);
            _sceneLoadRoutine = null;
        }
        SubscribeToRecenterEvent(false);
        RestoreTrackingOrigin();
    }

    void EnsureMasterControl()
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

    void TryLoadLocalScene()
    {
        int localIndex = (int)masterControl.Scenes.Local;
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex == localIndex)
        {
            return;
        }

        SceneManager.LoadScene(localIndex);
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    void OnDestroy()
    {
        if (_microphoneCallbacks != null)
        {
            _microphoneCallbacks.PermissionGranted -= OnMicrophonePermissionGranted;
            _microphoneCallbacks.PermissionDenied -= OnMicrophonePermissionDenied;
            _microphoneCallbacks.PermissionDeniedAndDontAskAgain -= OnMicrophonePermissionDontAskAgain;
        }

        if (_scenePermissionCallbacks != null)
        {
            _scenePermissionCallbacks.PermissionGranted -= OnScenePermissionGranted;
            _scenePermissionCallbacks.PermissionDenied -= OnScenePermissionDenied;
            _scenePermissionCallbacks.PermissionDeniedAndDontAskAgain -= OnScenePermissionDontAskAgain;
        }
    }
#endif
}
