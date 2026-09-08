# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

Unity 6 (6000.6.0f1) / URP 2D の縦スクロール型チャージジャンプゲーム「たまわたり」。
ハロウィンの「灯篭の道」をテーマにした完全2D(XY平面・Z固定)。WebGL 配信が主ターゲット。

**アート資産・プレハブ・音源ファイルを一切持たない**。スプライト・UI・効果音・BGM は
すべてランタイムに手続き生成される(`MockUtil` / `AudioManager`)。したがってシーンは
ほぼ空で、コードを読めばゲーム全体が分かる。

git のリポジトリルートはこのディレクトリの1つ上(`D:\DevDataGit\Prj_Tamawatari`)。

## コマンド

作業ディレクトリは `Tamawatari/`(このファイルがある場所)。

```sh
# コンパイル確認(約4秒)。Unity Editor を開いたままでも実行できるので、これを常用する。
# 出力は Temp\bin\Debug\(gitignore 済み)
dotnet build Assembly-CSharp.csproj -v q -nologo

# エディタ本体
"C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe" -projectPath .
```

- **バッチモード**(`-batchmode -quit -projectPath .`)は、同じプロジェクトを Editor が開いている間
  `Temp/UnityLockfile` のロックで失敗する。まず Editor を閉じる必要がある。
- ビルドは Build Profile 経由(`Assets/Settings/Build Profiles/`: Web - Desktop / Web - Mobile / Windows)。
  ビルド用の Editor スクリプトは無いので、CLI からビルドしたい場合は自分で `BuildPipeline` を叩く
  Editor スクリプトを足すことになる。
- `Assembly-CSharp.csproj` は Unity が生成する。**新しい .cs を足した直後は `<Compile Include>` に
  載っていないので `dotnet build` の対象にならない**。手で1行足すか、Unity を一度フォーカスして
  作り直させる(Unity が作り直すと手書きの行は消える)。
- シーン順は Title → InGame(`SampleScene` は無効)。
- **テストは存在しない**。`com.unity.test-framework` は入っているが test assembly も asmdef も無い
  (`Assets/Welcome/` の asmdef はチュートリアル同梱物で本編とは無関係)。

### 埋め込みフォントの作り直し

`../Tools/rebuild_font_subset.py`(リポジトリルートの `Tools/`)が
`Assets/Resources/Fonts/ShipporiMincho.ttf` を生成している。収録字は
**ASCII + Shift-JIS のかな・記号・第一水準漢字 + `Assets/Scripts/*.cs` に出てくる全文字**。

```sh
pip install fonttools
python ../Tools/rebuild_font_subset.py
```

第一水準の外の字を UI 文言に足したときは流し直さないと**その字だけ描画されない**。
UI に字を足したら、フォントに入っているかを確かめるのが安全(fontTools で cmap を見る)。

## アーキテクチャ

### Bootstrap パターン

シーンには入口のコンポーネント1個だけが置かれ、そこから全オブジェクトを `new GameObject()` で
組み立てて相互参照を手で配線する。

- `Title.unity` → `TitleController` … カメラ・演出・UI をその場で生成。SPACE で InGame、SHIFT でランキング。
- `InGame.unity` → `InGameBootstrap` … Player / PlatformSpawner / EnemySpawner / GameManager /
  UIManager / EventDirector / SectionDirector / CameraFollow / AtmosphereFx を生成し配線する。
  **ゲームに登場人物やシステムを足すときは、まずここに生成と配線を書く。**

### パラメータの流れ

`GameTunings.cs` に調整値クラスがすべて集まっている(`PlayerTuning`, `PlatformTuning`,
`EnemyTuning`, `EventTuning`, `SectionTuning`, `ScoreTuning`, `CameraTuning`,
`AtmosphereTuning`, `FontTuning`, `AudioTuning`, `IndicatorTuning`, `PlayerAnimTuning`)。
Bootstrap が `[SerializeField]` で保持し、`component.Tuning = xxxTuning` で各コンポーネントへ配る。

新しい数値は各コンポーネントに直接書かず、対応する Tuning クラスに追加する。

### 主要コンポーネント

| 役割 | 実体 |
| --- | --- |
| 状態・スコア・コンボ・連続キルの一元管理 | `GameManager`(static `Instance`。`GameState` = Playing / GameOver / Clear) |
| プレイヤー操作 | `PlayerController`(状態機械 Idle → Charging → Jumping / Falling) |
| 足場(灯篭)の動的生成 | `PlatformSpawner` + `LanternPlatform`(本道 chain + 撒き scatter、色ID、大灯籠) |
| 敵 | `EnemySpawner` + `EnemyController`(接触即ゲームオーバー、衝撃波で成仏) |
| 魂と統合 | `SoulSystem`(敵撃破で魂、満タン + SHIFT 長押しで安全な道を生成) |
| ランダムイベント | `EventDirector` + `GameEvents.cs` の `GameEvent` 派生群 |
| 区間進行と難易度上昇 | `SectionDirector`(大灯籠に乗る → 一息 → 敵強化) |
| HUD・リザルト | `UIManager`(911行。ランタイム uGUI を全部ここで組む) |
| ランキング永続化 | `ScoreBoard`(PlayerPrefs `tamawatari_ranking_v1` に自前の文字列直列化で上位8件) |
| ランキング表示 | `RankingView`(Title / Result の両方から使う) |

### 手続き生成の土台: `MockUtil`

- スプライト: 円 / リング / 光 / ビネット / 霧 / 矢印を `Texture2D` から生成しキャッシュ。
- ワールドオブジェクト: `MakeLantern`(灯篭。状態ごとに塗り替えられるよう `LanternParts` を返す)、
  `MakeBossGhost`(プレイヤー)、`MakeHitodama`(人魂)。
- `CircleVisualRadius = 0.5f` が **見た目と接地判定を一致させる基準**。円スプライトは直径1ワールドユニット。
- ランタイム uGUI: `CreateCanvas` / `CreateText` / `CreateImage` / `CreateBox`
  (Canvas は 1920x1080 参照解像度の ScaleWithScreenSize)。

### 入力

`InputHub`(static)が唯一の入力窓口。**`Keyboard.current` / `Gamepad.current` / `Touchscreen.current`
を各コンポーネントから直接読まない**。追加の入力手段が必要なら `InputHub` に集約する。

- Confirm(決定 / 狙う・溜める) = Space / パッド South / **画面タップ**
- Special(衝撃・統合・ランキング) = Shift / パッド West / **バーチャルボタン**
- 直近に触ったデバイスを `Poll()` で判定し、`ConfirmLabel` / `SpecialLabel` を切り替える。
  `[RuntimeInitializeOnLoadMethod]` で `~InputHub` 常駐オブジェクトを自前で立てている。
- UI の操作説明は `InputLabel.Bind(text, "{C} : はじめる  {S} : ランキング")` の書式で書く。
  `{C}` / `{S}` がデバイスに追従して自動で書き換わる。

### スマホ(タッチ)対応

**SHIFT だけがバーチャルボタン、それ以外のどこを触っても Space と同じ**という方針。

- `VirtualPad` が右下の SHIFT ボタンを描き、その画面矩形を `InputHub.SpecialTouchArea` に渡す。
  タッチの仕分け(Special / Confirm / 無視)は `InputHub.EnsureTouch` が生のタッチを見て行う。
  指ごとに `Touchscreen.touches` のスロット単位で判定するので、溜めながら SHIFT を押せる。
- 触っても Confirm にしたくない領域(名前入力欄)は `InputHub.AddTouchBlocker(() => rect)` で登録する。
- パッドは全シーンに1つずつ、`VirtualPad.Create(touchTuning)` で明示的に作る
  (Title は `TitleController`、InGame は `InGameBootstrap`)。タッチ環境でなければ隠れて常駐し、
  実際に画面が触られた時点で出てくる(WebGL でモバイル判定が外れる端末があるため)。
- **エディタ確認**: `Tamawatari > 開発用 > スマホ環境をまねる`(後述の `MobileSim`)を使う。
  `TouchTuning.forceVirtualPad` の方はビルドにも効く実運用スイッチなので、確認目的では使わない。

### 開発用のスマホ環境シミュレート(ビルドには載らない)

`MobileSim`(`Assets/Scripts/MobileSim.cs`)+ `Assets/Editor/MobileSimMenu.cs`。
**中身は丸ごと `#if UNITY_EDITOR` の中**で、プレイヤービルドでは `MobileSim.Enabled` が
常に false になるため一切効かない(スイッチも EditorPrefs なのでシーン・設定にも残らない)。

- `Tamawatari > 開発用 > スマホ環境をまねる` : 手動トグル(チェック付き)
- `Tamawatari > 開発用 > Device Simulator を開く` : 画面の形・解像度まで揃えたいとき。
  `MobileSim.Enabled` は `UnityEngine.Device.Application.isMobilePlatform` も見ているので、
  Device Simulator でスマホを選んでいる間はメニューを触らなくても ON になる。

ON の間、実機と条件を揃えるために変わるのは以下:

| 揃える対象 | 実装箇所 |
| --- | --- |
| バーチャルパッドが出る / マウス左クリックが指になる | `InputHub.WantsTouchUi`, `InputHub.EnsureTouch` |
| 物理キーボード / パッドを無かったことにする | `InputHub.PhysicalInputEnabled`(`Key` / `Pad` が null を返す) |
| 名前入力が OS ソフトキーボードではなく `OnScreenKeyboard` になる(WebGL と同じ) | `NameField.BeginEdit` |
| フォントが同梱フォント固定になり OS フォントへ落ちない(WebGL と同じ) | `MockUtil.BuildFont` |

ON の間は画面右上に「スマホ環境をまねています (開発用)」と出る(`VirtualPad` の `#if UNITY_EDITOR` 部分)。
**スマホ側の条件を1つ増やすときは、この表の4箇所と同じ形で `MobileSim.Enabled` を見て分岐させる。**
- `EventSystem` / `InputField` は使っていない。当たり判定は
  `MockUtil.ScreenRect(RectTransform)` で矩形を取って自前で突き合わせる。

### 名前入力

`NameField`(タイトル)が担当。編集中は `InputHub.Suppressed` を立てて SPACE / タップで
ゲームが始まらないようにする。文字入力の経路は3つ:

- PC : `Keyboard.onTextInput`(枠をクリック、または TAB で編集開始)
- Android / iOS : `TouchScreenKeyboard`
- **WebGL(TouchScreenKeyboard 非対応)**: 自前の `OnScreenKeyboard`(かな五十音 + 英数、
  濁点 / 半濁点 / 小文字は直前の1文字に掛ける)

名前は `ScoreBoard.PlayerName`(PlayerPrefs `tamawatari_player_name`)に保存され、
リザルト登録時に `ScoreEntry.Name` としてランキングに残る。

### イベントを追加する

`GameEvents.cs` に `GameEvent` 派生クラスを書き(`DisplayName` / `Description` / `Tint` と
`OnBegin` / `OnTick` / `OnEnd`、必要なら `CanStart` / `FixedDuration`)、
`EventDirector.Init` の `Register(new XxxEvent())` に1行足すだけで抽選対象になる。
ゲーム側へ触る手段は `GameEventContext` 経由。足りない参照はそこに増やす。

## 実装上の約束

- **`.meta` の GUID**: `1a2b3c4d5e6f4718293a4b5c6d7e8f0X` 系の GUID は手書きで、シーンから
  この GUID で参照されている(例: `InGameBootstrap` = `...8f0b`、`TitleController` = `...8f0a`)。
  既存スクリプトの `.meta` の GUID を書き換える・ファイルを作り直すとシーンの参照が切れる。
- **Tuning の防御的初期化**: シーンに古い直列化データが残るため、Bootstrap の `Awake` で
  `null` や 0 値をチェックして既定値に差し替えている。Tuning にフィールドを足すときも
  同じ発想で「シーンに保存されていなくても動く」ようにする。
- **WebGL の制約**(過去に踏んだ罠なので崩さない):
  - OS フォントも組み込みフォントも WebGL では描画されない。`Assets/Resources/Fonts/ShipporiMincho.ttf`
    (`MockUtil.EmbeddedFontResource`)への依存が必須。テキストが出ないときはまずここを疑う。
  - ブラウザの自動再生制限があるため、`AudioManager.StartBgm()` は最初の入力より後に呼ぶ
    (タイトルでキーを押したタイミング)。
- **効果音・BGM** は `AudioClip.Create` の波形合成(`Assets/Scripts/AudioManager.cs` の `Make`)。
  音を足すときも同じ流儀で合成する。
- コメントは日本語の XML doc コメント。クラス冒頭に仕様の要約を書く既存の密度に合わせる。
