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
    /// The current LevelEntry being played.
    /// </summary>
    public LevelEntry CurrentLevelEntry
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
    /// Begin a collection run starting from the first level.
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

        var entry = col.Levels[CurrentLevelIndex];
        Plugin.Logger.LogInfo($"Starting collection run: '{col.Name}' level {CurrentLevelIndex + 1}/{col.Levels.Count}: {entry.LevelId}");

        LaunchLevel(entry);
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
        var entry = col.Levels[CurrentLevelIndex];
        Plugin.Logger.LogInfo($"Collection run advancing: '{col.Name}' level {CurrentLevelIndex + 1}/{col.Levels.Count}: {entry.LevelId}");

        LaunchLevel(entry);
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

    private void LaunchLevel(LevelEntry entry)
    {
        if (App.instance == null)
        {
            Plugin.Logger.LogError("App.instance is null, cannot launch level.");
            return;
        }

        switch (entry.ResolvedLevelType)
        {
            case WorkshopItemSource.BuiltIn:
                // Find the index of this level in Game.instance.levels
                ulong levelIndex = FindBuiltInLevelIndex(entry.LevelId);
                App.instance.LaunchSinglePlayer(levelIndex, WorkshopItemSource.BuiltIn, 0, 0);
                break;

            case WorkshopItemSource.EditorPick:
                // Find the index in editor pick levels
                ulong epIndex = FindEditorPickLevelIndex(entry.LevelId);
                App.instance.LaunchSinglePlayer(epIndex, WorkshopItemSource.EditorPick, 0, 0);
                break;

            case WorkshopItemSource.LocalWorkshop:
                App.instance.LaunchCustomLevel(entry.LevelId, WorkshopItemSource.LocalWorkshop, 0, 0);
                break;

            case WorkshopItemSource.Subscription:
                App.instance.LaunchSinglePlayer(entry.ResolvedWorkshopId, WorkshopItemSource.Subscription, 0, 0);
                break;

            default:
                Plugin.Logger.LogError($"Unsupported level type: {entry.ResolvedLevelType}");
                break;
        }
    }

    private ulong FindBuiltInLevelIndex(string sceneName)
    {
        if (Game.instance != null && Game.instance.levels != null)
        {
            for (int i = 0; i < Game.instance.levels.Length; i++)
            {
                if (Game.instance.levels[i] == sceneName)
                    return (ulong)i;
            }
        }
        Plugin.Logger.LogWarning($"Built-in level '{sceneName}' not found in Game.instance.levels; launching with index 0.");
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
