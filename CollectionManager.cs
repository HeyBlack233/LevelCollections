using Multiplayer;
using UnityEngine;

namespace LevelCollections;

/// <summary>
/// Runtime singleton that tracks the current collection run state and drives
/// level progression within a collection.
/// </summary>
public class CollectionManager : MonoBehaviour
{
    public static CollectionManager Instance { get; private set; }

    /// <summary>
    /// Whether the player is currently in the middle of a collection run.
    /// </summary>
    public bool IsInCollectionRun { get; private set; }

    /// <summary>
    /// Index of the current collection (into ConfigLoader.Collections).
    /// </summary>
    public int CurrentCollectionIndex { get; private set; }

    /// <summary>
    /// Index of the current level within the collection.
    /// </summary>
    public int CurrentLevelIndex { get; private set; }

    /// <summary>
    /// The CollectionDefinition being played.
    /// </summary>
    public CollectionDefinition CurrentCollection
    {
        get
        {
            var cols = ConfigLoader.Collections;
            if (CurrentCollectionIndex < 0 || CurrentCollectionIndex >= cols.Count)
                return null;
            return cols[CurrentCollectionIndex];
        }
    }

    /// <summary>
    /// The LevelId string of the current level.
    /// </summary>
    public string CurrentLevelId
    {
        get
        {
            var col = CurrentCollection;
            if (col == null || col.Levels == null || CurrentLevelIndex < 0 || CurrentLevelIndex >= col.Levels.Count)
                return null;
            return col.Levels[CurrentLevelIndex];
        }
    }

    /// <summary>
    /// True if the current level is the last one in the collection.
    /// </summary>
    public bool IsLastLevel
    {
        get
        {
            var col = CurrentCollection;
            return col != null && col.Levels != null && CurrentLevelIndex >= col.Levels.Count - 1;
        }
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Begin a collection run starting from the given level index.
    /// </summary>
    public void StartCollectionRun(int collectionIndex, int startLevelIndex = 0)
    {
        var cols = ConfigLoader.Collections;
        if (collectionIndex < 0 || collectionIndex >= cols.Count)
        {
            Plugin.Logger.LogError($"StartCollectionRun: invalid collection index {collectionIndex}");
            return;
        }

        var col = cols[collectionIndex];
        if (col.Levels == null || col.Levels.Count == 0)
        {
            Plugin.Logger.LogError($"StartCollectionRun: collection '{col.Name}' has no levels");
            return;
        }

        if (startLevelIndex < 0 || startLevelIndex >= col.Levels.Count)
            startLevelIndex = 0;

        CurrentCollectionIndex = collectionIndex;
        CurrentLevelIndex = startLevelIndex;
        IsInCollectionRun = true;

        var levelId = col.Levels[CurrentLevelIndex];
        Plugin.Logger.LogInfo($"Starting collection run: '{col.Name}' level {CurrentLevelIndex + 1}/{col.Levels.Count}: {levelId}");

        LaunchLevel(levelId);
    }

    /// <summary>
    /// Advance to the next level in the collection, or end the run if this was the last.
    /// Called by the Harmony patch when a level is passed during a collection run.
    /// </summary>
    public void AdvanceToNextLevel()
    {
        if (!IsInCollectionRun)
            return;

        if (IsLastLevel)
        {
            EndCollectionRun();
            return;
        }

        CurrentLevelIndex++;
        var col = CurrentCollection;
        var levelId = col.Levels[CurrentLevelIndex];
        Plugin.Logger.LogInfo($"Collection run advancing: '{col.Name}' level {CurrentLevelIndex + 1}/{col.Levels.Count}: {levelId}");

        LaunchLevel(levelId);
    }

    /// <summary>
    /// End the current collection run and return to the main menu.
    /// </summary>
    public void EndCollectionRun()
    {
        if (!IsInCollectionRun)
            return;

        var col = CurrentCollection;
        Plugin.Logger.LogInfo($"Collection run complete: '{col?.Name}'");
        IsInCollectionRun = false;

        // Return to main menu
        if (App.instance != null)
        {
            App.instance.PauseLeave();
        }
    }

    /// <summary>
    /// Abort the collection run without finishing (e.g. player manually quits).
    /// </summary>
    public void AbortCollectionRun()
    {
        Plugin.Logger.LogInfo("Collection run aborted.");
        IsInCollectionRun = false;
    }

    // ── Level type auto-detection ────────────────────────────────

    /// <summary>
    /// Auto-detect the WorkshopItemSource from a LevelId string.
    /// </summary>
    public static WorkshopItemSource ResolveLevelType(string levelId)
    {
        if (string.IsNullOrEmpty(levelId))
            return WorkshopItemSource.BuiltIn;

        // BuiltIn: name in our dictionary
        if (_builtInDisplayNameToIndex.ContainsKey(levelId))
            return WorkshopItemSource.BuiltIn;

        // EditorPick: name in Game.instance.editorPickLevels[]
        if (Game.instance != null && Game.instance.editorPickLevels != null)
        {
            foreach (var name in Game.instance.editorPickLevels)
            {
                if (name == levelId)
                    return WorkshopItemSource.EditorPick;
            }
        }

        // LocalWorkshop: path-like string (contains "lvl:" prefix or path separators)
        if (levelId.StartsWith("lvl:") || levelId.Contains("/") || levelId.Contains("\\"))
            return WorkshopItemSource.LocalWorkshop;

        // Subscription: purely numeric workshop ID
        if (ulong.TryParse(levelId, out _))
            return WorkshopItemSource.Subscription;

        // Fallback
        return WorkshopItemSource.BuiltIn;
    }

    /// <summary>
    /// Look up WorkshopLevelMetadata for a level by its LevelId.
    /// Returns null for BuiltIn levels (use the dictionary instead).
    /// </summary>
    public static HumanAPI.WorkshopLevelMetadata ResolveWorkshopMetadata(string levelId)
    {
        var type = ResolveLevelType(levelId);
        if (type == WorkshopItemSource.BuiltIn)
            return null;

        var repo = WorkshopRepository.instance;
        if (repo == null)
            return null;

        var list = repo.levelRepo.BySource(type);
        if (list == null)
            return null;

        foreach (var meta in list)
        {
            switch (type)
            {
                case WorkshopItemSource.Subscription:
                    if (ulong.TryParse(levelId, out ulong wsId) && meta.workshopId == wsId)
                        return meta;
                    break;
                case WorkshopItemSource.EditorPick:
                    if (meta is HumanAPI.BuiltinLevelMetadata blm && blm.internalName == levelId)
                        return meta;
                    break;
                case WorkshopItemSource.LocalWorkshop:
                    if (meta.folder == levelId)
                        return meta;
                    break;
            }
        }

        return null;
    }

    // ── Level launching ───────────────────────────────────────────

    private void LaunchLevel(string levelId)
    {
        if (App.instance == null)
        {
            Plugin.Logger.LogError("App.instance is null, cannot launch level.");
            return;
        }

        var type = ResolveLevelType(levelId);

        switch (type)
        {
            case WorkshopItemSource.BuiltIn:
                ulong levelIndex = FindBuiltInLevelIndex(levelId);
                App.instance.LaunchSinglePlayer(levelIndex, WorkshopItemSource.BuiltIn, 0, 0);
                break;

            case WorkshopItemSource.EditorPick:
                ulong epIndex = FindEditorPickLevelIndex(levelId);
                App.instance.LaunchSinglePlayer(epIndex, WorkshopItemSource.EditorPick, 0, 0);
                break;

            case WorkshopItemSource.LocalWorkshop:
                App.instance.LaunchCustomLevel(levelId, WorkshopItemSource.LocalWorkshop, 0, 0);
                break;

            case WorkshopItemSource.Subscription:
                if (ulong.TryParse(levelId, out ulong wsId))
                    App.instance.LaunchSinglePlayer(wsId, WorkshopItemSource.Subscription, 0, 0);
                else
                    Plugin.Logger.LogError($"Subscription level '{levelId}' is not a valid ulong.");
                break;

            default:
                Plugin.Logger.LogError($"Unsupported level type: {type}");
                break;
        }
    }

    // ── Index lookups ─────────────────────────────────────────────

    /// <summary>
    /// Maps Display Name (internalName from WorkshopRepository) →
    /// index into Game.instance.levels[] (scene names).
    /// Two base levels have divergent names: "Train"→scene "Push", "Water"→scene "River".
    /// "Intro_Reprise" (index 12) and "Credits" (index 13) are in levels[] but NOT
    /// registered in WorkshopRepository; their scene names and display names are identical.
    /// </summary>
    private static readonly System.Collections.Generic.Dictionary<string, int> _builtInDisplayNameToIndex = new System.Collections.Generic.Dictionary<string, int>
    {
        { "Intro",         0  },
        { "Train",         1  },  // scene: "Push"
        { "Carry",         2  },
        { "Climb",         3  },
        { "Break",         4  },
        { "Siege",         5  },
        { "Water",         6  },  // scene: "River"
        { "Power",         7  },
        { "Aztec",         8  },
        { "Halloween",     9  },
        { "Steam",         10 },
        { "Ice",           11 },
        { "Intro_Reprise", 12 },
        { "Credits",       13 },
    };

    private ulong FindBuiltInLevelIndex(string displayName)
    {
        if (_builtInDisplayNameToIndex.TryGetValue(displayName, out int idx))
            return (ulong)idx;

        Plugin.Logger.LogWarning($"Built-in level '{displayName}' not found in display name map; launching with index 0.");
        return 0;
    }

    private ulong FindEditorPickLevelIndex(string sceneName)
    {
        if (Game.instance != null && Game.instance.editorPickLevels != null)
        {
            for (int i = 0; i < Game.instance.editorPickLevels.Length; i++)
            {
                if (Game.instance.editorPickLevels[i] == sceneName)
                    return (ulong)i;
            }
        }
        return 0;
    }
}
