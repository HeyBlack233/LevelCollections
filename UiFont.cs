using TMPro;
using UnityEngine;

namespace LevelCollections;

/// <summary>
/// Ensures the plugin's TextMeshPro text can render CJK (Chinese /
/// Japanese) glyphs.  New TextMeshProUGUI components get fontAsset = null
/// and fall back to TMP_FontAsset.defaultFontAsset, which is hard-coded to
/// "Fonts & Materials/LiberationSans SDF" — a Latin-only font, so Chinese
/// text renders as tofu boxes.  We find a game font asset that covers the
/// plugin's text (NotoSansCJKsc / ARIALUNI / rounded-x-mgenplus …) and
/// swap it in only when the component's current font is missing glyphs.
/// </summary>
internal static class UiFont
{
    /// <summary>Simplified-Chinese characters the plugin's UI can show.</summary>
    private const string ZhSample = "收藏集关卡返回刷新开始";

    /// <summary>Japanese characters the plugin's UI can show.</summary>
    private const string JaSample = "コレクションレベル戻る更新開始";

    private static TMP_FontAsset _cjkFont;
    private static bool _warnedNoFont;

    /// <summary>
    /// Find (and cache) a loaded TMP_FontAsset able to display the
    /// plugin's CJK text, or null when none is available yet.  Search
    /// order: a font covering both scripts, then one covering Simplified
    /// Chinese, then one covering Japanese, then any font containing at
    /// least one plugin CJK glyph (better than the Latin default even if
    /// a few glyphs are missing).
    /// </summary>
    public static TMP_FontAsset GetCjkFont()
    {
        if (_cjkFont != null && _cjkFont)
            return _cjkFont;

        var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (var fa in fonts)
            if (IsUsable(fa) && fa.HasCharacters(ZhSample) && fa.HasCharacters(JaSample))
                return Cache(fa);
        foreach (var fa in fonts)
            if (IsUsable(fa) && fa.HasCharacters(ZhSample))
                return Cache(fa);
        foreach (var fa in fonts)
            if (IsUsable(fa) && fa.HasCharacters(JaSample))
                return Cache(fa);
        foreach (var fa in fonts)
            if (IsUsable(fa) && ContainsAnyCjkGlyph(fa))
                return Cache(fa);
        return null;
    }

    /// <summary>
    /// If <paramref name="tmp"/>'s current font cannot display its text,
    /// swap in a CJK-capable game font.  No-op when the font is fine, the
    /// text is empty, or no CJK font is available.
    /// </summary>
    public static void EnsureCjkFont(TMP_Text tmp)
    {
        if (tmp == null || !tmp)
            return;
        if (string.IsNullOrEmpty(tmp.text))
            return;
        if (tmp.font != null && tmp.font.HasCharacters(tmp.text))
            return;
        var cjk = GetCjkFont();
        if (cjk == null)
        {
            if (!_warnedNoFont)
            {
                _warnedNoFont = true;
                Plugin.Logger.LogWarning(
                    "[UiFont] no CJK-capable font asset found for text: " + tmp.text
                );
            }
            return;
        }
        Plugin.Logger.LogInfo("[UiFont] swapping font on '" + tmp.name + "' to " + cjk.name);
        tmp.font = cjk;
    }

    private static bool IsUsable(TMP_FontAsset fa) => fa != null && fa;

    private static TMP_FontAsset Cache(TMP_FontAsset fa)
    {
        _cjkFont = fa;
        Plugin.Logger.LogInfo("[UiFont] using font asset: " + fa.name);
        return _cjkFont;
    }

    private static bool ContainsAnyCjkGlyph(TMP_FontAsset fa)
    {
        for (int i = 0; i < ZhSample.Length; i++)
            if (fa.HasCharacter(ZhSample[i]))
                return true;
        for (int i = 0; i < JaSample.Length; i++)
            if (fa.HasCharacter(JaSample[i]))
                return true;
        return false;
    }
}
