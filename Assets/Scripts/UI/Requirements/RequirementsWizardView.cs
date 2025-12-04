using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Nobi.UiRoundedCorners;

/// <summary>
/// Runtime-built world-space UI panel for the requirements wizard.
/// </summary>
public class RequirementsWizardView
{
    const float CanvasScale = 0.001f;
    const float PanelSizeScale = 0.9f;
    const float PanelHeightPixels = 900f;
    const float TitleHeight = 140f;
    const float ButtonAreaHeight = 140f;
    const float HintAreaHeight = 70f;
    const float SidePadding = 50f;
    const float BodyPadding = 40f;
    const float PanelCornerRadius = 36f;
    const float ButtonCornerRadius = 24f;
    const float ScrollAreaCornerRadius = 28f;
    const float ScrollBottomThreshold = 0.05f;

    readonly MonoBehaviour owner;
    readonly Vector2 panelSizeMeters;
    readonly Shader roundedCornersShader;
    readonly string nextButtonHint;
    readonly string backButtonHint;

    Canvas canvas;
    Transform canvasTransform;
    RectTransform panelRect;
    TextMeshProUGUI titleLabel;
    TextMeshProUGUI bodyLabel;
    Button nextButton;
    Button backButton;
    TextMeshProUGUI nextButtonLabel;
    TextMeshProUGUI backButtonLabel;
    GameObject scrollHintRoot;
    ScrollRect scrollRect;
    RectTransform contentRect;
    TMP_FontAsset uiFont;
    Coroutine postLayoutRoutine;
    bool hasScrolledToEnd;

    public event Action contentUpdated;

    public RequirementsWizardView(MonoBehaviour owner, Vector2 panelSizeMeters, Shader roundedCornersShader, string nextButtonHint, string backButtonHint)
    {
        this.owner = owner;
        this.panelSizeMeters = panelSizeMeters;
        this.roundedCornersShader = roundedCornersShader;
        this.nextButtonHint = nextButtonHint;
        this.backButtonHint = backButtonHint;
    }

    public Transform CanvasTransform => canvasTransform;
    public RectTransform PanelRect => panelRect;
    public ScrollRect ScrollRect => scrollRect;
    public bool HasScrolledToEnd => hasScrolledToEnd;
    public float PanelWorldHeight => PanelHeightPixels * CanvasScale;

    public void build(Transform parent, Camera worldCamera, Transform headAnchor, UnityAction onNext, UnityAction onBack)
    {
        GameObject canvasGo = new GameObject("RequirementsCanvas", typeof(RectTransform));
        canvasGo.layer = headAnchor != null ? headAnchor.gameObject.layer : parent.gameObject.layer;
        canvasGo.transform.SetParent(parent, false);

        canvasTransform = canvasGo.transform;
        canvasTransform.localScale = Vector3.one * CanvasScale;

        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5000;
        canvas.worldCamera = worldCamera;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;
        scaler.dynamicPixelsPerUnit = 8000f;

        canvasGo.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvas.transform as RectTransform;
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.pivot = new Vector2(0.5f, 0.5f);
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        GameObject panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(canvasRect, false);
        panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        Vector2 panelPixelSize = panelSizeMeters * (PanelSizeScale / CanvasScale);
        panelPixelSize.y = PanelHeightPixels;
        panelRect.sizeDelta = panelPixelSize;
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0.02f, 0.02f, 0.02f, 1f);
        var panelRounded = panelGo.AddComponent<ImageWithRoundedCorners>();
        panelRounded.radius = PanelCornerRadius;
        configureRoundedCorners(panelRounded);

        buildTitle(panelRect);
        buildScrollArea(panelRect);
        buildButtons(panelRect, onNext, onBack);
        buildScrollHint(panelRect);

        setNextLabel("Agree");
        setBackLabel("Back");

        setLayerRecursively(canvasGo.transform, canvasGo.layer);
    }

    public void tearDown()
    {
        if (canvas != null)
        {
            UnityEngine.Object.Destroy(canvas.gameObject);
        }

        canvas = null;
        canvasTransform = null;
        panelRect = null;
        titleLabel = null;
        bodyLabel = null;
        nextButton = null;
        backButton = null;
        nextButtonLabel = null;
        backButtonLabel = null;
        scrollRect = null;
        contentRect = null;
        scrollHintRoot = null;
        hasScrolledToEnd = false;

        if (postLayoutRoutine != null && owner != null)
        {
            owner.StopCoroutine(postLayoutRoutine);
            postLayoutRoutine = null;
        }
    }

    public void setTitle(string value)
    {
        if (titleLabel != null)
        {
            titleLabel.text = value ?? string.Empty;
        }
    }

    public void setBody(string body)
    {
        if (bodyLabel == null || contentRect == null || scrollRect == null)
        {
            return;
        }

        bodyLabel.text = body ?? string.Empty;
        bodyLabel.margin = new Vector4(BodyPadding, BodyPadding, BodyPadding, BodyPadding);

        applyBodyLayout();

        if (owner != null && owner.isActiveAndEnabled)
        {
            if (postLayoutRoutine != null)
            {
                owner.StopCoroutine(postLayoutRoutine);
            }
            postLayoutRoutine = owner.StartCoroutine(deferredBodyLayout());
        }
    }

    public void resetScrollState()
    {
        hasScrolledToEnd = false;
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
            scrollRect.velocity = Vector2.zero;
        }
        notifyContentUpdated();
    }

    public void tryMarkScrolledToEnd()
    {
        if (hasScrolledToEnd || scrollRect == null)
        {
            return;
        }

        float viewportHeight = scrollRect.viewport.rect.height;
        float contentHeight = scrollRect.content.rect.height;
        if (contentHeight <= viewportHeight + 0.5f)
        {
            hasScrolledToEnd = true;
            notifyContentUpdated();
        }
        else if (scrollRect.verticalNormalizedPosition <= ScrollBottomThreshold)
        {
            hasScrolledToEnd = true;
            notifyContentUpdated();
        }
    }

    public void setScrollHintVisible(bool visible)
    {
        if (scrollHintRoot != null && scrollHintRoot.activeSelf != visible)
        {
            scrollHintRoot.SetActive(visible);
        }
    }

    public void setNextInteractable(bool value)
    {
        if (nextButton != null)
        {
            nextButton.interactable = value;
        }
    }

    public void setBackInteractable(bool value)
    {
        if (backButton != null)
        {
            backButton.interactable = value;
        }
    }

    public void setNextLabel(string label)
    {
        if (nextButtonLabel != null)
        {
            nextButtonLabel.text = appendHint(label, nextButtonHint);
        }
    }

    public void setBackLabel(string label)
    {
        if (backButtonLabel != null)
        {
            backButtonLabel.text = appendHint(label, backButtonHint);
        }
    }

    public void assignButtonHandlers(UnityAction onNext, UnityAction onBack)
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            if (onNext != null)
            {
                nextButton.onClick.AddListener(onNext);
            }
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            if (onBack != null)
            {
                backButton.onClick.AddListener(onBack);
            }
        }
    }

    public Button getNextButton()
    {
        return nextButton;
    }

    public Button getBackButton()
    {
        return backButton;
    }

    public void updateWorldCamera(Camera worldCamera)
    {
        if (canvas != null)
        {
            canvas.worldCamera = worldCamera;
        }
    }

    void applyBodyLayout()
    {
        Canvas.ForceUpdateCanvases();
        bodyLabel.ForceMeshUpdate();

        float preferredHeight = bodyLabel.preferredHeight;
        float viewportHeight = scrollRect.viewport.rect.height;
        if (viewportHeight <= 0f)
        {
            viewportHeight = preferredHeight;
        }
        float targetHeight = Mathf.Max(preferredHeight, viewportHeight);

        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, targetHeight);
        RectTransform textRect = bodyLabel.rectTransform;
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.sizeDelta = new Vector2(0f, targetHeight);
        textRect.anchoredPosition = Vector2.zero;

        scrollRect.verticalNormalizedPosition = 1f;
        scrollRect.velocity = Vector2.zero;
        hasScrolledToEnd = targetHeight <= viewportHeight + 0.5f;
        notifyContentUpdated();
    }

    IEnumerator deferredBodyLayout()
    {
        yield return null;
        applyBodyLayout();
        tryMarkScrolledToEnd();
        postLayoutRoutine = null;
    }

    void notifyContentUpdated()
    {
        contentUpdated?.Invoke();
    }

    void buildTitle(RectTransform parent)
    {
        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(parent, false);
        RectTransform rect = titleGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, TitleHeight);
        rect.anchoredPosition = Vector2.zero;

        titleLabel = titleGo.AddComponent<TextMeshProUGUI>();
        titleLabel.fontSize = 48f;
        titleLabel.alignment = TextAlignmentOptions.Center;
        titleLabel.enableWordWrapping = false;
        titleLabel.color = Color.white;
        applyUiFont(titleLabel);
    }

    void buildScrollArea(RectTransform parent)
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
        configureRoundedCorners(scrollRounded);

        scrollRect = scrollRoot.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = false;
        scrollRect.scrollSensitivity = 0f;

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
        contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 400f);

        GameObject textGo = new GameObject("Body");
        textGo.transform.SetParent(contentGo.transform, false);
        bodyLabel = textGo.AddComponent<TextMeshProUGUI>();
        bodyLabel.fontSize = 32f;
        bodyLabel.alignment = TextAlignmentOptions.TopLeft;
        bodyLabel.enableWordWrapping = true;
        bodyLabel.color = Color.white;
        applyUiFont(bodyLabel);

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
    }

    void buildButtons(RectTransform parent, UnityAction onNext, UnityAction onBack)
    {
        Color backNormal = new Color(0.25f, 0.25f, 0.28f, 1f);
        Color backHighlight = new Color(0.35f, 0.35f, 0.38f, 1f);
        Color nextNormal = new Color(0.1f, 0.6f, 0.18f, 1f);
        Color nextHighlight = new Color(0.18f, 0.72f, 0.28f, 1f);
        Color nextPressed = new Color(0.08f, 0.45f, 0.14f, 1f);
        Color disabledGray = new Color(0.32f, 0.32f, 0.34f, 1f);

        float buttonBaseY = SidePadding + HintAreaHeight;

        backButton = createButton(parent, "BackButton", new Vector2(SidePadding, buttonBaseY), TextAlignmentOptions.Center, false, backNormal, backHighlight, backHighlight, backNormal);
        backButtonLabel = backButton.GetComponentInChildren<TextMeshProUGUI>();
        applyUiFont(backButtonLabel);
        backButton.interactable = false;
        backButton.onClick.AddListener(onBack);

        nextButton = createButton(parent, "NextButton", new Vector2(-SidePadding, buttonBaseY), TextAlignmentOptions.Center, true, nextNormal, nextHighlight, nextPressed, disabledGray);
        nextButtonLabel = nextButton.GetComponentInChildren<TextMeshProUGUI>();
        applyUiFont(nextButtonLabel);
        var colors = nextButton.colors;
        colors.disabledColor = disabledGray;
        colors.normalColor = nextNormal;
        colors.highlightedColor = nextHighlight;
        colors.pressedColor = nextPressed;
        colors.colorMultiplier = 1f;
        nextButton.colors = colors;
        nextButton.onClick.AddListener(onNext);
    }

    Button createButton(RectTransform parent, string name, Vector2 anchoredPosition, TextAlignmentOptions alignment, bool anchorRight, Color normalColor, Color highlightColor, Color pressedColor, Color disabledColor)
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
        configureRoundedCorners(rounded);

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
        applyUiFont(label);

        return button;
    }

    void buildScrollHint(RectTransform parent)
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
        applyUiFont(hintLabel);
        scrollHintRoot = hintGo;
        scrollHintRoot.SetActive(false);
    }

    void applyUiFont(TextMeshProUGUI label)
    {
        if (label == null)
        {
            return;
        }

        if (uiFont == null)
        {
            uiFont = UIFontProvider.GetFont();
        }

        if (uiFont != null)
        {
            label.font = uiFont;
        }
    }

    void configureRoundedCorners(ImageWithRoundedCorners rounded)
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

    static void setLayerRecursively(Transform root, int layer)
    {
        if (root == null)
        {
            return;
        }

        root.gameObject.layer = layer;
        foreach (Transform child in root)
        {
            setLayerRecursively(child, layer);
        }
    }

    static string appendHint(string label, string hint)
    {
        if (string.IsNullOrEmpty(hint))
        {
            return label;
        }

        return string.Concat(label, hint);
    }
}
