using System.Collections;
using BepInEx;
using BepInEx.Logging;
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

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(InjectLoop());
    }
    private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _menuInjected = false;
        _collectionsButton = null;
        _buttonFailed = false;
    }

    private IEnumerator InjectLoop()
    {
        while (true)
        {
            if (MenuSystem.instance != null)
            {
                if (!_menuInjected) InjectCollectionsMenu();
                if (_menuInjected && !ButtonAlive() && !_buttonFailed) InjectCollectionsButton();
            }
            yield return new WaitForSeconds(1f);
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
        var all = MenuSystem.instance.GetComponentsInChildren<LevelSelectMenu2>(includeInactive: true);
        if (all == null || all.Length == 0) return;
        var lsm2 = all[0];

        Plugin.Logger.LogInfo("LevelCollections: injecting Collections button...");

        // Clone from an actual Button (not a text label like TitleCampaign)
        GameObject refBtn = FirstValid(
            lsm2.showCustomButton, lsm2.showSubscribedButton,
            lsm2.ShowSubscribedLevelButton, lsm2.ShowLocalLevelButton, lsm2.PlayButton);
        if (refBtn == null || !refBtn) { Plugin.Logger.LogWarning("LevelCollections: no reference button."); return; }

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
        if (lbl != null) lbl.text = "COLLECTIONS";
        else { var t = go.GetComponentInChildren<Text>(); if (t != null) t.text = "COLLECTIONS"; }

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
