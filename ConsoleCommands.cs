using Multiplayer;
using UnityEngine;

namespace LevelCollections;

/// <summary>
/// Registers the "lc" command group in the game's developer console
/// (opened with BackQuote / F1).
///
///     lc restart  — restart the current collection from its first level
///     lc skip     — skip the current level and load the next one
///
/// Both commands only work while a collection run is in progress (single player).
/// </summary>
internal static class ConsoleCommands
{
    private const string HelpText =
        "lc <command>\r\n" +
        "\trestart - restart the current collection from level 1\r\n" +
        "\tskip - skip the current level and advance to the next one";

    public static void Register()
    {
        // Shell is the game's dev console (BackQuote/F1). RegisterCommand only
        // touches Shell's static command table, so it is safe to call before
        // the Shell scene object exists (Shell.Awake() registers "?"/"help" too).
        Shell.RegisterCommand("lc", OnLcCommand, HelpText);
        Plugin.Logger.LogInfo("LevelCollections: registered 'lc' console commands (restart, skip).");
    }

    private static void OnLcCommand(string args)
    {
        if (string.IsNullOrEmpty(args))
        {
            PrintHelp();
            return;
        }

        var mgr = CollectionManager.Instance;
        if (mgr == null || !mgr.IsInCollectionRun)
        {
            Print("lc: no collection run in progress. Start one from the Collections menu first.");
            return;
        }

        switch (args.ToLowerInvariant())
        {
            case "restart":
                Restart(mgr);
                break;
            case "skip":
                Skip(mgr);
                break;
            default:
                PrintHelp();
                break;
        }
    }

    private static void Restart(CollectionManager mgr)
    {
        var col = mgr.CurrentCollection;
        Print($"lc: restarting collection '{(col != null ? col.Name : "?")}' from level 1.");
        mgr.StartCollectionRun(mgr.CurrentCollectionIndex, 0);
    }

    private static void Skip(CollectionManager mgr)
    {
        var col = mgr.CurrentCollection;
        var curLevel = mgr.CurrentLevelId;
        int curIndex = mgr.CurrentLevelIndex;
        int total = col != null && col.Levels != null ? col.Levels.Count : 0;

        if (mgr.IsLastLevel)
        {
            Print($"lc: '{curLevel}' is the last level ({curIndex + 1}/{total}); completing the run.");
            mgr.AdvanceToNextLevel(); // ends the run and returns to the main menu
            return;
        }

        Print($"lc: skipping '{curLevel}' (level {curIndex + 1}/{total}).");
        mgr.AdvanceToNextLevel();
    }

    private static void PrintHelp() => Print(HelpText);

    /// <summary>
    /// Shell.Print requires Shell.instance to be alive; fall back to the plugin
    /// logger if the console object isn't available yet (Unity fake-null check).
    /// </summary>
    private static void Print(string message)
    {
        if (Shell.instance != null && Shell.instance)
            Shell.Print(message);
        else
            Plugin.Logger.LogInfo(message);
    }
}
