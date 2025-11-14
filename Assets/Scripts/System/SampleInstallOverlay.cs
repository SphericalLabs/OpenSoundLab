using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SampleInstallOverlay : MonoBehaviour
{
    const float CanvasScale = 0.001f;
    const float PanelWidthPixels = 900f;
    const float PanelHeightPixels = 320f;
    const float PanelDistanceMeters = 1.55f;
    const float VerticalOffsetMeters = -0.15f;
    const float OrientationFollowSpeed = 2.75f;
    const float VerticalFollowSpeed = 2.25f;
    const float FadeDuration = 0.35f;
    const float MinimumVisibleProgress = 0.04f;

    Canvas _canvas;
    CanvasGroup _canvasGroup;
    Image _progressFill;
    Coroutine _fadeRoutine;
    Transform _headAnchor;
    Camera _headCamera;
    bool _hasPositionedPanel;
    bool _hasShownActualProgress;
    int _panelLayer;
    static Sprite _solidSprite;

    public static SampleInstallOverlay CreateAndAttach()
    {
        Transform anchor = ResolveAnchor();
        if (anchor == null)
        {
            Debug.LogWarning("SampleInstallOverlay: no CenterEyeAnchor or main camera found. Overlay will not be shown.");
            return null;
        }

        GameObject root = new GameObject("SampleInstallOverlay", typeof(RectTransform));
        root.layer = anchor.gameObject.layer;

        var overlay = root.AddComponent<SampleInstallOverlay>();
        overlay.Initialize(anchor);
        return overlay;
    }

    void Initialize(Transform anchor)
    {
        _headAnchor = anchor;
        _headCamera = ResolveCamera(anchor);
        _panelLayer = anchor != null ? anchor.gameObject.layer : gameObject.layer;

        transform.localScale = Vector3.one * CanvasScale;

        BuildUi();
        SetLayerRecursively(transform, _panelLayer);
        UpdateFollower(0f, true);
    }

    public void CompleteAndHide()
    {
        if (_progressFill != null)
        {
            _progressFill.fillAmount = 1f;
        }

        if (!gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }
        _fadeRoutine = StartCoroutine(FadeAndDestroy());
    }

    void Update()
    {
        if (_headAnchor == null)
        {
            _headAnchor = ResolveAnchor();
            if (_headAnchor != null)
            {
                _panelLayer = _headAnchor.gameObject.layer;
                SetLayerRecursively(transform, _panelLayer);
            }
        }

        if (_headCamera == null && _headAnchor != null)
        {
            _headCamera = ResolveCamera(_headAnchor);
            if (_canvas != null)
            {
                _canvas.worldCamera = _headCamera;
            }
        }

        UpdateFollower(Time.deltaTime, false);
    }

    void BuildUi()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 5200;
        _canvas.worldCamera = _headCamera;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 12000f;

        gameObject.AddComponent<GraphicRaycaster>();

        var rectTransform = (RectTransform)transform;
        rectTransform.sizeDelta = new Vector2(PanelWidthPixels, PanelHeightPixels);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;

        GameObject panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = panel.AddComponent<Image>();
        panelImage.sprite = GetSolidSprite();
        panelImage.type = Image.Type.Sliced;
        panelImage.color = new Color(0.02f, 0.02f, 0.02f, 0.96f);

        BuildTitle(panelRect);
        BuildProgressBar(panelRect);

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }
        _fadeRoutine = StartCoroutine(FadeCanvas(1f, FadeDuration));
    }

    void BuildTitle(RectTransform parent)
    {
        GameObject titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(parent, false);
        var rect = titleGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, 140f);
        rect.anchoredPosition = Vector2.zero;

        var titleLabel = titleGo.AddComponent<TextMeshProUGUI>();
        titleLabel.text = "Preparing the sample library";
        titleLabel.fontSize = 52f;
        titleLabel.alignment = TextAlignmentOptions.Center;
        titleLabel.color = Color.white;
        titleLabel.enableWordWrapping = false;

        GameObject bodyGo = new GameObject("Body", typeof(RectTransform));
        bodyGo.transform.SetParent(parent, false);
        var bodyRect = bodyGo.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.12f, 1f);
        bodyRect.anchorMax = new Vector2(0.88f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.sizeDelta = new Vector2(0f, 120f);
        bodyRect.anchoredPosition = new Vector2(0f, -120f);

        var bodyLabel = bodyGo.AddComponent<TextMeshProUGUI>();
        bodyLabel.text = "Installing the bundled samples... Please keep the headset on.";
        bodyLabel.fontSize = 34f;
        bodyLabel.alignment = TextAlignmentOptions.Center;
        bodyLabel.color = new Color(0.8f, 0.8f, 0.8f, 0.95f);
        bodyLabel.enableWordWrapping = true;
    }

    public void SetProgress(float normalized)
    {
        if (_progressFill == null)
        {
            return;
        }

        float clamped = Mathf.Clamp01(normalized);
        if (!_hasShownActualProgress)
        {
            if (clamped >= MinimumVisibleProgress || Mathf.Approximately(clamped, 1f))
            {
                _hasShownActualProgress = true;
            }
            else
            {
                clamped = Mathf.Max(clamped, MinimumVisibleProgress);
            }
        }

        _progressFill.fillAmount = clamped;
    }

    void BuildProgressBar(RectTransform parent)
    {
        GameObject barRoot = new GameObject("ProgressBar", typeof(RectTransform));
        barRoot.transform.SetParent(parent, false);
        var barRect = barRoot.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.12f, 1f);
        barRect.anchorMax = new Vector2(0.88f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.sizeDelta = new Vector2(0f, 70f);
        barRect.anchoredPosition = new Vector2(0f, -240f);

        var barBackground = barRoot.AddComponent<Image>();
        barBackground.sprite = GetSolidSprite();
        barBackground.type = Image.Type.Sliced;
        barBackground.color = new Color(1f, 1f, 1f, 0.05f);

        GameObject fillGo = new GameObject("Fill", typeof(RectTransform));
        fillGo.transform.SetParent(barRoot.transform, false);
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(6f, 6f);
        fillRect.offsetMax = new Vector2(-6f, -6f);

        _progressFill = fillGo.AddComponent<Image>();
        _progressFill.sprite = GetSolidSprite();
        _progressFill.type = Image.Type.Filled;
        _progressFill.fillMethod = Image.FillMethod.Horizontal;
        _progressFill.fillOrigin = 0;
        _progressFill.fillAmount = 0f;
        _progressFill.color = new Color(0.1f, 0.6f, 0.18f, 1f);
    }

    IEnumerator FadeCanvas(float targetAlpha, float duration)
    {
        float startAlpha = _canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
        _fadeRoutine = null;
    }

    IEnumerator FadeAndDestroy()
    {
        yield return FadeCanvas(0f, FadeDuration);
        Destroy(gameObject);
    }

    void UpdateFollower(float deltaTime, bool forceSnap)
    {
        Vector3 targetPosition = CalculateTargetPosition();
        bool snapTranslation = forceSnap || !_hasPositionedPanel || VerticalFollowSpeed <= Mathf.Epsilon || deltaTime <= Mathf.Epsilon;

        if (snapTranslation)
        {
            transform.position = targetPosition;
        }
        else
        {
            float t = 1f - Mathf.Exp(-VerticalFollowSpeed * deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPosition, t);
        }

        Quaternion targetRotation = CalculateTargetRotation(transform.position);
        bool snapRotation = forceSnap || !_hasPositionedPanel || OrientationFollowSpeed <= Mathf.Epsilon || deltaTime <= Mathf.Epsilon;
        if (snapRotation)
        {
            transform.rotation = targetRotation;
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Mathf.Clamp01(OrientationFollowSpeed * deltaTime));
        }

        _hasPositionedPanel = true;
    }

    Vector3 CalculateTargetPosition()
    {
        if (_headAnchor == null)
        {
            return new Vector3(0f, VerticalOffsetMeters, PanelDistanceMeters);
        }

        Vector3 forward = _headAnchor.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }
        forward.Normalize();

        Vector3 target = _headAnchor.position + forward * PanelDistanceMeters;
        target.y += VerticalOffsetMeters;
        return target;
    }

    Quaternion CalculateTargetRotation(Vector3 panelPosition)
    {
        if (_headAnchor == null)
        {
            return Quaternion.LookRotation(Vector3.forward, Vector3.up);
        }

        Vector3 lookDirection = _headAnchor.position - panelPosition;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            return Quaternion.LookRotation(Vector3.forward, Vector3.up);
        }

        return Quaternion.LookRotation(-lookDirection, Vector3.up);
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

    static Transform ResolveAnchor()
    {
        OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
        if (rig != null)
        {
            if (rig.centerEyeAnchor != null)
            {
                return rig.centerEyeAnchor;
            }

            return rig.transform;
        }

        if (Camera.main != null)
        {
            return Camera.main.transform;
        }

        return null;
    }

    static Camera ResolveCamera(Transform anchor)
    {
        if (anchor == null)
        {
            return Camera.main;
        }

        Camera camera = anchor.GetComponentInChildren<Camera>();
        if (camera != null)
        {
            return camera;
        }

        return Camera.main;
    }

    static Sprite GetSolidSprite()
    {
        if (_solidSprite == null)
        {
            Texture2D source = Texture2D.whiteTexture;
            _solidSprite = Sprite.Create(source, new Rect(0f, 0f, source.width, source.height), new Vector2(0.5f, 0.5f), 100f);
        }

        return _solidSprite;
    }

    void OnDestroy()
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }
    }
}
