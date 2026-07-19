using System;
using System.Collections.Generic;
using UnityEngine;

namespace LevelCollections;

/// <summary>
/// A single level entry within a collection.
/// </summary>
[Serializable]
public class LevelEntry
{
    /// <summary>
    /// For BuiltIn/EditorPick: the scene name (e.g. "Intro", "Mansion").
    /// For Subscription/LocalWorkshop: the string form of the workshop ID (ulong).
    /// </summary>
    public string LevelId;

    /// <summary>
    /// The source type of this level.
    /// </summary>
    public string LevelType;

    /// <summary>
    /// Optional override title; if empty, the game's own title lookup is used.
    /// </summary>
    public string Title;

    /// <summary>
    /// Resolved WorkshopItemSource enum value.
    /// </summary>
    public WorkshopItemSource ResolvedLevelType
    {
        get
        {
            if (string.IsNullOrEmpty(LevelType))
                return WorkshopItemSource.BuiltIn;
            if (Enum.TryParse(LevelType, out WorkshopItemSource result))
                return result;
            return WorkshopItemSource.BuiltIn;
        }
    }

    /// <summary>
    /// Resolved level ID as ulong (for non-BuiltIn types).
    /// </summary>
    public ulong ResolvedWorkshopId
    {
        get
        {
            if (ulong.TryParse(LevelId, out ulong id))
                return id;
            return 0;
        }
    }
}

/// <summary>
/// A named, ordered collection of levels.
/// </summary>
[Serializable]
public class CollectionDefinition
{
    public string Name;
    public List<LevelEntry> Levels;
}

/// <summary>
/// Root config object for de/serializing the collections JSON file.
/// </summary>
[Serializable]
public class CollectionConfig
{
    public List<CollectionDefinition> Collections;
}
