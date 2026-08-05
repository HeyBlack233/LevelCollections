# Level Collections

[English](README.md)

## 概述

Level Collections 是一个 Human: Fall Flat 的 [BepInEx](https://github.com/bepinex/bepinex) 插件。它允许你创建自定义关卡合集，并像内置梦境列表一样依次游玩。

## 安装

1. 为游戏安装 [BepInEx](https://github.com/bepinex/bepinex) 并运行一次游戏
2. 前往 `<游戏根目录>/BepInEx/plugins/`
3. 将插件的 `.dll` 文件放入其中
4. 重启游戏即可

## 配置

首次使用插件启动游戏后，`./BepInEx/config/` 下会自动生成 `LevelCollections.json` 配置文件，并包含一个示例合集。

```JSON
{
  "RandomLevelCount": 5,
  "RandomLevelPool": [
    "Intro",
    "Train",
    "Carry",
    "Climb",
    "Break",
    "Siege",
    "Water",
    "Power",
    "Aztec",
    "Halloween",
    "Steam",
    "Ice"
  ],
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

每个合集包含一个 `Name`（在 UI 中显示）和一个 `Levels` 数组，数组元素为 **LevelId** 字符串。插件会根据 LevelId 自动识别关卡类型——你无需手动指定关卡是 BuiltIn、EditorPick 还是 Workshop。

`RandomLevelCount` 和 `RandomLevelPool` 用于 **`lc random`** 控制台命令：从 `RandomLevelPool` 中随机抽取 `RandomLevelCount` 个关卡组成一个临时合集并开始游玩。随机合集**不会**写回配置文件，只在本次游玩中生效。

- `RandomLevelPool` 缺失或为空时，使用全部 12 个常规 BuiltIn 关卡作为默认池；`RandomLevelCount` 缺失或小于 1 时，默认抽取 5 个。
- 抽取前会过滤掉当前不可用的关卡（如未订阅的 Workshop 关卡）；可用关卡不足时按可用数量抽取。

## 使用方法

1. 启动游戏并进入关卡选择界面（Play → Select Level）。
2. 右上角会出现一个 **COLLECTIONS** 按钮，点击即可进入合集菜单。
3. 合集菜单有三个面板：
   - **左侧** — 合集列表。点击或使用方向键选择合集。
   - **中间** — 当前选中合集包含的关卡列表。按**右方向键**从合集列表切换到此处，按**左方向键**返回。
   - **右侧** — 关卡信息面板，显示当前选中关卡的缩略图和标题。
4. **双击**关卡或按**回车**开始游玩。
5. **BACK** 按钮（或 **Escape**）返回关卡选择界面。
6. 完成一个关卡后，合集内的下一关会自动开始。完成最后一关后返回主菜单。
7. **REFRESH** 按钮用于重新加载配置文件。

## 控制台命令

按 **BackQuote**（`` ` ``）或 **F1** 打开游戏内开发者控制台，使用 `lc` 命令组：

| 命令 | 说明 |
|---|---|
| `lc random [seconds]` | 从配置的关卡池中随机抽取关卡组成合集并开始游玩（无需先进入合集菜单）。 |
| `lc restart [seconds]` | 从第一关重新开始当前合集。 |
| `lc skip [seconds]` | 跳过当前关卡，进入当前合集的下一关（最后一关时结束本次合集游玩）。 |
| `lc abort` | 取消一个正在计时的延迟命令。 |

- `[seconds]` 为可选的正整数——命令将在对应秒数后执行。最后 5 秒每秒会在控制台输出一次倒计时提示。
- 若在计时结束前退出合集游玩或切换合集，延迟命令会被**取消**。
- 计时进行中会拒绝新的 `lc restart` / `lc skip` 及带秒数的 `lc random` 命令——请先使用 `lc abort` 取消；直接执行不带秒数的 `lc random` 会立即开始新的随机合集，并自动取消挂起的延迟命令。
- `lc random` 不要求已有合集在进行（可在主菜单直接使用）；`lc restart` / `lc skip` 仅在合集游玩进行中（单人模式）可用。

## 支持的关卡 ID

### BuiltIn

这些是游戏自带关卡。在 JSON 配置中使用 **ID** 列的值：

| ID | 显示名称 |
|---|---|
| `Intro` | 豪宅 |
| `Train` | 火车 |
| `Carry` | 搬运 |
| `Climb` | 山峰 |
| `Break` | 拆除 |
| `Siege` | 城堡 |
| `Water` | 水 |
| `Power` | 发电厂 |
| `Aztec` | 阿芝特克 |
| `Halloween` | 黑暗 |
| `Steam` | 蒸汽 |
| `Ice` | 冰 |
| `Intro_Reprise` | Reprise |
| `Credits` | 制作人名单 |

### EditorPick (Extra Dreams)

由开发者精选的社区关卡。在 JSON 配置中使用 **ID** 列的值：

| ID | 显示名称 |
|---|---|
| `Thermal` | 山顶 |
| `Factory` | 工厂 |
| `Golf` | 高尔夫 |
| `City` | 大都会 |
| `Forest` | 森林 |
| `Lab` | 实验室 |
| `Lumber` | 伐木场 |
| `RedRock` | 红岩 |
| `Tower` | 高塔 |
| `Miniature` | 巨人国 |
| `CopperWorld` | 黄铜世界 |
| `Naval_Ben` | 港口 |
| `OceanAdventure` | 水下世界 |
| `Dockyard` | 船坞 |
| `Museum` | 博物馆 |
| `Hike` | 徒步旅行 |
| `Candyland` | 糖果王国 |
| `Facility` | 后室 |
| `SteamPunk` | 蒸汽朋克派对 |
| `Viking` | 维京 |
| `Anniversary` | 十周年 |

### Workshop 关卡

- **已订阅**的 Workshop 关卡：使用数字形式的 Workshop 文件 ID 字符串（例如 `"123456789"`）。
- **本地** Workshop 关卡：使用本地 workshop 目录中的文件夹名称。

> **注意：** Workshop 关卡的缩略图和标题依赖于 `WorkshopRepository` 加载完毕。如果缩略图或标题显示异常，请先尝试刷新 Subscribed 标签页。

### 重新生成表格

上面的表格可以从游戏自带的本地化数据自动生成（游戏更新加入新关卡时，无需手动编辑）：

```bash
python3 tools/gen_level_table.py          # 输出 LevelCollections.json 中用到的关卡
python3 tools/gen_level_table.py --all    # 输出全部已知关卡（按游戏内顺序）
python3 tools/gen_level_table.py --lang "Chinese Simplified"   # 简体中文名
python3 tools/gen_level_table.py --all -o docs/LEVEL_TABLE.md # 写入文件
```

脚本读取 `<游戏目录>/Human_Data/sharedassets0.assets` 中内嵌的本地化 CSV（与游戏运行时解析的是同一份数据），以及 `BepInEx/config/LevelCollections.json` 中使用的关卡 ID。表格行按游戏自身的 `levels[]` / `editorPickLevels[]` 数组排序，与游戏内关卡顺序一致。无法识别为 BuiltIn/EditorPick 的关卡会标记为 `?` —— 游戏更新推出新关卡时，重点检查这些行。

## 从源码构建

### 前置条件

- .NET SDK（项目目标框架为 `netstandard2.0`）
- 通过 Steam 安装的 **Human: Fall Flat** 游戏副本
- 游戏目录下已安装 **BepInEx 5.x**

### 配置路径

`.csproj` 文件底部硬编码了两个指向游戏目录和 BepInEx 目录的路径。默认使用 Linux Steam 路径——**Windows 用户构建前必须先修改**。

打开 `LevelCollections.csproj`，找到文件底部的 `<PropertyGroup>`，将两个路径改为实际位置：

**Linux**（默认，无需修改）：

```xml
<GAME_MANAGED>$(HOME)/.local/share/Steam/steamapps/common/Human Fall Flat/Human_Data/Managed</GAME_MANAGED>
<BEPINEX_CORE>$(HOME)/.local/share/Steam/steamapps/common/Human Fall Flat/BepInEx/core</BEPINEX_CORE>
```

**Windows**（典型 Steam 路径——如果游戏库在其他盘请相应调整）：

```xml
<GAME_MANAGED>C:\Program Files (x86)\Steam\steamapps\common\Human Fall Flat\Human_Data\Managed</GAME_MANAGED>
<BEPINEX_CORE>C:\Program Files (x86)\Steam\steamapps\common\Human Fall Flat\BepInEx\core</BEPINEX_CORE>
```

也可以不修改文件，直接在命令行传入路径：

```
dotnet build --no-restore -c Release -p:GAME_MANAGED="C:\...\Human_Data\Managed" -p:BEPINEX_CORE="C:\...\BepInEx\core"
```

### 构建

```bash
dotnet build --no-restore -c Release
```

输出 DLL 位于 `bin/Release/netstandard2.0/LevelCollections.dll`。将其复制到 `<游戏目录>/BepInEx/plugins/`。

所有依赖均通过 `.csproj` 中的 `<HintPath>` 直接从游戏目录和 BepInEx 目录引用——无需 NuGet restore。

## 许可证

[GNU LGPL v3](LICENSE)
