using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LevelCollections;

/// <summary>
/// Ensures the plugin's TextMeshPro text can render CJK (Chinese /
/// Japanese) glyphs.  New TextMeshProUGUI components get font = null and
/// fall back to TMP_FontAsset.defaultFontAsset ("Fonts & Materials/
/// LiberationSans SDF") — a Latin-only font, so Chinese text renders as
/// tofu boxes.  The game's own CJK fonts (NotoSansCJKsc / ARIALUNI /
/// rounded-x-mgenplus) only contain glyphs the game UI actually uses —
/// the plugin's "收藏集" is missing from every one of them.
///
/// Strategy, per text component:
///   1. if its current font already covers the text — nothing to do;
///   2. else if a loaded game font covers the text — swap to it
///      (keeps the game's visual style);
///   3. else — build a runtime "dynamic bitmap" TMP_FontAsset: glyphs are
///      rasterised from a system CJK font into a writable atlas, missing
///      characters are added on demand, and the component is rendered with
///      a plain (non-SDF) "UI/Default" material.  This covers arbitrary
///      user text (custom collection names) with any Chinese/Japanese
///      character.
/// </summary>
internal static class UiFont
{
    /// <summary>Simplified-Chinese characters the plugin's UI can show.</summary>
    private const string ZhSample = "收藏集关卡返回刷新开始";

    /// <summary>Japanese characters the plugin's UI can show.</summary>
    private const string JaSample = "コレクションレベル戻る更新開始";

    /// <summary>All plugin UI text characters (for initial rasterising).</summary>
    private const string PluginChars = ZhSample + JaSample + "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789>()!.-+ ";

    private static TMP_FontAsset _gameFont;
    private static bool _warnedNoFont;

    // ── Dynamic bitmap font (system-font rasterisation) ────────────
    private const int AtlasSize = 1024;
    private const int RasterSize = 64;
    private static TMP_FontAsset _dynFont;
    private static Material _dynMat;
    private static Font _sysFont;
    private static Texture2D _dynAtlas;
    private static readonly List<TMP_Glyph> DynGlyphs = new List<TMP_Glyph>();
    private static int _packX;
    private static int _packY;
    private static int _packRowH;

    /// <summary>
    /// If <paramref name="tmp"/>'s current font cannot display its text,
    /// swap in a CJK-capable font (game font first, dynamic fallback).
    /// </summary>
    public static void EnsureCjkFont(TMP_Text tmp)
    {
        if (tmp == null || !tmp)
            return;
        if (string.IsNullOrEmpty(tmp.text))
            return;
        if (tmp.font != null && tmp.font.HasCharacters(tmp.text))
            return;

        var game = GetGameCjkFont();
        if (game != null && game.HasCharacters(tmp.text))
        {
            Plugin.Logger.LogInfo("[UiFont] swapping font on '" + tmp.name + "' to " + game.name);
            tmp.font = game;
            return;
        }

        var dyn = GetDynamicFont();
        if (dyn == null)
        {
            if (!_warnedNoFont)
            {
                _warnedNoFont = true;
                Plugin.Logger.LogWarning(
                    "[UiFont] no CJK font path available; text may show tofu: " + tmp.text
                );
            }
            return;
        }
        EnsureGlyphs(dyn, tmp.text);
        Plugin.Logger.LogInfo("[UiFont] swapping font on '" + tmp.name + "' to dynamic system font");
        tmp.font = dyn;
        if (tmp.fontSharedMaterial == null || tmp.fontSharedMaterial.mainTexture != _dynAtlas)
            tmp.fontSharedMaterial = _dynMat;
    }

    // ── Game font lookup ────────────────────────────────────────────

    /// <summary>
    /// The loaded game TMP_FontAsset covering the most plugin CJK
    /// characters (cached).  Null when none contains any.
    /// </summary>
    private static TMP_FontAsset GetGameCjkFont()
    {
        if (_gameFont != null && _gameFont)
            return _gameFont;
        var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        TMP_FontAsset best = null;
        int bestScore = 0;
        foreach (var fa in fonts)
        {
            if (fa == null || !fa)
                continue;
            int score = CountCovered(fa, ZhSample) + CountCovered(fa, JaSample);
            if (score > bestScore)
            {
                bestScore = score;
                best = fa;
            }
        }
        if (best != null && bestScore > 0)
        {
            _gameFont = best;
            Plugin.Logger.LogInfo(
                "[UiFont] game font '" + best.name + "' covers "
                + bestScore + "/" + (ZhSample.Length + JaSample.Length)
                + " plugin CJK chars"
            );
        }
        return _gameFont;
    }

    private static int CountCovered(TMP_FontAsset fa, string chars)
    {
        int n = 0;
        for (int i = 0; i < chars.Length; i++)
            if (fa.HasCharacter(chars[i]))
                n++;
        return n;
    }

    // ── Dynamic bitmap font ─────────────────────────────────────────

    private static TMP_FontAsset GetDynamicFont()
    {
        if (_dynFont != null && _dynFont)
            return _dynFont;
        if (_sysFont == null)
        {
            _sysFont = CreateSystemCjkFont();
            if (_sysFont == null)
                return null;
        }

        _dynAtlas = new Texture2D(AtlasSize, AtlasSize, TextureFormat.RGBA32, false);
        _dynAtlas.hideFlags = HideFlags.HideAndDontSave;
        var clear = new Color32[AtlasSize * AtlasSize];
        for (int i = 0; i < clear.Length; i++)
            clear[i] = new Color32(255, 255, 255, 0);
        _dynAtlas.SetPixels32(clear);
        _dynAtlas.Apply(false);

        // Material must exist BEFORE the font asset is read: TMP_FontAsset.
        // ReadFontDefinition touches material / kerning info and NREs when
        // they are null.
        var shader = Shader.Find("UI/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
        {
            Plugin.Logger.LogError("[UiFont] no bitmap shader available (UI/Default missing)");
            return null;
        }
        _dynMat = new Material(shader);
        _dynMat.hideFlags = HideFlags.HideAndDontSave;
        _dynMat.mainTexture = _dynAtlas;

        var fi = new FaceInfo
        {
            Name = "LC_DynamicCjk",
            PointSize = RasterSize,
            Scale = 1f,
            AtlasWidth = AtlasSize,
            AtlasHeight = AtlasSize,
            Ascender = _sysFont.ascent,
            Descender = -(_sysFont.lineHeight - _sysFont.ascent),
            Baseline = _sysFont.ascent,
            LineHeight = _sysFont.lineHeight,
            Padding = 0f,
        };

        _dynFont = ScriptableObject.CreateInstance<TMP_FontAsset>();
        _dynFont.name = "LC_DynamicCjk";
        _dynFont.hideFlags = HideFlags.HideAndDontSave;
        _dynFont.AddFaceInfo(fi);
        _dynFont.material = _dynMat;
        _dynFont.atlas = _dynAtlas;
        _dynFont.AddKerningInfo(new KerningTable());
        _dynFont.AddGlyphInfo(DynGlyphs.ToArray());
        _dynFont.ReadFontDefinition();

        EnsureGlyphs(_dynFont, PluginChars);
        return _dynFont;
    }

    /// <summary>Add every character of <paramref name="text"/> missing from
    /// the dynamic font.</summary>
    private static void EnsureGlyphs(TMP_FontAsset font, string text)
    {
        if (string.IsNullOrEmpty(text))
            return;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == ' ' || font.HasCharacter(c))
                continue;
            AddGlyph(font, c);
        }
    }

    private static void AddGlyph(TMP_FontAsset font, char c)
    {
        _sysFont.RequestCharactersInTexture(c.ToString(), RasterSize, FontStyle.Normal);
        CharacterInfo ci;
        if (!_sysFont.GetCharacterInfo(c, out ci))
        {
            Plugin.Logger.LogWarning("[UiFont] glyph not available in system font: " + c);
            return;
        }

        var srcTex = _sysFont.material.mainTexture as Texture2D;
        if (srcTex == null)
            return;
        float tw = srcTex.width;
        float th = srcTex.height;
        int gw = Mathf.Max(1, Mathf.RoundToInt((ci.uvTopRight.x - ci.uvBottomLeft.x) * tw));
        // Unity 2017 CharacterInfo UVs are bottom-left origin, but guard
        // against the top-left convention (uvTopRight.y < uvBottomLeft.y)
        // so glyphs never end up upside down / 1px smears.
        int ghRaw = Mathf.RoundToInt((ci.uvTopRight.y - ci.uvBottomLeft.y) * th);
        bool flipY = ghRaw < 0;
        int gh = Mathf.Max(1, Mathf.Abs(ghRaw));
        int sx0 = Mathf.RoundToInt(ci.uvBottomLeft.x * tw);
        int sy0 = flipY
            ? Mathf.RoundToInt((1f - ci.uvBottomLeft.y) * th)
            : Mathf.RoundToInt(ci.uvBottomLeft.y * th);

        var srcPixels = ReadTextureRegion(srcTex, sx0, sy0, gw, gh);
        if (srcPixels == null)
            return;

        // Pack into the atlas (row-major from the top-left).
        if (_packX + gw + 2 > AtlasSize)
        {
            _packX = 0;
            _packY += _packRowH;
            _packRowH = 0;
        }
        if (_packY + gh + 2 > AtlasSize)
        {
            Plugin.Logger.LogError("[UiFont] dynamic atlas full, cannot add glyph: " + c);
            return;
        }
        int px = _packX;
        int py = _packY;
        _packX += gw + 2;
        if (gh + 2 > _packRowH)
            _packRowH = gh + 2;

        // Copy glyph pixels.  Atlas SetPixel y counts from the BOTTOM;
        // TMP glyph.y counts from the TOP, so the glyph sits at py from
        // the top → atlas row (from bottom) = AtlasSize - (py + gh) + row.
        for (int row = 0; row < gh; row++)
        {
            // srcPixels rows run bottom→top; with the flipped convention
            // the sampled region starts at the glyph's top, so reverse.
            int srcRow = flipY ? (gh - 1 - row) : row;
            for (int col = 0; col < gw; col++)
            {
                _dynAtlas.SetPixel(
                    px + col,
                    AtlasSize - (py + gh) + row,
                    srcPixels[srcRow * gw + col]
                );
            }
        }
        _dynAtlas.Apply(false);

        var g = new TMP_Glyph
        {
            id = c,
            x = px,
            y = py,
            width = gw,
            height = gh,
            xOffset = ci.bearing,
            yOffset = ci.maxY,
            xAdvance = ci.advance,
            scale = 1f,
        };
        DynGlyphs.Add(g);
        font.AddGlyphInfo(DynGlyphs.ToArray());
        font.ReadFontDefinition();
    }

    /// <summary>Read a pixel region of a texture via a RenderTexture
    /// (works even when the source texture is not readable).</summary>
    private static Color32[] ReadTextureRegion(Texture2D src, int x, int y, int w, int h)
    {
        var rt = RenderTexture.GetTemporary(src.width, src.height, 0);
        var prev = RenderTexture.active;
        try
        {
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(x, y, w, h), 0, 0);
            tex.Apply(false);
            var px = tex.GetPixels32();
            Object.Destroy(tex);
            return px;
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError("[UiFont] ReadTextureRegion failed: " + ex);
            return null;
        }
        finally
        {
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
        }
    }

    private static Font CreateSystemCjkFont()
    {
        try
        {
            string[] names = Font.GetOSInstalledFontNames();
            if (names != null)
            {
                foreach (var n in names)
                {
                    string low = n.ToLowerInvariant();
                    if (
                        low.Contains("yahei") || low.Contains("simhei") || low.Contains("simsun")
                        || low.Contains("noto sans cjk") || low.Contains("source han")
                        || low.Contains("pingfang") || low.Contains("yu gothic")
                        || low.Contains("ms gothic") || low.Contains("ms ui gothic")
                    )
                    {
                        Plugin.Logger.LogInfo("[UiFont] system CJK font: " + n);
                        return Font.CreateDynamicFontFromOSFont(n, RasterSize);
                    }
                }
                if (names.Length > 0)
                {
                    Plugin.Logger.LogWarning(
                        "[UiFont] no known CJK font installed, trying first available: " + names[0]
                    );
                    return Font.CreateDynamicFontFromOSFont(names[0], RasterSize);
                }
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError("[UiFont] CreateSystemCjkFont failed: " + ex);
        }
        return null;
    }
}
