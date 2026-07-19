# LevelCollections — Human: Fall Flat BepInEx Plugin

Adds custom level collections to the game. Each collection is an ordered list of levels; clearing one advances to the next, and the last level returns to the main menu.

## Project

- **Stack**: C# / .NET Standard 2.0, BepInEx 5.x, HarmonyX, Unity 2017.4 (IL2CPP/Mono)
- **Entry**: `Plugin.cs` — `BaseUnityPlugin.Awake()` wires config loading, `CollectionManager`, Harmony patches, and `Bootstrapper` (coroutine-based UI injection)
- **Config**: JSON at `BepInEx/Config/LevelCollections.json` (auto-generated on first run). Serialization via `MiniJSON.cs` because Unity 2017 `JsonUtility` fails on nested `List<T>`
- **Game refs at**: `~/.local/share/Steam/steamapps/common/Human Fall Flat/Human_Data/Managed/`
- **Game sources** (read-only): `/home/Chocola/dev/unity_mods/hff/sources/`

## Commands

```bash
# Build (no NuGet restore — all refs are direct DLL references to game/BepInEx)
dotnet build --no-restore -c Release
# Output: bin/Release/netstandard2.0/LevelCollections.dll

# Debug build
dotnet build --no-restore

# Clean
rm -rf bin obj && dotnet restore && dotnet build --no-restore -c Release
```

## Architecture

```
Plugin.cs          — Entry point, Bootstrapper (coroutine polls MenuSystem,
                     injects CollectionsMenu + Collections button into LevelSelectMenu2)
                     ★ IMPORTANT: SetActive(false) BEFORE AddComponent, or OnEnable
                     fires during injection and BuildOnce runs prematurely.
HarmonyPatches.cs  — Single prefix on App.StartNextLevel (ulong,int) — intercepts
                     level progression for collection runs
CollectionManager.cs — Singleton MonoBehaviour, tracks IsInCollectionRun,
                     CurrentCollectionIndex, CurrentLevelIndex. Calls App.instance
                     to launch levels; AdvanceToNextLevel/EndCollectionRun
CollectionsMenu.cs — MenuTransition subclass, three-panel UI built in code:
                     left=collection list, middle=level list, right=info+play button.
                     Awake() ensures RectTransform+CanvasGroup exist BEFORE
                     MenuTransition.OnEnable() caches them (otherwise Apply() NREs).
                     Back button at bottom-left (40,40), Start button at bottom-right.
CollectionData.cs  — LevelEntry / CollectionDefinition / CollectionConfig (POCOs)
ConfigLoader.cs    — Reads/writes JSON config via MiniJSON, creates default on first run
MiniJSON.cs        — Standalone JSON serializer/deserializer (no external deps)
CollectionListItem.cs — ListViewItem subclass, handles highlight/press visual state
```

## Conventions

- **Build**: always use `--no-restore` (NuGet is broken in this Docker environment). All DLLs referenced directly via `<HintPath>` in `.csproj`.
- **Logging**: use `Plugin.Logger.LogInfo/LogWarning/LogError`. BepInEx console output. Diagnostic logs prefixed with `[BackBtn]` are temporary and can be removed after stabilization.
- **UI injection**: Harmony is unreliable for private/override methods on `LevelSelectMenu2`. Use the `Bootstrapper` coroutine pattern instead — poll with `MenuSystem.instance.GetComponentsInChildren<T>(includeInactive: true)` and inject directly.
- **Unity fake-null**: always check `obj != null && obj` — Unity overrides `==` so destroyed objects aren't truly null.
- **Button/Selectable ambiguity**: `HumanAPI.Button` conflicts with `UnityEngine.UI.Button`. Use `using Button = UnityEngine.UI.Button;` in files that import both namespaces.
- **Target**: `netstandard2.0` with `<NoStdLib>true</NoStdLib>` + `<DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>`. All BCL types come from the game's `mscorlib.dll`/`System.dll`/`System.Core.dll`.
- **.csproj game paths**: defined via `<GAME_MANAGED>` and `<BEPINEX_CORE>` properties at the bottom of the csproj.

## Gotchas (hard-won debugging lessons)

### Unity Graphic component conflicts
A single GameObject CANNOT hold two `Graphic` subclasses (Image, RawImage, TextMeshProUGUI). If you need both, put them on sibling/parent-child GameObjects.
- ❌ `go.AddComponent<Image>(); go.AddComponent<RawImage>();`
- ✅ Image on parent, RawImage on child (or vice versa)
- Hit twice in `BuildInfo`: thumbArea (Image+Mask → child RawImage for thumbnail) and overlay (Image background → child TextMeshProUGUI for title)

### CanvasGroup alpha is inherited recursively
`MenuTransition.Apply()` sets `CanvasGroup.alpha = 0` during `FadeInForward`. All children inherit alpha=0 and become invisible for the 0.3s fade-in duration. `activeInHierarchy` stays true (alpha doesn't affect it). The fade-in animation runs in `Update()` — ensure the GameObject stays active throughout.

### Injecting menus: deactivate before AddComponent
```csharp
// ✅ Correct — OnEnable only fires on real transition
go.SetActive(false);
go.transform.SetParent(MenuSystem.instance.transform, false);
go.AddComponent<CollectionsMenu>();

// ❌ Wrong — OnEnable fires immediately during injection
go.transform.SetParent(...);
go.AddComponent<CollectionsMenu>();
go.SetActive(false);
```

### MenuTransition lifecycle
- `Awake()` must ensure `RectTransform` + `CanvasGroup` exist **before** `base.OnEnable()` (MenuTransition) caches them — otherwise `Apply()` gets null refs.
- `OnEnable()` only runs ONCE (guarded by `_colList != null` in `BuildOnce`). If it throws during that one run, UI is permanently broken.
- `OnGotFocus()` runs AFTER `Transition(1,0)` sets alpha to 0. CanvasGroup alpha will be 0 here; it recovers to 1 over fadeInTime via `Update()`.

### Cloning button templates
When `Instantiate`-cloning a native button (e.g. `LevelSelectMenu2.BackButton`), aggressively sanitize:
- `localScale = Vector3.one`
- `CanvasGroup.alpha = 1; CanvasGroup.blocksRaycasts = true`
- Destroy all `Localize` components (I2.Loc may override text)
- Force-enable all `Image`/`RawImage` components
- Force `Button.transition = ColorTint`

## Notes

- `App` lives in `Multiplayer` namespace despite the name.
- `LevelSelectMenu2.campaignTitle` / `subscribedTitle` etc. are text LABELS (TextMeshProUGUI), NOT buttons. Use `showCustomButton`, `showSubscribedButton`, `ShowLocalLevelButton`, `PlayButton` as button reference templates.
- The tab bar container is `topPanel.transform` (AutoNavigation component).
- `MenuTransition` expects `CanvasGroup` on the root GameObject for fade animations.
- Temporary `[BackBtn]` diagnostic logs in `CollectionsMenu.cs` can be removed once UI stabilizes.
