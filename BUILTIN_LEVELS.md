# Built-in Level IDs

Human Fall Flat 内置关卡有两套 ID 系统：

- **Display Name**（`internalName`）：来自 `WorkshopRepository.AddLevel()`，用于缩略图 (`res:LevelImages/<name>`) 和本地化 (`LEVEL/<name>`)。
- **Scene Name**（`levels[]`）：Unity 场景文件名，用于实际加载关卡。

本插件统一使用 **Display Name** 作为 `LevelId`，内部自动映射到正确的场景索引。

---

## Base Levels (BuiltIn)

共 14 个基础关卡。其中 **2 个关卡** 的 Display Name 与 Scene Name 不一致（⚠️ 标记），**2 个关卡** 仅存在于 `levels[]` 中、未在 `WorkshopRepository` 注册（🔸 标记）。

| # | Workshop ID | Display Name | Scene Name | 备注 |
|---|-------------|-------------|------------|------|
| 0 | 0 | `Intro` | `Intro` | |
| 1 | 1 | `Train` | `Push` | ⚠️ 名称不一致 |
| 2 | 2 | `Carry` | `Carry` | |
| 3 | 3 | `Climb` | `Climb` | |
| 4 | 4 | `Break` | `Break` | |
| 5 | 5 | `Siege` | `Siege` | |
| 6 | 6 | `Water` | `River` | ⚠️ 名称不一致 |
| 7 | 7 | `Power` | `Power` | |
| 8 | 8 | `Aztec` | `Aztec` | |
| 9 | 9 | `Halloween` | `Halloween` | |
| 10 | 10 | `Steam` | `Steam` | |
| 11 | 11 | `Ice` | `Ice` | |
| 12 | — | `Intro_Reprise` | `Intro_Reprise` | 🔸 仅在 `levels[]` 中 |
| 13 | — | `Credits` | `Credits` | 🔸 仅在 `levels[]` 中 |

> 🔸 `Intro_Reprise`（索引 12）和 `Credits`（索引 13）不在 `WorkshopRepository` 中注册（无独立的 Workshop ID 和 `internalName`），但它们存在于 `Game.levels[]` 数组中。在战役流程中，通关 Ice 后依次进入 `Intro_Reprise` → `Credits`。Scene Name 即为 Display Name。

配置文件中 `LevelType` 使用 `"BuiltIn"`。

---

## Editor Pick Levels

共 20 个编辑精选关卡。Editor Pick 关卡的 Display Name 与 Scene Name 一致，无分歧。

| # | Workshop ID | Name |
|---|-------------|------|
| 0 | 0 | `Thermal` |
| 1 | 1 | `Factory` |
| 2 | 2 | `Golf` |
| 3 | 3 | `City` |
| 4 | 4 | `Forest` |
| 5 | 5 | `Lab` |
| 6 | 6 | `Lumber` |
| 7 | 7 | `RedRock` |
| 8 | 8 | `Tower` |
| 9 | 9 | `Miniature` |
| 10 | 10 | `CopperWorld` |
| 11 | 12 | `Naval_Ben` |
| 12 | 13 | `OceanAdventure` |
| 13 | 14 | `Dockyard` |
| 14 | 15 | `Museum` |
| 15 | 16 | `Hike` |
| 16 | 17 | `Candyland` |
| 17 | 18 | `Facility` |
| 18 | 19 | `SteamPunk` |
| 19 | 20 | `Viking` |

> 注意：Editor Pick 的 Workshop ID 11 被跳过，因此 `Naval_Ben` 的 Workshop ID 为 12。

配置文件中 `LevelType` 使用 `"EditorPick"`。

---

## 参考

- 源码：`WorkshopRepository.cs:410-460`（`AddLevel` / `AddEditorPickLevel`）
- `levels[]` 数组：`sharedassets0.assets`（运行时数据）
- 映射字典：`CollectionManager.cs` — `_builtInDisplayNameToIndex`
