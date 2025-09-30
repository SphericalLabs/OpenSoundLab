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
    const float FadeDuration = 0.35f;

    Canvas _canvas;
    CanvasGroup _canvasGroup;
    Image _progressFill;
    Coroutine _progressRoutine;
    Coroutine _fadeRoutine;
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
        root.transform.SetParent(anchor, false);
        root.transform.localPosition = new Vector3(0f, VerticalOffsetMeters, PanelDistanceMeters);
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one * CanvasScale;

        var overlay = root.AddComponent<SampleInstallOverlay>();
        overlay.BuildUi(anchor);
        return overlay;
    }

    public void BeginFakeProgress(float durationSeconds)
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (_progressRoutine != null)
        {
            StopCoroutine(_progressRoutine);
        }
        _progressRoutine = StartCoroutine(AnimateProgress(durationSeconds));
    }

    public void CompleteAndHide()
    {
        if (_progressRoutine != null)
        {
            StopCoroutine(_progressRoutine);
            _progressRoutine = null;
        }

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

    void BuildUi(Transform anchor)
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 5200;
        _canvas.worldCamera = ResolveCamera(anchor);

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
        bodyRect.anchoredPosition = new Vector2(0f, -170f);

        var bodyLabel = bodyGo.AddComponent<TextMeshProUGUI>();
        bodyLabel.text = "We are installing the bundled samples. Please keep the headset on.";
        bodyLabel.fontSize = 34f;
        bodyLabel.alignment = TextAlignmentOptions.Center;
        bodyLabel.color = new Color(0.8f, 0.8f, 0.8f, 0.95f);
        bodyLabel.enableWordWrapping = true;
    }

    void BuildProgressBar(RectTransform parent)
    {
        GameObject barRoot = new GameObject("ProgressBar", typeof(RectTransform));
        barRoot.transform.SetParent(parent, false);
        var barRect = barRoot.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.12f, 0f);
        barRect.anchorMax = new Vector2(0.88f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.sizeDelta = new Vector2(0f, 70f);
        barRect.anchoredPosition = new Vector2(0f, 35f);

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

    IEnumerator AnimateProgress(float durationSeconds)
    {
        float elapsed = 0f;
        while (elapsed < durationSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / durationSeconds);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            if (_progressFill != null)
            {
                _progressFill.fillAmount = eased;
            }
            yield return null;
        }

        if (_progressFill != null)
        {
            _progressFill.fillAmount = 1f;
        }
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
        if (_progressRoutine != null)
        {
            StopCoroutine(_progressRoutine);
            _progressRoutine = null;
        }

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }
    }
}
