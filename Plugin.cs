using System;
using System.Collections;
using BepInEx;
using BepInEx.Logging;
using Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LevelCollections;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    internal static class PluginInfo
    {
        public const string PLUGIN_GUID = "LevelCollections";
        public const string PLUGIN_NAME = "Level Collections";
        public const string PLUGIN_VERSION = "1.0.0";
    }

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        ConfigLoader.Load();

        var mgr = new GameObject("CollectionManager");
        DontDestroyOnLoad(mgr);
        mgr.AddComponent<CollectionManager>();

        ConsoleCommands.Register();

        HarmonyPatches.Apply();

        var boot = new GameObject("LevelCollectionsBootstrapper");
        DontDestroyOnLoad(boot);
        boot.AddComponent<Bootstrapper>();
    }
}

internal class Bootstrapper : MonoBehaviour
{
    private bool _menuInjected;
    private Button _collectionsButton;
    private bool _buttonFailed;
    private Action _collectionsLabelRefresh;

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(InjectLoop());
    }
    private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_collectionsLabelRefresh != null)
        {
            LocalizedText.Unregister(_collectionsLabelRefresh);
            _collectionsLabelRefresh = null;
        }
        _menuInjected = false;
        _collectionsButton = null;
        _buttonFailed = false;
    }

    private static readonly WaitForSeconds _pollInterval = new WaitForSeconds(1f);

    private IEnumerator InjectLoop()
    {
        while (true)
        {
            if (MenuSystem.instance != null)
            {
                if (IsMultiplayerMode())
                {
                    // Collections don't work in multiplayer — using them causes a soft-lock.
                    // Destroy known button and also sweep for orphaned clones
                    // (e.g. left over after a scene reload cleared _collectionsButton but
                    // the LevelSelectMenu2 instance was reused via MenuSystem).
                    if (ButtonAlive())
                    {
                        Destroy(_collectionsButton.gameObject);
                        _collectionsButton = null;
                    }
                    if (_collectionsLabelRefresh != null)
                    {
                        LocalizedText.Unregister(_collectionsLabelRefresh);
                        _collectionsLabelRefresh = null;
                    }
                    SweepCollectionsButtons();
                }
                else
                {
                    if (!_menuInjected) InjectCollectionsMenu();
                    if (_menuInjected && !ButtonAlive() && !_buttonFailed) InjectCollectionsButton();
                }
            }
            yield return _pollInterval;
        }
    }

    private static bool IsMultiplayerMode()
    {
        // NetGame.isNetStarted covers most cases (hosted/joined a room).
        // LevelSelectMenu2.displayMode covers the edge case where the
        // menu has been set up for lobbies but isNetStarted hasn't flipped yet.
        if (NetGame.isNetStarted)
            return true;
        var mode = LevelSelectMenu2.displayMode;
        return mode == LevelSelectMenuMode.BuiltInLobbies
            || mode == LevelSelectMenuMode.WorkshopLobbies;
    }

    /// <summary>
    /// Destroy every "CollectionsTitle" GameObject under every
    /// LevelSelectMenu2 instance.  This catches orphaned buttons
    /// that were created before OnSceneLoaded cleared our reference.
    /// </summary>
    private static void SweepCollectionsButtons()
    {
        var all = MenuSystem.instance.GetComponentsInChildren<LevelSelectMenu2>(includeInactive: true);
        if (all == null)
            return;
        foreach (var lsm2 in all)
        {
            if (lsm2 == null)
                continue;
            // Button lives under topPanel, not directly under lsm2 — search recursively.
            foreach (Transform child in lsm2.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (child != null && child.name == "CollectionsTitle")
                {
                    Destroy(child.gameObject);
                    Plugin.Logger.LogInfo("LevelCollections: swept orphan button from multiplayer menu.");
                }
            }
        }
    }

    // ── Menu ──────────────────────────────────────────────────────

    private void InjectCollectionsMenu()
    {
        var ex = MenuSystem.instance.GetComponentInChildren<CollectionsMenu>(includeInactive: true);
        if (ex != null) { _menuInjected = true; return; }
        var go = new GameObject("CollectionsMenu");
        go.SetActive(false); // deactivate BEFORE AddComponent so OnEnable won't fire yet
        go.transform.SetParent(MenuSystem.instance.transform, false);
        go.AddComponent<CollectionsMenu>(); // only Awake runs; OnEnable waits for real transition
        _menuInjected = true;
        Plugin.Logger.LogInfo("CollectionsMenu injected into MenuSystem.");
    }

    // ── Button ────────────────────────────────────────────────────

    private bool ButtonAlive() => _collectionsButton != null && _collectionsButton;

    private void InjectCollectionsButton()
    {
        // Never inject the Collections button in a multiplayer lobby —
        // collection runs don't work over the network and cause a soft-lock.
        if (IsMultiplayerMode())
        {
            Plugin.Logger.LogInfo("LevelCollections: skipping button injection (multiplayer).");
            return;
        }

        var all = MenuSystem.instance.GetComponentsInChildren<LevelSelectMenu2>(includeInactive: true);
        if (all == null || all.Length == 0) return;
        var lsm2 = all[0];

        Plugin.Logger.LogInfo("LevelCollections: injecting Collections button...");

        // Clone from an actual Button (not a text label like TitleCampaign)
        GameObject refBtn = FirstValid(
            lsm2.showCustomButton, lsm2.showSubscribedButton,
            lsm2.ShowSubscribedLevelButton, lsm2.ShowLocalLevelButton, lsm2.PlayButton);
        if (refBtn == null || !refBtn) { Plugin.Logger.LogWarning("LevelCollections: no reference button."); return; }
        Plugin.Logger.LogInfo("LevelCollections: Collections Button using " + refBtn.gameObject.ToString());

        // Parent under topPanel (AutoNavigation) so the button lives in the tab bar
        Transform parent;
        if (lsm2.topPanel != null && lsm2.topPanel)
            parent = lsm2.topPanel.transform;
        else
            parent = refBtn.transform.parent;
        if (parent == null) { Plugin.Logger.LogWarning("LevelCollections: no parent."); return; }

        var go = Instantiate(refBtn, parent, false);
        go.name = "CollectionsTitle";

        // Nudge away from screen edges (keep original anchor/pivot — changing them
        // breaks the button's internal layout).
        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
            rt.anchoredPosition += new Vector2(-8f, -8f);

        go.transform.SetAsFirstSibling(); // leftmost in AutoNavigation order
        go.SetActive(true);

        var lbl = go.GetComponentInChildren<TextMeshProUGUI>();
        if (lbl != null)
        {
            var tmp = lbl;
            _collectionsLabelRefresh = () =>
            {
                if (tmp != null && tmp)
                    tmp.text = LocalizedText.Get("Collections").ToUpper();
            };
            LocalizedText.Register(_collectionsLabelRefresh);
        }
        else
        {
            var t = go.GetComponentInChildren<Text>();
            if (t != null)
            {
                var legacy = t;
                _collectionsLabelRefresh = () =>
                {
                    if (legacy != null && legacy)
                        legacy.text = LocalizedText.Get("Collections").ToUpper();
                };
                LocalizedText.Register(_collectionsLabelRefresh);
            }
        }

        var btn = go.GetComponentInChildren<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                Plugin.Logger.LogInfo("Collections button clicked.");
                if (MenuSystem.instance != null &&
                    MenuSystem.instance.GetComponentInChildren<CollectionsMenu>(includeInactive: true) != null)
                    lsm2.TransitionForward<CollectionsMenu>();
                else
                    Plugin.Logger.LogError("CollectionsMenu missing.");
            });
            _collectionsButton = btn;
            Plugin.Logger.LogInfo("LevelCollections: Collections button injected.");
        }
        else
        {
            Plugin.Logger.LogWarning("LevelCollections: no Button on clone.");
            Destroy(go);
            _buttonFailed = true;
        }
    }

    private static GameObject FirstValid(params GameObject[] cands)
    {
        foreach (var g in cands) if (g != null && g) return g;
        return null;
    }
}
