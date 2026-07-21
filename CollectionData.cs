using System;
using System.Collections.Generic;

namespace LevelCollections;

/// <summary>
/// A named, ordered collection of levels.
/// Each level is identified by a string LevelId; its type is
/// auto-detected at runtime by CollectionManager.ResolveLevelType().
/// </summary>
[Serializable]
public class CollectionDefinition
{
    public string Name;
    public List<string> Levels;

    /// <summary>
    /// Set by ConfigLoader when this collection's definition is invalid
    /// (e.g. missing Name, empty Levels, malformed JSON element).
    /// The UI shows these entries in red with a warning so the user can
    /// fix their config file and press Refresh.
    /// </summary>
    [NonSerialized] public bool IsBroken;
    [NonSerialized] public string ErrorMessage;
}

/// <summary>
/// Root config object for de/serializing the collections JSON file.
/// </summary>
[Serializable]
public class CollectionConfig
{
    public List<CollectionDefinition> Collections;
}
