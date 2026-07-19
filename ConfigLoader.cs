using System.Collections.Generic;
using System.IO;
using BepInEx;
using HumanAPI;
using UnityEngine;

namespace LevelCollections;

/// <summary>
/// Loads and provides access to the collection configuration file.
/// Uses MiniJSON for serialization because Unity 2017's JsonUtility
/// does not handle nested List&lt;T&gt; correctly.
/// </summary>
public static class ConfigLoader
{
    private static CollectionConfig _config;
    private static string _configPath;

    private static readonly CollectionDefinition[] _emptyCollections = new CollectionDefinition[0];

    public static IReadOnlyList<CollectionDefinition> Collections
    {
        get
        {
            if (_config == null || _config.Collections == null)
                return _emptyCollections;
            return _config.Collections;
        }
    }

    public static void Load()
    {
        _configPath = Path.Combine(Paths.ConfigPath, "LevelCollections.json");

        if (!File.Exists(_configPath))
        {
            CreateDefaultConfig();
        }

        try
        {
            string json = File.ReadAllText(_configPath);
            _config = DeserializeConfig(json);
            if (_config == null || _config.Collections == null)
            {
                Plugin.Logger.LogWarning("LevelCollections config parsed but was null or had no Collections; using empty list.");
                _config = new CollectionConfig { Collections = new List<CollectionDefinition>() };
            }
            else
            {
                Plugin.Logger.LogInfo($"LevelCollections: loaded {_config.Collections.Count} collection(s).");
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to load LevelCollections config: {ex.Message}");
            _config = new CollectionConfig { Collections = new List<CollectionDefinition>() };
        }
    }

    /// <summary>
    /// Try to get a title and thumbnail for a level entry.
    /// For BuiltIn levels: look up via ScriptLocalization and HFFResources.
    /// </summary>
    public static void ResolveLevelInfo(LevelEntry entry, out string title, out Texture2D thumbnail)
    {
        title = null;
        thumbnail = null;

        if (!string.IsNullOrEmpty(entry.Title))
        {
            title = entry.Title;
        }

        if (entry.ResolvedLevelType == WorkshopItemSource.BuiltIn)
        {
            if (string.IsNullOrEmpty(title) && Game.instance != null && Game.instance.levels != null)
            {
                title = "LEVEL/" + entry.LevelId;
            }
            if (HFFResources.instance != null)
            {
                thumbnail = HFFResources.instance.FindTextureResource("LevelImages/" + entry.LevelId);
            }
        }
    }

    // ── Serialization via MiniJSON ─────────────────────────────────

    private static void CreateDefaultConfig()
    {
        var defaultConfig = new CollectionConfig
        {
            Collections = new List<CollectionDefinition>
            {
                new CollectionDefinition
                {
                    Name = "Example Collection",
                    Levels = new List<LevelEntry>
                    {
                        new LevelEntry { LevelId = "Intro", LevelType = "BuiltIn" },
                        new LevelEntry { LevelId = "Mansion", LevelType = "BuiltIn" },
                        new LevelEntry { LevelId = "Train", LevelType = "BuiltIn" },
                        new LevelEntry { LevelId = "Carry", LevelType = "BuiltIn" },
                        new LevelEntry { LevelId = "Climb", LevelType = "BuiltIn" },
                        new LevelEntry { LevelId = "Halloween", LevelType = "BuiltIn" },
                        new LevelEntry { LevelId = "Steam", LevelType = "BuiltIn" },
                        new LevelEntry { LevelId = "Ice", LevelType = "BuiltIn" },
                    }
                },
                new CollectionDefinition
                {
                    Name = "Short & Sweet",
                    Levels = new List<LevelEntry>
                    {
                        new LevelEntry { LevelId = "Intro", LevelType = "BuiltIn" },
                        new LevelEntry { LevelId = "Train", LevelType = "BuiltIn" },
                        new LevelEntry { LevelId = "Halloween", LevelType = "BuiltIn" },
                    }
                }
            }
        };

        try
        {
            string json = MiniJSON.Serialize(defaultConfig);
            // Pretty-print: re-parse and re-serialize with indentation
            object parsed = MiniJSON.Deserialize(json);
            string pretty = SerializePretty(parsed, 0);
            File.WriteAllText(_configPath, pretty);
            Plugin.Logger.LogInfo($"Created default LevelCollections config at: {_configPath}");
        }
        catch (System.Exception ex)
        {
            Plugin.Logger.LogError($"Failed to create default config: {ex.Message}");
        }
    }

    private static CollectionConfig DeserializeConfig(string json)
    {
        object parsed = MiniJSON.Deserialize(json);
        if (parsed is Dictionary<string, object> root)
        {
            var config = new CollectionConfig();
            if (root.TryGetValue("Collections", out object colsObj) && colsObj is List<object> colsList)
            {
                config.Collections = new List<CollectionDefinition>();
                foreach (object colObj in colsList)
                {
                    if (colObj is Dictionary<string, object> colDict)
                    {
                        var col = new CollectionDefinition();
                        if (colDict.TryGetValue("Name", out object nameObj))
                            col.Name = nameObj as string ?? "";
                        if (colDict.TryGetValue("Levels", out object lvlsObj) && lvlsObj is List<object> lvlsList)
                        {
                            col.Levels = new List<LevelEntry>();
                            foreach (object lvlObj in lvlsList)
                            {
                                if (lvlObj is Dictionary<string, object> lvlDict)
                                {
                                    var entry = new LevelEntry();
                                    if (lvlDict.TryGetValue("LevelId", out object idObj))
                                        entry.LevelId = idObj as string ?? "";
                                    if (lvlDict.TryGetValue("LevelType", out object typeObj))
                                        entry.LevelType = typeObj as string ?? "";
                                    if (lvlDict.TryGetValue("Title", out object titleObj))
                                        entry.Title = titleObj as string ?? "";
                                    col.Levels.Add(entry);
                                }
                            }
                        }
                        config.Collections.Add(col);
                    }
                }
            }
            return config;
        }
        return null;
    }

    // ── Pretty-printer ─────────────────────────────────────────────

    private static string SerializePretty(object obj, int indent)
    {
        var sb = new System.Text.StringBuilder();
        SerializePrettyValue(obj, indent, sb);
        return sb.ToString();
    }

    private static void SerializePrettyValue(object obj, int indent, System.Text.StringBuilder sb)
    {
        if (obj == null)
        {
            sb.Append("null");
        }
        else if (obj is Dictionary<string, object> dict)
        {
            sb.Append("{\n");
            bool first = true;
            foreach (var kvp in dict)
            {
                if (!first) sb.Append(",\n");
                first = false;
                Indent(sb, indent + 1);
                sb.Append('"');
                sb.Append(EscapeString(kvp.Key));
                sb.Append("\": ");
                SerializePrettyValue(kvp.Value, indent + 1, sb);
            }
            sb.Append('\n');
            Indent(sb, indent);
            sb.Append('}');
        }
        else if (obj is List<object> list)
        {
            if (list.Count == 0)
            {
                sb.Append("[]");
                return;
            }
            sb.Append("[\n");
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(",\n");
                Indent(sb, indent + 1);
                SerializePrettyValue(list[i], indent + 1, sb);
            }
            sb.Append('\n');
            Indent(sb, indent);
            sb.Append(']');
        }
        else if (obj is string s)
        {
            sb.Append('"');
            sb.Append(EscapeString(s));
            sb.Append('"');
        }
        else if (obj is bool b)
        {
            sb.Append(b ? "true" : "false");
        }
        else if (obj is long l)
        {
            sb.Append(l);
        }
        else if (obj is int i32)
        {
            sb.Append(i32);
        }
        else if (obj is double d)
        {
            sb.Append(d.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        }
        else
        {
            sb.Append(obj.ToString());
        }
    }

    private static void Indent(System.Text.StringBuilder sb, int level)
    {
        for (int i = 0; i < level * 2; i++)
            sb.Append(' ');
    }

    private static string EscapeString(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new System.Text.StringBuilder();
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }
}
