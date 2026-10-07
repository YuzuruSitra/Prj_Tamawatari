# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

Unity 6 (6000.6.0f1) / URP 2D の縦スクロール型チャージジャンプゲーム「たまわたり」。
ハロウィンの「灯篭の道」をテーマにした完全2D(XY平面・Z固定)。WebGL 配信が主ターゲット。

絵・音・プレハブは**アセットとしてリポジトリに入っている**。実行時のコードはそれを
`Instantiate` するだけで、形や音をその場で作ることはしない。

| 置き場所 | 中身 |
| --- | --- |
| `Assets/Art/Sprites/` | 円 / リング / 光 / ビネット / 霧 / 矢印の PNG |
| `Assets/Audio/` | 効果音 12 種と BGM。合成音は WAV、手で差し替えた音は MP3 |
| `Assets/Prefabs/` | 灯篭・プレイヤー・お化け・魂・衝撃波・灯の粉・ふわっと消えるお化け・音のリグ |
| `Assets/Prefabs/UI/` | タイトル / HUD + リザルト / ランキング / バーチャルパッド / ソフトキーボード |
| `Assets/Resources/GameAssets.asset` | 上記をまとめたカタログ。実行時はここだけを見る |
| `Assets/Plugins/WebGL/` | WebGL でしか書けない処理の jslib(いまは画像のクリップボードコピーだけ) |

これらは `Assets/Editor/Bake/` の**焼き直しツールが作ったもの**で、手で描いたものではない。
形と音を決める式、画面のレイアウトはすべてそこにコードとして残っている。
Inspector で直接いじってもよいが、焼き直すと上書きされる。

git のリポジトリルートはこのディレクトリの1つ上(`D:\DevDataGit\Prj_Tamawatari`)。

## コマンド

作業ディレクトリは `Tamawatari/`(このファイルがある場所)。

```sh
# コンパイル確認(約4秒)。Unity Editor を開いたままでも実行できるので、これを常用する。
# 出力は Temp\bin\Debug\(gitignore 済み)
dotnet build Assembly-CSharp.csproj -v q -nologo

# エディタ本体
"C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe" -projectPath .

# アセット(スプライト / 音 / プレハブ)の焼き直し。Editor を閉じてから実行する。
"C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe" -batchmode -quit -nographics \
  -projectPath . -executeMethod Tamawatari.Bake.AssetBaker.BakeAll -logFile bake.log
```

エディタを開いたままなら **Tamawatari > アセット > 焼き直す** メニューで同じことができる。

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

シーンには入口のコンポーネント1個だけが置かれ、そこから必要なものを出して相互参照を手で配線する。
見た目を持つものは `GameAssets` のプレハブを `Instantiate` し、持たないもの(進行管理など)だけ
`new GameObject()` で作る。

- `Title.unity` → `TitleController` … カメラと空気感を用意し、`titleCanvas` プレハブ
  (なまえ入力欄も中に入っている)と `rankingPanel` を出す。SPACE で InGame、SHIFT でランキング。
- `InGame.unity` → `InGameBootstrap` … Player プレハブと HUD プレハブを出し、
  PlatformSpawner / EnemySpawner / GameManager / EventDirector / SectionDirector /
  CameraFollow / AtmosphereFx を作って配線する。
  **ゲームに登場人物やシステムを足すときは、まずここに生成と配線を書く。**

### パラメータの流れ

`GameTunings.cs` に調整値クラスがすべて集まっている(`PlayerTuning`, `PlatformTuning`,
`EnemyTuning`, `EventTuning`, `SectionTuning`, `ScoreTuning`, `CameraTuning`,
`AtmosphereTuning`, `AudioTuning`, `IndicatorTuning`, `PlayerAnimTuning`, `TouchTuning`)。
Bootstrap が `[SerializeField]` で保持し、`component.Tuning = xxxTuning` で各コンポーネントへ配る。

新しい数値は各コンポーネントに直接書かず、対応する Tuning クラスに追加する。

Tuning はプレハブの上に**流し込む**形になる。たとえば `EnemyTuning.enemyDiameter` は
`Enemy.prefab` のルートスケールへ、`soulColor` は `TintedParts.SetTint` へ渡る。
「アセットが持つ形」と「Tuning が決める大きさ・色」の境目はこの流し込みの位置にある。

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
| HUD・リザルト | `UIManager`(`HudCanvas.prefab` のルートに付く。値の出し入れと演出だけ) |
| ランキングの画像コピー | `ScoreShot`(画面を撮ってクリップボードへ。WebGL は `navigator.clipboard` / Windows は CF_DIB) |
| ランキング永続化 | `ScoreBoard`(PlayerPrefs `tamawatari_ranking_v1` に自前の文字列直列化で上位8件) |
| ランキング表示 | `RankingView`(`RankingPanel.prefab`。Title / Result の両方から使う。出すのは `ScoreBoard` だけで**通信はしない**。開いている間の「画像をコピー」も持つ) |
| アセットのカタログ | `GameAssets`(`Resources/GameAssets.asset`。`GameAssets.I` と `Spawn()` で引く) |
| 絵まわりの決めごと | `GameArt`(`CircleVisualRadius` / `FogCoreT` / `WithAlpha` / `ScreenRect`) |
| 1色から作った色の塗り替え | `TintedParts`(人魂・プレイヤー・ふわっと消えるお化けに付く) |

`GameArt.CircleVisualRadius = 0.5f` が **見た目と接地判定を一致させる基準**。
円スプライトは PPU = 画像の1辺 で読み込んであるので、スケール1で直径1ワールドユニットになる。

### アセットの持ち方

実行時のコードは**アセットを出すだけ**で、形を組み立てない。組み立ては
`Assets/Editor/Bake/` にあり、ここを直して焼き直すと `Assets/` のアセットが更新される。

| 焼き直しツール | 作るもの |
| --- | --- |
| `BakeSprites` | PNG(円 / リング / 光 / ビネット / 霧 / 矢印)。中心からの距離で不透明度を決める式 |
| `BakeAudio` | WAV(効果音 12 + BGM)。波形合成の式と 16bit PCM の書き出し。**同じ名前の wav 以外が置かれていればそれを使い、書かない** |
| `BakeWorldPrefabs` | 灯篭 / プレイヤー / お化け / 魂 / 衝撃波 / 灯の粉 / ふわっと消えるお化け / 音のリグ |
| `BakeUiPrefabs` | タイトル / HUD + リザルト / ランキング / バーチャルパッド / ソフトキーボード |
| `UiBuild` | UI プレハブを組むための uGUI ヘルパ(Canvas は 1920x1080 参照解像度の ScaleWithScreenSize) |
| `AssetBaker` | 上を順に呼び、`GameAssets.asset` に参照をまとめる。メニューとバッチの入り口 |

プレハブ側のパーツ参照は、各コンポーネントの `#if UNITY_EDITOR` な `BindPartsForBake` /
`SetXxxRefs` に焼き直しツールから差し込んでいる。**プレハブに子オブジェクトを足したら、
そのコンポーネントに `[SerializeField]` を1つと、対応する差し込み口を1つ増やす。**

<a id="asset-bake-note"></a>
**プレハブに触るときの注意**:
- 実行時に `Instantiate` するものを増やしたら、`GameAssets` にフィールドを足して
  `AssetBaker.WriteCatalog` で埋める。Resources に増やす必要はない(カタログ1枚だけが Resources)。
- 灯篭の描画順は `LanternPlatform` の `OrderXxx` 定数が全体の順からの相対で決めていて、
  プレハブもその並びで焼いてある。片方だけ変えるとズレる。

### 入力

`InputHub`(static)が唯一の入力窓口。**`Keyboard.current` / `Gamepad.current` / `Touchscreen.current`
を各コンポーネントから直接読まない**。追加の入力手段が必要なら `InputHub` に集約する。

- Confirm(決定 / 狙う・溜める) = Space / パッド South / **画面タップ**
- Special(衝撃・統合・ランキング) = Shift / パッド West / **バーチャルボタン**
- Capture(ランキングの画像をコピー) = P / パッド North / **ランキング右上の「画像をコピー」ボタン**
- 直近に触ったデバイスを `Poll()` で判定し、`ConfirmLabel` / `SpecialLabel` を切り替える。
  `[RuntimeInitializeOnLoadMethod]` で `~InputHub` 常駐オブジェクトを自前で立てている。
- UI の操作説明は `InputLabel.Bind(text, "{C} : はじめる  {S} : ランキング")` の書式で書く。
  `{C}` / `{S}` / `{P}` がデバイスに追従して自動で書き換わる。

### ランキングの画像コピー

**ランキングを開いている間**に P / パッド △ / 右上の「画像をコピー」を押すと、いま見えている
画面を画像にして**クリップボードに置く**(ファイルとして保存するのではなく、そのまま
貼り付けられる状態にする)。担当は `ScoreShot`(静的クラス)と `RankingView` の画像コピー節。
**リザルトとタイトルの両方で効く** — ランキングパネルは同じプレハブを共有しているため。

- 撮るのは画面なので、**写したくない飾りは呼び出し側が隠す**。`ScoreShot.Capture` の
  `hide` に並べたものを一時的に消し、1フレーム置いてから `WaitForEndOfFrame` で読み、
  撮り終えたら元に戻す(もともと消えていたものは触らない)。
  いま隠しているのは操作案内とコピーボタン。
- 別 Canvas にいる `VirtualPad` は hide に渡せないので、自分で `ScoreShot.IsCapturing`
  を見て引っ込む。**撮影中に消したいものを増やすときはどちらかの形に合わせる。**
- 代わりに、撮るときだけ署名(ゲーム名・日付)の1行を出す
  (`copyStamp`。隠れた案内文と同じ位置に入れ替わる)。
- 結果の一言(`copyNote`)は **5 秒**出て消える(`RankingView.NoteLife`)。
  写り込まないように**カードの外**(すぐ下)に置いてある。
- **ボタンはどの UI にも重ならない位置に置く**。いまは見出しの反対側、
  カード右上の y 202..264(1位の行は y 130..174、上の罫線は y 290)。
  角丸の画像は焼いていないので、`BakeUiPrefabs.Capsule`(箱 + 両端の円)で
  カプセルを作り、一回り大きい琥珀のカプセルを下に敷いて縁取りにしている。
  **3枚で 1 つの形なので、色は必ず `Paint` でまとめて塗る**(でないと継ぎ目が見える)。
- 撮影中にパネルを閉じるとコルーチンごと止まるので、`RankingView.OnEnable` が
  飾りを必ず出し直し、`OnDisable` が `ScoreShot.Cancel()` で印を落とす。

出し先は環境で分かれる。**どれもクリップボードが第一候補**:

| 環境 | 出し先 |
| --- | --- |
| WebGL | `navigator.clipboard.write` に PNG の Blob を渡す(`Assets/Plugins/WebGL/TamawatariShot.jslib`)。書き込みは Promise なので `TamawatariCopyResult` を C# 側が見張る。画像のコピーに未対応のブラウザではダウンロードに落ちる |
| Windows(Editor 含む) | `user32` を直接叩いて CF_DIB を置く。**Unity に画像をクリップボードへ置く API は無い**(`systemCopyBuffer` は文字だけ)ため。24bit BI_RGB で渡す(32bit は貼り先によって真っ黒になる) |
| それ以外 | クリップボードに手が届かないので `persistentDataPath/Screenshots/` に書く(受け皿) |

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

ON の間は画面右上に「スマホ環境をまねています (開発用)」と出る(`VirtualPad` の `#if UNITY_EDITOR` 部分)。
**スマホ側の条件を1つ増やすときは、この表の3箇所と同じ形で `MobileSim.Enabled` を見て分岐させる。**
- `EventSystem` / `InputField` は使っていない。当たり判定は
  `GameArt.ScreenRect(RectTransform)` で矩形を取って自前で突き合わせる。

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
- **アセットが無くても落とさない**: `GameAssets.I` も `GameAssets.Spawn()` も、焼き直し前や
  参照切れのときは `null` を返してログを出すだけにしてある。呼び出し側も `null` を通す。
- **WebGL の制約**(過去に踏んだ罠なので崩さない):
  - OS フォントも組み込みフォントも WebGL では描画されない。`Assets/Resources/Fonts/ShipporiMincho.ttf`
    への依存が必須。UI プレハブの `Text` はすべてこのフォントを直接参照している
    (焼き直しツールが差し込む)。テキストが出ないときはまずここを疑う。
  - ブラウザの自動再生制限があるため、`AudioManager.StartBgm()` は最初の入力より後に呼ぶ
    (タイトルでキーを押したタイミング)。
- **効果音・BGM** は `Assets/Audio/`。波形合成の式は `BakeAudio` にあるので、
  音を足すときはそこに1行足して焼き直し、`GameAssets` にフィールドを増やす。
- **本物の音への差し替え**: `BakeAudio` が書くのは `<名前>.wav` だけなので、
  **同じ名前で wav 以外(mp3 / ogg)を置けば、焼き直しても上書きされず、
  `GameAssets` もそちらを指す**。合成音に戻したいときは置いた方を消して焼き直す。
  **wav のまま中身を差し替えると焼き直しで消える**ので、必ず別の拡張子で置く。
  どれが差し替わっているかは `Assets/Audio/` を見れば分かる(wav = 合成音 / それ以外 = 本物)。
  **差し替えたあとは焼き直すまで `GameAssets` が消えた wav を指したままになる**ので、その音だけ無音になる。
- コメントは日本語の XML doc コメント。クラス冒頭に仕様の要約を書く既存の密度に合わせる。


## ループ協議

各タスクは「直線」ではなく「ループ」として走らせる:

1. 変更を書く
2. チェックを走らせる: テスト + linter + 型チェック
3. 失敗した? エラーを読み、原因を特定し、直して、2 に戻る
4. ループは最大 5 回まで

停止条件:
- 全チェック通過 → 「完了」と報告。通過した出力を証拠として添える
- 5 回使い切った → 止まって、何が残っているか報告する
- 同じエラーが 2 回連続 → ループを止め、@fixer を呼ぶよう促す

禁止: チェック出力なしで「完了」と報告すること
禁止: アサーション削除やテスト弱体化で通すこと。直すのはコードで、スコアボードではない
