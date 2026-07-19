using System.Collections.Generic;
using HumanAPI;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace LevelCollections;

public class CollectionsMenu : MenuTransition
{
    private ListView _colList, _lvlList;
    private readonly List<CollectionDefinition> _colData = new List<CollectionDefinition>();
    private int _selCol = -1, _selLvl = -1;
    private CollectionListItem _prevColItem, _prevLvlItem;

    private RawImage _thumbnail;
    private TextMeshProUGUI _titleText;
    private Button _startBtn;
    private GameObject _startBtnGo;

    private const float PanelW  = 280f;
    private const float ItemH   = 40f;
    private const float ThumbW  = 320f;
    private const float ThumbH  = 180f;

    // ── Lifecycle ──────────────────────────────────────────────────

    /// <summary>
    /// Ensure RectTransform and CanvasGroup exist before MenuTransition.OnEnable()
    /// caches them — otherwise Apply() throws NRE during transitions.
    /// </summary>
    private void Awake()
    {
        if (GetComponent<RectTransform>() == null)
            gameObject.AddComponent<RectTransform>();
        if (GetComponent<CanvasGroup>() == null)
            gameObject.AddComponent<CanvasGroup>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        BuildOnce();
        var cg = GetComponent<CanvasGroup>();
        Plugin.Logger.LogInfo(
            $"[BackBtn] OnEnable DONE | " +
            $"myCG.alpha={cg?.alpha} | " +
            $"myCG.blocksRaycasts={cg?.blocksRaycasts} | " +
            $"gameObject.activeSelf={gameObject.activeSelf} | " +
            $"gameObject.activeInHierarchy={gameObject.activeInHierarchy}");
    }

    public override void OnGotFocus()
    {
        base.OnGotFocus();
        var cg = GetComponent<CanvasGroup>();
        Plugin.Logger.LogInfo(
            $"[BackBtn] OnGotFocus | " +
            $"myCG.alpha={cg?.alpha} | " +
            $"myCG.blocksRaycasts={cg?.blocksRaycasts} | " +
            $"backBtnGo.activeSelf={_backBtnGo?.activeSelf} | " +
            $"backBtnGo.activeInHierarchy={_backBtnGo?.activeInHierarchy}");
        _colList.onSelect = OnColSelect;
        _colList.onSubmit = OnColSubmit;
        _colList.onDeSelect = OnColDeSelect;
        _colList.onPointerClick = OnColPointerClick;
        _lvlList.onSelect = OnLvlSelect;
        _lvlList.onSubmit = OnLvlSubmit;
        _lvlList.onDeSelect = OnLvlDeSelect;
        _lvlList.onPointerClick = OnLvlPointerClick;
        Rebuild();
        if (_colData.Count > 0)
        {
            _colList.FocusItem(0);
            SelectCollection(0);
        }
        if (_startBtnGo != null) _startBtnGo.SetActive(true);
    }

    public override void OnLostFocus()
    {
        base.OnLostFocus();
        Clear();
    }

    public override void OnBack() => TransitionBack<LevelSelectMenu2>();

    // ── One-time UI construction ──────────────────────────────────

    private void BuildOnce()
    {
        if (_colList != null) return;

        var root = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;

        var tmpl = FindTemplateButton();

        var main = NewChild("MainPanel", gameObject);
        var mRT = main.GetComponent<RectTransform>();
        mRT.anchorMin = new Vector2(0.04f, 0.10f);
        mRT.anchorMax = new Vector2(0.96f, 0.92f);
        mRT.offsetMin = mRT.offsetMax = Vector2.zero;
        var hlg = main.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.UpperCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        BuildColList(main);
        BuildLvlList(main);
        BuildInfo(main);
        BuildBackButton(tmpl);
        BuildStartButton(tmpl);
    }

    private static GameObject FindTemplateButton()
    {
        var all = MenuSystem.instance.GetComponentsInChildren<LevelSelectMenu2>(true);
        if (all == null || all.Length == 0)
        {
            Plugin.Logger.LogWarning("[BackBtn] FindTemplateButton: LevelSelectMenu2 NOT found in MenuSystem children.");
            return null;
        }
        var lsm2 = all[0];
        if (lsm2.BackButton != null && lsm2.BackButton)
        {
            Plugin.Logger.LogInfo($"[BackBtn] FindTemplateButton: using BackButton (activeSelf={lsm2.BackButton.activeSelf}).");
            return lsm2.BackButton;
        }
        if (lsm2.PlayButton != null && lsm2.PlayButton)
        {
            Plugin.Logger.LogInfo($"[BackBtn] FindTemplateButton: Fallback to PlayButton (activeSelf={lsm2.PlayButton.activeSelf}).");
            return lsm2.PlayButton;
        }
        if (lsm2.showCustomButton != null && lsm2.showCustomButton)
        {
            Plugin.Logger.LogInfo($"[BackBtn] FindTemplateButton: Fallback to showCustomButton (activeSelf={lsm2.showCustomButton.activeSelf}).");
            return lsm2.showCustomButton;
        }
        Plugin.Logger.LogWarning("[BackBtn] FindTemplateButton: All candidates null, using from-scratch fallback.");
        return null;
    }

    // ── Left / Middle: list panels ────────────────────────────────

    private void BuildColList(GameObject parent)
    {
        var panel = NewChild("ColPanel", parent);
        panel.AddComponent<LayoutElement>().preferredWidth = PanelW;
        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 4f;
        AddHeader("Collections", panel);
        _colList = BuildListView(panel);
    }

    private void BuildLvlList(GameObject parent)
    {
        var panel = NewChild("LvlPanel", parent);
        panel.AddComponent<LayoutElement>().preferredWidth = PanelW;
        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 4f;
        AddHeader("Levels", panel);
        _lvlList = BuildListView(panel);
    }

    /// <summary>
    /// Build a game-native ListView inside a panel.  The ListView is created
    /// inactive so its Awake sees the fully-wired itemTemplate and itemContainer.
    /// </summary>
    private static ListView BuildListView(GameObject panelParent)
    {
        var lvGo = new GameObject("ListView", typeof(RectTransform));
        lvGo.SetActive(false);

        // Fill remaining space in parent
        var lvRT = lvGo.GetComponent<RectTransform>();
        lvRT.anchorMin = Vector2.zero;
        lvRT.anchorMax = Vector2.one;
        lvRT.offsetMin = Vector2.zero;
        lvRT.offsetMax = Vector2.zero;
        var le = lvGo.AddComponent<LayoutElement>();
        le.preferredHeight = 100f;
        le.flexibleHeight = 1f;

        // Image is required for the GraphicRaycaster to route events into
        // this subtree correctly when using InControlInputModule.
        var bg = lvGo.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0f);
        bg.raycastTarget = false;

        // Item container pinned to top, grows downward
        var container = NewChild("ItemContainer", lvGo);
        var crt = container.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1);
        crt.anchorMax = new Vector2(1, 1);
        crt.pivot = new Vector2(0.5f, 1);
        crt.sizeDelta = Vector2.zero;
        container.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Item template: built from scratch with correct settings.
        // Explicit Navigation mode is required for EventSystem to
        // route input events correctly with InControlInputModule.
        var tmpl = NewChild("ItemTemplate", lvGo);
        var tmplRT2 = tmpl.GetComponent<RectTransform>();
        tmplRT2.anchorMin = new Vector2(0, 1);
        tmplRT2.anchorMax = new Vector2(1, 1);
        tmplRT2.pivot = new Vector2(0.5f, 1);
        tmpl.AddComponent<LayoutElement>().preferredHeight = ItemH;
        var img2 = tmpl.AddComponent<Image>();
        img2.color = Color.white;
        var mb2 = tmpl.AddComponent<MenuButton>();
        mb2.targetGraphic = img2;
        var nav = mb2.navigation;
        nav.mode = Navigation.Mode.Automatic;
        mb2.navigation = nav;
        mb2.transition = Selectable.Transition.ColorTint;
        tmpl.AddComponent<CollectionListItem>();
        var labelGo2 = NewChild("Label", tmpl);
        var lrt2 = labelGo2.GetComponent<RectTransform>();
        lrt2.anchorMin = Vector2.zero;
        lrt2.anchorMax = Vector2.one;
        lrt2.offsetMin = new Vector2(12, 2);
        lrt2.offsetMax = new Vector2(-12, -2);
        var txt2 = labelGo2.AddComponent<TextMeshProUGUI>();
        txt2.fontSize = 16;
        txt2.alignment = TextAlignmentOptions.Left;
        tmpl.SetActive(false);

        // Add ListView component while still inactive, wire fields, then activate
        var lv = lvGo.AddComponent<ListView>();
        lv.itemTemplate = tmpl.GetComponent<Button>();
        lv.itemContainer = container;

        lvGo.transform.SetParent(panelParent.transform, false);
        lvGo.SetActive(true);

        return lv;
    }

    // ── Right: info panel ─────────────────────────────────────────

    private void BuildInfo(GameObject parent)
    {
        var panel = NewChild("InfoPanel", parent);
        panel.AddComponent<LayoutElement>().preferredWidth = ThumbW + 16f;
        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 0f;
        vlg.childAlignment = TextAnchor.UpperCenter;

        var spacer = NewChild("Spacer", panel);
        spacer.AddComponent<LayoutElement>().preferredHeight = 32f;

        var thumbArea = NewChild("ThumbArea", panel);
        thumbArea.AddComponent<LayoutElement>().preferredWidth = ThumbW;
        thumbArea.AddComponent<LayoutElement>().preferredHeight = ThumbH;
        thumbArea.AddComponent<Image>().color = Color.white;
        thumbArea.AddComponent<Mask>().showMaskGraphic = false;

        // RawImage must be a CHILD of thumbArea (not on thumbArea itself —
        // Unity forbids two Graphic components on the same GameObject).
        var thumbChild = NewChild("ThumbImage", thumbArea);
        _thumbnail = thumbChild.AddComponent<RawImage>();
        _thumbnail.color = Color.grey;
        _thumbnail.enabled = false;
        var tnRT = thumbChild.GetComponent<RectTransform>();
        tnRT.anchorMin = Vector2.zero;
        tnRT.anchorMax = Vector2.one;
        tnRT.offsetMin = tnRT.offsetMax = Vector2.zero;

        var overlay = NewChild("Overlay", thumbArea);
        var ovRT = overlay.GetComponent<RectTransform>();
        ovRT.anchorMin = new Vector2(0f, 0f);
        ovRT.anchorMax = new Vector2(1f, 0.25f);
        ovRT.offsetMin = ovRT.offsetMax = Vector2.zero;
        var ovImg = overlay.AddComponent<Image>();
        ovImg.color = new Color(0f, 0f, 0f, 0.55f);
        ovImg.raycastTarget = false;

        // TextMeshProUGUI must be on a child — overlay already has an Image.
        var titleGo = NewChild("Title", overlay);
        _titleText = titleGo.AddComponent<TextMeshProUGUI>();
        _titleText.fontSize = 16;
        _titleText.alignment = TextAlignmentOptions.Center;
        _titleText.color = Color.white;
        var ttRT = titleGo.GetComponent<RectTransform>();
        ttRT.anchorMin = Vector2.zero;
        ttRT.anchorMax = Vector2.one;
        ttRT.offsetMin = new Vector2(8f, 0f);
        ttRT.offsetMax = new Vector2(-8f, 0f);
    }

    // ── Buttons ───────────────────────────────────────────────────

    private Button _backBtn;
    private GameObject _backBtnGo;

    private void BuildBackButton(GameObject tmpl)
    {
        var go = CloneOrCreateButton(tmpl, "CollectionsBackBtn");
        if (go == null) return;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(40f, 40f);
        EnsureMinSize(rt, 120f, 44f);
        SetButtonText(go, "Back");
        _backBtn = go.GetComponentInChildren<Button>() ?? go.GetComponent<Button>();
        if (_backBtn != null)
        {
            _backBtn.onClick.RemoveAllListeners();
            _backBtn.onClick.AddListener(() => OnBack());
        }
        _backBtnGo = go;
        go.transform.SetAsLastSibling();
        go.SetActive(true);

        // ── Diagnostic log ────────────────────────────────────
        var myCG = GetComponent<CanvasGroup>();
        var img = go.GetComponent<Image>();
        Plugin.Logger.LogInfo(
            $"[BackBtn] CREATED | " +
            $"template={tmpl?.name ?? "NULL"} | " +
            $"go.activeSelf={go.activeSelf} | " +
            $"go.activeInHierarchy={go.activeInHierarchy} | " +
            $"myCG.alpha={myCG?.alpha} | " +
            $"myCG.blocksRaycasts={myCG?.blocksRaycasts} | " +
            $"pos=({rt.anchoredPosition.x:F0},{rt.anchoredPosition.y:F0}) | " +
            $"size=({rt.sizeDelta.x:F0},{rt.sizeDelta.y:F0}) | " +
            $"img.sprite={(img != null && img.sprite != null ? img.sprite.name : "NULL")} | " +
            $"img.color={img?.color} | " +
            $"img.enabled={img?.enabled}");
    }

    private void BuildStartButton(GameObject tmpl)
    {
        var go = CloneOrCreateButton(tmpl, "CollectionsStartBtn");
        if (go == null) return;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.68f, 0.10f);
        rt.anchorMax = new Vector2(0.92f, 0.18f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        EnsureMinSize(rt, 120f, 44f);
        SetButtonText(go, "Start");
        _startBtn = go.GetComponentInChildren<Button>() ?? go.GetComponent<Button>();
        if (_startBtn != null)
        {
            _startBtn.onClick.RemoveAllListeners();
            _startBtn.onClick.AddListener(DoPlay);
        }
        _startBtnGo = go;
        go.SetActive(false);
    }

    private GameObject CloneOrCreateButton(GameObject tmpl, string name)
    {
        GameObject go;
        if (tmpl != null && tmpl)
        {
            go = Instantiate(tmpl, transform, false);
            // ── Sanitize clone: strip anything that can hide the button ──
            // Some prefab buttons have CanvasGroup (alpha 0), Localize, etc.
            go.transform.localScale = Vector3.one;
            var cg = go.GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = 1f; cg.blocksRaycasts = true; }
            foreach (var loc in go.GetComponentsInChildren<Localize>(true))
                Destroy(loc);
            foreach (var img in go.GetComponentsInChildren<Image>(true))
                img.enabled = true;
            foreach (var raw in go.GetComponentsInChildren<RawImage>(true))
                raw.enabled = true;
            // Ensure the button itself is a MenuButton (template should already be)
            var existingBtn = go.GetComponent<Button>();
            if (existingBtn != null) existingBtn.transition = Selectable.Transition.ColorTint;
        }
        else
        {
            go = NewChild(name, gameObject);
            var img = go.AddComponent<Image>();
            img.sprite = GetDefaultSprite();
            img.color = new Color(1f, 1f, 1f, 0.85f);
            // Use MenuButton to match the game's native button styling (highlight/press colors etc.)
            var mb = go.AddComponent<MenuButton>();
            mb.targetGraphic = img;
            mb.transition = Selectable.Transition.ColorTint;
            var lb = NewChild("Label", go);
            var lrt = lb.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var txt = lb.AddComponent<TextMeshProUGUI>();
            txt.fontSize = 22;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(0.19f, 0.19f, 0.19f, 1f);
            // Let MenuButton find its label
            mb.SetLabel(txt);
        }
        go.name = name;
        return go;
    }

    private static Sprite _defaultSprite;
    private static Sprite GetDefaultSprite()
    {
        if (_defaultSprite != null && _defaultSprite)
            return _defaultSprite;
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _defaultSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        _defaultSprite.hideFlags = HideFlags.HideAndDontSave;
        return _defaultSprite;
    }

    private static void EnsureMinSize(RectTransform rt, float minW, float minH)
    {
        var sd = rt.sizeDelta;
        if (sd.x < minW) sd.x = minW;
        if (sd.y < minH) sd.y = minH;
        rt.sizeDelta = sd;
    }

    private static void SetButtonText(GameObject go, string text)
    {
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) { tmp.text = text; return; }
        var t = go.GetComponentInChildren<Text>();
        if (t != null) t.text = text;
    }

    // ── Data / selection logic ────────────────────────────────────

    private void Rebuild()
    {
        _colData.Clear();
        _colData.AddRange(ConfigLoader.Collections);
        var names = new List<string>(_colData.Count);
        foreach (var c in _colData) names.Add(c.Name);
        _prevColItem = null;
        _prevLvlItem = null;
        _colList.Bind(names);
        _lvlList.Bind(new List<string>());
        _selCol = -1; _selLvl = -1;
    }

    private void Clear()
    {
        _prevColItem = null;
        _prevLvlItem = null;
        _colList?.Clear();
        _lvlList?.Clear();
        _colData.Clear();
        _selCol = -1; _selLvl = -1;
    }

    // ── ListView callbacks ────────────────────────────────────────

    private void OnColSelect(ListViewItem item)
    {
        var ci = item as CollectionListItem;
        if (ci == null) return;
        if (_prevColItem != null && _prevColItem != ci) _prevColItem.SetActive(false);
        ci.SetActive(true);
        _prevColItem = ci;
        SelectCollection(item.index);
    }

    private void OnColDeSelect(ListViewItem item)
    {
        var ci = item as CollectionListItem;
        if (ci != null) ci.SetActive(false);
    }

    private void OnColSubmit(ListViewItem item)
    {
        if (_lvlList.GetNumberItems > 0)
            _lvlList.FocusItem(0);
    }

    private void OnColPointerClick(ListViewItem item, int clickCount, PointerEventData.InputButton button)
    {
        // Single click: select (in case hover didn't fire onSelect)
        if (clickCount == 1)
            OnColSelect(item);
        // Double click: move focus to level list
        else if (clickCount > 1 && _lvlList.GetNumberItems > 0)
            _lvlList.FocusItem(0);
    }

    private void OnLvlSelect(ListViewItem item)
    {
        var ci = item as CollectionListItem;
        if (ci == null) return;
        if (_prevLvlItem != null && _prevLvlItem != ci) _prevLvlItem.SetActive(false);
        ci.SetActive(true);
        _prevLvlItem = ci;
        SelectLevel(item.index);
    }

    private void OnLvlDeSelect(ListViewItem item)
    {
        var ci = item as CollectionListItem;
        if (ci != null) ci.SetActive(false);
    }

    private void OnLvlSubmit(ListViewItem item)
    {
        DoPlay();
    }

    private void OnLvlPointerClick(ListViewItem item, int clickCount, PointerEventData.InputButton button)
    {
        // Single click: select (in case hover didn't fire onSelect)
        if (clickCount == 1)
            OnLvlSelect(item);
        // Double click: start game
        else if (clickCount > 1)
            DoPlay();
    }

    // ── Selection logic ───────────────────────────────────────────

    private void SelectCollection(int i)
    {
        if (i < 0 || i >= _colData.Count) return;
        if (_selCol == i) return;
        _selCol = i;

        var col = _colData[i];
        var names = new List<string>();
        if (col.Levels != null)
            foreach (var e in col.Levels)
                names.Add(string.IsNullOrEmpty(e.Title) ? e.LevelId : e.Title);

        _prevLvlItem = null;
        _lvlList.Bind(names);
        if (names.Count > 0)
        {
            _lvlList.FocusItem(0);
            SelectLevel(0);
        }
        else ClearInfo();
        if (_startBtnGo) _startBtnGo.SetActive(names.Count > 0);
    }

    private void SelectLevel(int i)
    {
        var col = (_selCol >= 0 && _selCol < _colData.Count) ? _colData[_selCol] : null;
        if (col?.Levels == null || i < 0 || i >= col.Levels.Count) return;
        if (_selLvl == i) return;
        _selLvl = i;
        ShowInfo(col.Levels[i]);
    }

    private void ShowInfo(LevelEntry e)
    {
        string t = string.IsNullOrEmpty(e.Title) ? e.LevelId : e.Title;
        if (_titleText != null) _titleText.text = t;

        Texture2D tex = null;
        if (e.ResolvedLevelType == WorkshopItemSource.BuiltIn && HFFResources.instance != null)
            tex = HFFResources.instance.FindTextureResource("LevelImages/" + e.LevelId);
        if (tex != null)
        {
            _thumbnail.texture = tex;
            _thumbnail.enabled = true;
            _thumbnail.color = Color.white;
            var tnRT = _thumbnail.rectTransform;
            if (tnRT != null)
            {
                float w = tex.width, h = tex.height;
                float rw = tnRT.rect.width, rh = tnRT.rect.height;
                float sx = 1f, sy = 1f;
                if (w / rw > h / rh)
                    sx = h / rh / (w / rw);
                else
                    sy = w / rw / (h / rh);
                _thumbnail.uvRect = new Rect(0.5f - sx / 2f, 0.5f - sy / 2f, sx, sy);
            }
        }
        else
        {
            _thumbnail.texture = null;
            _thumbnail.enabled = false;
        }
    }

    private void ClearInfo()
    {
        if (_titleText != null) _titleText.text = "";
        if (_thumbnail != null) { _thumbnail.texture = null; _thumbnail.enabled = false; }
    }

    private void DoPlay()
    {
        if (_selCol < 0 || _selCol >= _colData.Count) return;
        var col = _colData[_selCol];
        if (col.Levels == null || col.Levels.Count == 0) return;
        CollectionManager.Instance?.StartCollectionRun(_selCol, _selLvl >= 0 ? _selLvl : 0);
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static void AddHeader(string text, GameObject parent)
    {
        var go = NewChild("Header", parent);
        go.AddComponent<LayoutElement>().preferredHeight = 28f;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    private static GameObject NewChild(string name, GameObject parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent.transform, false);
        else go.transform.SetParent(null, false);
        return go;
    }

}
