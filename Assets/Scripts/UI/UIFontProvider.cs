using TMPro;
using UnityEngine;

public static class UIFontProvider
{
    const string ResourcePath = "UI/UIFontConfig";
    static TMP_FontAsset _cachedFont;
    static bool _loggedMissing;

    public static TMP_FontAsset GetFont()
    {
        if (_cachedFont != null)
        {
            return _cachedFont;
        }

        UIFontConfig config = Resources.Load<UIFontConfig>(ResourcePath);
        if (config != null && config.BaseFont != null)
        {
            _cachedFont = config.BaseFont;
            return _cachedFont;
        }

        _cachedFont = TMP_Settings.defaultFontAsset;
        if (!_loggedMissing)
        {
            Debug.LogWarning("UIFontProvider: UIFontConfig missing or unassigned. Falling back to TMP default font.");
            _loggedMissing = true;
        }
        return _cachedFont;
    }
}
