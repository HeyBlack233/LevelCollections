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
}

/// <summary>
/// Root config object for de/serializing the collections JSON file.
/// </summary>
[Serializable]
public class CollectionConfig
{
    public List<CollectionDefinition> Collections;
}
