using HarmonyLib;
using Multiplayer;

namespace LevelCollections;

/// <summary>
/// Harmony patches for methods that CAN be reliably matched.
/// Button injection is handled by Bootstrapper (no Harmony dependency)
/// because private/override method matching is unreliable in this environment.
/// </summary>
internal static class HarmonyPatches
{
    public static void Apply()
    {
        var harmony = new Harmony("LevelCollections");

        // ── App.StartNextLevel prefix ──────────────────────────────
        // Manual patch — avoids PatchAll auto-discovery issues.

        var original = AccessTools.Method(typeof(App), nameof(App.StartNextLevel));
        var prefix = AccessTools.Method(typeof(HarmonyPatches), nameof(StartNextLevel_Prefix));

        if (original != null && prefix != null)
        {
            harmony.Patch(original, new HarmonyMethod(prefix));
            Plugin.Logger.LogInfo("Harmony: patched App.StartNextLevel.");
        }
        else
        {
            Plugin.Logger.LogError(
                $"Harmony: failed to find methods. original={original != null}, prefix={prefix != null}");
        }
    }

    private static bool StartNextLevel_Prefix(ulong level, int checkpoint)
    {
        var mgr = CollectionManager.Instance;
        if (mgr == null || !mgr.IsInCollectionRun)
            return true;

        Plugin.Logger.LogInfo($"Collection run: intercepting StartNextLevel({level}, {checkpoint}), advancing.");
        mgr.AdvanceToNextLevel();
        return false;
    }
}
