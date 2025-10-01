using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "UIFontConfig", menuName = "OpenSoundLab/UI Font Config")]
public class UIFontConfig : ScriptableObject
{
    [SerializeField] TMP_FontAsset baseFont;

    public TMP_FontAsset BaseFont => baseFont;
}
