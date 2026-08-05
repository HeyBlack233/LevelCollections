# Level Collections

[English](README.md) · [中文](README_zh.md)

## 概要

Level Collections は、Human: Fall Flat 用の [BepInEx](https://github.com/bepinex/bepinex) プラグインです。カスタムのレベルコレクションを作成し、内蔵のドリームリストと同じように順番にプレイすることができます。

## インストール

1. [BepInEx](https://github.com/bepinex/bepinex) をゲームにインストールし、一度ゲームを起動する
2. `<ゲームのルートディレクトリ>/BepInEx/plugins/` に移動する
3. プラグインの `.dll` ファイルをその中に置く
4. ゲームを再起動すれば完了

## 設定

プラグインを入れて初回起動すると、`./BepInEx/config/` に `LevelCollections.json` という JSON ファイルが作成され、サンプルコレクションが含まれています。

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

各コレクションは `Name`(UI に表示される名前)と、**LevelId** 文字列の配列である `Levels` を持ちます。プラグインは LevelId からレベルタイプを自動判別するため、レベルが BuiltIn・EditorPick・Workshop のどれかを指定する必要はありません。

`RandomLevelCount` と `RandomLevelPool` は **`lc random`** コンソールコマンドを制御します: `RandomLevelPool` から `RandomLevelCount` 個のレベルをランダムに抽選して一時的なコレクションを作り、プレイを開始します。ランダムコレクションは設定ファイルに**書き戻されません** — そのランの間だけ存在します。

- `RandomLevelPool` が無い/空の場合、通常の BuiltIn レベル 12 個がデフォルトのプールとして使われます。`RandomLevelCount` が無い/1 未満の場合、デフォルトは 5 です。
- 抽選の前に現在利用できないレベル(例: 購読を解除した Workshop レベル)は除外されます。利用可能なレベルが要求数より少ない場合は、そのすべてが使われます。

## 使い方

1. ゲームを起動し、レベル選択メニュー(Play → Select Level)を開きます。
2. 右上に新しい **COLLECTIONS** ボタンが表示されるので、クリックしてコレクションメニューに入ります。
3. コレクションメニューには 3 つのパネルがあります:
   - **左** — コレクションリスト。クリックまたは矢印キーでコレクションを選択します。
   - **中央** — 選択中のコレクションに含まれるレベルのリスト。**右矢印キー**でコレクションリストからここにフォーカスを移動し、**左矢印キー**で戻ります。
   - **右** — 現在選択中のレベルのサムネイルとタイトルを表示する情報パネル。
4. レベルを**ダブルクリック**するか **Enter** キーを押すとプレイが始まります。
5. **BACK** ボタン(または **Escape**)でレベル選択メニューに戻ります。
6. レベルをクリアすると、コレクション内の次のレベルが自動的に始まります。最後のレベルを終えるとメインメニューに戻ります。
7. **REFRESH** ボタンで設定を再読み込みします。
8. プラグインの UI テキスト(**COLLECTIONS** ボタン、メニュー/パネルのタイトル、**BACK** / **REFRESH** / **START**)はゲームの言語に追従します: 簡体字中国語(`合辑`、`返回` …)と日本語(`コレクション`、`戻る` …)に対応し、それ以外の言語では英語にフォールバックします。オプション → 言語 で言語を変更すると、テキストは即座に更新されます。

## コンソールコマンド

**BackQuote**(`` ` ``)または **F1** でゲーム内開発者コンソールを開き、`lc` コマンドグループを使用します:

| コマンド | 説明 |
|---|---|
| `lc random [seconds]` | 設定のレベルプールからランダムなコレクションを抽選してプレイを開始します(実行中のランは不要)。 |
| `lc restart [seconds]` | 現在のコレクションを最初のレベルから再開します。 |
| `lc skip [seconds]` | 現在のレベルをスキップして次のレベルを読み込みます(最後のレベルではランを完了します)。 |
| `lc abort` | 保留中の遅延コマンドをキャンセルします。 |

- `[seconds]` は省略可能な正の整数です — 指定した秒数後にコマンドが実行されます。最後の 5 秒間は毎秒 1 回、コンソールにカウントダウンが表示されます。
- 遅延が経過する前にコレクションのランが終了するかコレクションを切り替えると、遅延コマンドは**キャンセル**されます。
- 遅延コマンドが保留中は、新しい `lc restart` / `lc skip` コマンドと遅延付きの `lc random` は拒否されます — 先に `lc abort` でキャンセルしてください。遅延なしの `lc random` は即座に新しいランダムランを開始し、保留中の遅延をキャンセルします。
- `lc random` は実行中のランを必要としません(メインメニューから直接使用できます)。`lc restart` / `lc skip` はコレクションのラン実行中(シングルプレイヤー)のみ動作します。

## 対応レベル ID

### BuiltIn

これらは基本ゲームのレベルです。JSON 設定では **ID** 列の値を使用します:

| ID | 表示名 |
|---|---|
| `Intro` | ミュージアム |
| `Train` | トレイン |
| `Carry` | オハコビ |
| `Climb` | マウンテン |
| `Break` | コウジゲンバ |
| `Siege` | キャッスル |
| `Water` | ウォーター |
| `Power` | ハツデンショ |
| `Aztec` | アステカ |
| `Halloween` | ダーク |
| `Steam` | スチーム |
| `Ice` | アイス |
| `Intro_Reprise` | Reprise |
| `Credits` | クレジット |

### EditorPick (Extra Dreams)

開発者が厳選したコミュニティ製レベル。JSON 設定では **ID** 列の値を使用します:

| ID | 表示名 |
|---|---|
| `Thermal` | サーマル |
| `Factory` | ファクトリー |
| `Golf` | ゴルフ |
| `City` | シティ |
| `Forest` | フォレスト |
| `Lab` | ラボラトリー |
| `Lumber` | ランバー |
| `RedRock` | レッドロック |
| `Tower` | タワー |
| `Miniature` | ミニチュア |
| `CopperWorld` | カッパーワールド |
| `Naval_Ben` | 港 |
| `OceanAdventure` | アンダーウォーター |
| `Dockyard` | ドックヤード |
| `Museum` | 美術館 |
| `Hike` | ハイキング |
| `Candyland` | キャンディランド |
| `Facility` | Test Chamber |
| `SteamPunk` | スチームパンクパーティー |
| `Viking` | ヴァイキング |
| `Anniversary` | 10周年記念 |

### Workshop レベル

- **購読中**の Workshop レベル: 数字の Workshop ファイル ID を文字列として使用します(例: `"123456789"`)。
- **ローカル** Workshop レベル: ローカルの workshop ディレクトリ内のフォルダ名を使用します。

> **注:** Workshop レベルのサムネイルとタイトルは、`WorkshopRepository` がメタデータの読み込みを完了しているかどうかに依存します。サムネイルやタイトルが表示されない場合は、まず Subscribed タブを更新してみてください。

### テーブルの再生成

上記のテーブルは、ゲーム自身のローカライズデータから自動生成できます(ゲームアップデートで新しいレベルが追加されても手動編集は不要):

```bash
python3 tools/gen_level_table.py          # LevelCollections.json で使用されているレベル
python3 tools/gen_level_table.py --all    # 既知の全レベル(ゲーム内の順序)
python3 tools/gen_level_table.py --lang "Chinese Simplified"   # 簡体字中国語名
python3 tools/gen_level_table.py --lang Japanese               # 日本語名
python3 tools/gen_level_table.py --all -o docs/LEVEL_TABLE.md # ファイルに書き出す
```

スクリプトは `<ゲーム>/Human_Data/sharedassets0.assets` に埋め込まれたローカライズ CSV(ゲームが実行時に解析するのと同じテーブル)と、`BepInEx/config/LevelCollections.json` の使用中 ID を読み取ります。行はゲーム自身の `levels[]` / `editorPickLevels[]` 配列の順に並ぶため、ゲーム内のレベル順を反映します。BuiltIn/EditorPick として認識されないレベルは `?` でマークされます — ゲームアップデートで新しいレベルが追加されたら、これらの行を確認してください。

## ソースからのビルド

### 前提条件

- .NET SDK(プロジェクトのターゲットは `netstandard2.0`)
- Steam でインストールした **Human: Fall Flat** のコピー
- ゲームディレクトリにインストールされた **BepInEx 5.x**

### パスの設定

`.csproj` ファイルには、ゲームと BepInEx のディレクトリを指す 2 つのパスがハードコードされています。デフォルトは Linux の Steam パスです — **Windows ユーザーはビルド前に調整する必要があります**。

`LevelCollections.csproj` を開き、末尾の `<PropertyGroup>` を探して、2 つのパスを自分の環境に合わせて編集します:

**Linux**(デフォルト、変更不要):

```xml
<GAME_MANAGED>$(HOME)/.local/share/Steam/steamapps/common/Human Fall Flat/Human_Data/Managed</GAME_MANAGED>
<BEPINEX_CORE>$(HOME)/.local/share/Steam/steamapps/common/Human Fall Flat/BepInEx/core</BEPINEX_CORE>
```

**Windows**(一般的な Steam パス — ライブラリが別ドライブにある場合は調整してください):

```xml
<GAME_MANAGED>C:\Program Files (x86)\Steam\steamapps\common\Human Fall Flat\Human_Data\Managed</GAME_MANAGED>
<BEPINEX_CORE>C:\Program Files (x86)\Steam\steamapps\common\Human Fall Flat\BepInEx\core</BEPINEX_CORE>
```

または、ファイルを編集せずにコマンドラインでパスを渡すこともできます:

```
dotnet build --no-restore -c Release -p:GAME_MANAGED="C:\...\Human_Data\Managed" -p:BEPINEX_CORE="C:\...\BepInEx\core"
```

### ビルド

```bash
dotnet build --no-restore -c Release
```

出力 DLL は `bin/Release/netstandard2.0/LevelCollections.dll` です。`<ゲーム>/BepInEx/plugins/` にコピーしてください。

すべての依存関係は `.csproj` の `<HintPath>` でゲームと BepInEx のディレクトリから直接参照されています — NuGet restore は不要です。

## ライセンス

[GNU LGPL v3](LICENSE)
