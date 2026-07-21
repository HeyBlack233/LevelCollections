# Level Collections

[中文](README_zh.md)

## Overview

Level Collections is a [BepInEx](https://github.com/bepinex/bepinex) plugin for Human: Fall Flat. It provides the ability to create custom level collections and play through them just like the built-in dreams list.

## Installation

1. Install [BepInEx](https://github.com/bepinex/bepinex) to your game and launch the game once
2. Navigate to `<game_root_directory>/BepInEx/plugins/`
3. Put the plugin's `.dll` file inside
4. Restart the game, and that's it

## Configuration

After the first launch with the plugin, a JSON file `LevelCollections.json` will be created in `./BepInEx/config/` , with an example collection.

```JSON
{
  "Collections": [
    {
      "Name": "Example Collection",
      "Levels": [
        "Intro",
        "Water",
        "Train",
        "Carry",
        "Climb",
        "Halloween",
        "Steam",
        "Ice"
      ]
    }
  ]
}
```

Each collection has a `Name` (displayed in the UI) and a `Levels` array of **LevelId** strings. The plugin auto-detects the level type from the LevelId — you don't need to specify whether a level is BuiltIn, EditorPick, or Workshop.

## Usage

1. Launch the game and navigate to the level select menu (Play → Select Level).
2. A new **COLLECTIONS** button appears at the top-right, click it to enter the collections menu.
3. The Collections menu has three panels:
   - **Left** — your collection list. Click or use arrow keys to select a collection.
   - **Middle** — the levels in the selected collection. Press **Right Arrow** to move focus here from the collection list, **Left Arrow** to go back.
   - **Right** — level info panel showing the thumbnail and title of the currently selected level.
4. **Double-click** a level or press **Enter** to start playing it.
5. **BACK** button (or **Escape**) returns to the level select menu.
6. When you complete a level, the next one in the collection starts automatically. Finishing the last level takes you back to the main menu.
7. **REFRESH** button to reload config.

## Supported Level IDs

### BuiltIn

These are the base-game levels. Use the **ID** in your JSON config:

| ID | Display Name |
|---|---|
| `Intro` | Mansion |
| `Train` | Train |
| `Carry` | Carry |
| `Climb` | Mountain |
| `Break` | Demolition |
| `Siege` | Castle |
| `Water` | Water |
| `Power` | Power Plant |
| `Aztec` | Aztec |
| `Halloween` | Dark |
| `Steam` | Steam |
| `Ice` | Ice |
| `Intro_Reprise` | Reprise |
| `Credits` | Credits |

### EditorPick (Extra Dreams)

Community-made levels curated by the developers. Use the **ID** in your JSON config:

| ID | Display Name |
|---|---|
| `Thermal` | Thermal |
| `Factory` | Factory |
| `Golf` | Golf |
| `City` | City |
| `Forest` | Forest |
| `Lab` | Laboratory |
| `Lumber` | Lumber |
| `RedRock` | Red Rock |
| `Tower` | Tower |
| `Miniature` | Miniature |
| `CopperWorld` | Copper World |
| `Naval_Ben` | Port |
| `OceanAdventure` | Underwater |
| `Dockyard` | Dockyard |
| `Museum` | Museum |
| `Hike` | Hike |
| `Candyland` | Candyland |
| `Facility` | Test Chamber |
| `SteamPunk` | Steampunk Party |
| `Viking` | Viking |

### Workshop levels

- **Subscribed** Workshop levels: use the numeric Workshop file ID as a string (e.g. `"123456789"`).
- **Local Workshop** levels: use the folder name from your local workshop directory.

> **Note:** Workshop thumbnails and titles depend on `WorkshopRepository` having finished loading its metadata. If a workshop thumbnail or title shows as missing, try refreshing the Subscribed tab first.

## Building from Source

### Prerequisites

- .NET SDK (the project targets `netstandard2.0`)
- A copy of **Human: Fall Flat** installed via Steam
- **BepInEx 5.x** installed in the game directory

### Configure Paths

The `.csproj` file hardcodes two paths pointing to the game and BepInEx directories. The defaults use the Linux Steam path — **Windows users must adjust them before building**.

Open `LevelCollections.csproj` and locate the `<PropertyGroup>` at the bottom. Edit the two paths to match your system:

**Linux** (default, no changes needed):

```xml
<GAME_MANAGED>$(HOME)/.local/share/Steam/steamapps/common/Human Fall Flat/Human_Data/Managed</GAME_MANAGED>
<BEPINEX_CORE>$(HOME)/.local/share/Steam/steamapps/common/Human Fall Flat/BepInEx/core</BEPINEX_CORE>
```

**Windows** (typical Steam path — adjust if your library is on a different drive):

```xml
<GAME_MANAGED>C:\Program Files (x86)\Steam\steamapps\common\Human Fall Flat\Human_Data\Managed</GAME_MANAGED>
<BEPINEX_CORE>C:\Program Files (x86)\Steam\steamapps\common\Human Fall Flat\BepInEx\core</BEPINEX_CORE>
```

Alternatively, you can pass the paths on the command line without editing the file:

```
dotnet build --no-restore -c Release -p:GAME_MANAGED="C:\...\Human_Data\Managed" -p:BEPINEX_CORE="C:\...\BepInEx\core"
```

### Build

```bash
dotnet build --no-restore -c Release
```

The output DLL is at `bin/Release/netstandard2.0/LevelCollections.dll`. Copy it to `<game>/BepInEx/plugins/`.

All dependencies are referenced directly from the game and BepInEx directories via `<HintPath>` in the `.csproj` — no NuGet restore is needed.

## License

[GNU LGPL v3](LICENSE)
