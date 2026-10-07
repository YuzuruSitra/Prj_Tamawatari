using UnityEngine;

/// <summary>
/// Inspector で調整する数値パラメータ群(完全2D / ハロウィン「灯篭の道」)。
/// 溜め速度・カーソル速度・敵挙動・カメラ・魂・見た目をまとめてある。
/// </summary>
[System.Serializable]
public class PlayerTuning
{
    [Header("Charge (溜め)")]
    public float chargeSpeed = 0.67f;          // 溜め速度 (0->1 / 秒)。約1.5秒で最大
    public float chargeZeroThreshold = 0.06f;  // これ以下で離すと「その場で衝撃波」
    public float groundShockCooldown = 0.4f;   // その場/Shift 衝撃波の連発防止(秒)

    [Header("Jump")]
    public float minJumpDistance = 2.5f;
    public float maxJumpDistance = 6.5f;
    public float jumpDuration = 0.55f;

    [Header("Body / Landing")]
    public float playerDiameter = 0.8f;
    // 着地判定 = プレイヤー中心が「足場の見た目の円 + landingProbe」以内。
    public float landingProbe = 0.25f;
    public Color bodyColor = new Color(0.62f, 0.42f, 0.86f);
    // 待機中に足場から外れたら落下扱いにする(磁気嵐で流されたとき等)
    public bool fallWhenOffPlatform = true;

    [Header("Miss (空振り演出時間)")]
    public float missFallTime = 0.35f;

    [Header("Shockwave (衝撃波)")]
    public float shockBaseRadius = 1.5f;       // 半径 = shockBaseRadius + charge * shockMaxRadiusBonus
    public float shockMaxRadiusBonus = 3.5f;
    public float shockwaveDuration = 0.35f;
    [Range(0.1f, 1f)]
    public float shiftShockScale = 0.5f;       // Shift 発動時は溜め段階の半分のサイズ

    [Header("Flight (ジャンプ中)")]
    public bool invincibleWhileFlying = true;  // 飛んでいる間は無敵
    public float trailInterval = 0.035f;       // 霊気の尾を出す間隔(秒)

    [Header("Perfect Landing (ど真ん中着地とコンボ)")]
    [Range(0.1f, 0.9f)]
    public float perfectThreshold = 0.35f;     // 足場半径のこの割合以内なら PERFECT
    public float perfectShockScale = 1.6f;     // PERFECT 時の衝撃波倍率
    public int perfectScore = 30;              // PERFECT 基礎点
    public int comboBonusPerStep = 15;         // コンボ1段ごとの上乗せ

    [Header("Souls / Merge (魂と統合)")]
    public int maxSouls = 3;
    public float soulOrbitRadius = 1.15f;
    public float soulOrbitSpeed = 220f;
    public float soulMergeHoldTime = 0.6f;
    public int safePathCount = 5;
    public float safePathGap = 3.5f;
    public float safePathDiameter = 2.6f;
}

[System.Serializable]
public class IndicatorTuning
{
    public float swingSpeed = 200f;            // カーソル速度 (deg/sec) ※振り子の往復速度
    public float swingHalfRangeDeg = 90f;      // 真上から左右それぞれの振れ幅(合計180度)
    public float radius = 2.0f;
    public float markerDiameter = 0.85f;       // 矢印カーソルの大きさ
    public Color markerColor = new Color(1f, 0.72f, 0.3f);
}

/// <summary>プレイヤーの簡易アニメーション(手続き)。</summary>
[System.Serializable]
public class PlayerAnimTuning
{
    [Header("Idle")]
    public float bobAmount = 0.07f;            // ふわふわ上下
    public float bobSpeed = 2.2f;
    public float breathAmount = 0.04f;         // 呼吸のスケール揺れ

    [Header("Charge")]
    public float crouchAmount = 0.16f;         // 溜めでしゃがむ量
    public float chargeSquash = 0.18f;         // 溜めのつぶれ
    public float chargeShake = 0.03f;          // 溜めの震え

    [Header("Jump")]
    public float jumpHop = 0.45f;              // 見た目の跳ね上がり
    public float jumpStretch = 0.22f;          // 進行方向への伸び
    public float jumpTiltDeg = 14f;            // 進行方向へのわずかな傾き

    [Header("Land / Miss")]
    public float landSquash = 0.30f;
    public float landSquashTime = 0.18f;
    public float missSpinDeg = 540f;
}

/// <summary>ランダムイベント(磁気嵐など)。</summary>
[System.Serializable]
public class EventTuning
{
    public bool enabled = true;
    public float firstDelay = 12f;             // 最初のイベントまで
    public Vector2 intervalRange = new Vector2(16f, 26f);   // イベント間隔
    public Vector2 durationRange = new Vector2(7f, 11f);    // 継続時間
    public float warningTime = 2f;             // 開始前の予告表示(秒)
    public float eventFadeTime = 0.7f;         // イベント開始時にお化けが消えるまでの時間

    [Header("磁気嵐 (Magnetic Storm)")]
    public float stormDriftSpeed = 1.15f;      // 停止中に左下へ流される速さ (unit/sec)
    public Vector2 stormDriftDir = new Vector2(-1f, -1f);   // 流される向き

    [Header("お化け大量発生 (Ghost Swarm)")]
    public float swarmIntervalScale = 0.22f;   // 出現間隔をこの倍率に
    public int swarmBurstCount = 6;            // 開始時に一気に湧く数

    [Header("色替え (Lantern Roulette)")]
    public float rouletteFirstSwitchDelay = 0.4f;   // 開始直後の猶予
    public float rouletteIntervalScale = 2.6f;      // 出現間隔をこの倍率に(少しだけ湧く)

    [Header("濃霧 (Deep Fog)")]
    public float fogHoleRadius = 250f;              // 見える範囲の半径(px, 1920x1080 基準)
    public float fogFadeTime = 0.8f;                // 霧の出入りにかける時間
    public Color fogColor = new Color(0.05f, 0.05f, 0.09f, 0.97f);
    public float fogIntervalScale = 1.6f;           // 霧の間は敵をやや控えめに

    [Header("黄金の道 / ボーナスタイム (Golden Path)")]
    public float goldenDuration = 9f;               // 専用の継続時間(0 以下なら durationRange)
    public float goldenPlatformScale = 1.45f;       // 足場が大きくなる倍率
    public int goldenLandingBonus = 120;            // 1回着地するごとのボーナス点
    public float goldenMoteBoost = 2.6f;            // 火の粉の増し方
}

/// <summary>リザルトのスコア計算と連続キル。</summary>
[System.Serializable]
public class ScoreTuning
{
    public int depthMultiplier = 10;           // 深度(m) × これ
    public int killMultiplier = 50;            // 成仏1体 × これ

    [Header("連続キル (Kill Streak)")]
    public float killStreakWindow = 3.0f;      // 次のキルまでこの秒数以内なら連鎖継続
    public int killStreakBonus = 25;           // 連鎖 n 段目のキルで n × これ を加点
}

/// <summary>
/// 区間(セクション)。一定数の灯籠を渡ると大灯籠に到達し、
/// お化けが一掃されて一息つける。その後難易度が一段上がる。
/// </summary>
[System.Serializable]
public class SectionTuning
{
    public bool enabled = true;
    public float restDuration = 3.5f;          // 一息つける長さ(秒)
    public int sectionBonus = 250;             // 区間クリアの基礎点
    public int sectionBonusStep = 120;         // 区間が進むごとに上乗せ
    [Range(0.05f, 1f)] public float slowMoScale = 0.32f;   // 到達瞬間のスロー
    public float slowMoTime = 0.45f;
    [Range(1f, 2f)] public float speedStep = 1.10f;        // 1区間ごとに敵速度 ×
    [Range(0.5f, 1f)] public float intervalStep = 0.90f;   // 1区間ごとに出現間隔 ×
    public float minIntervalScale = 0.45f;                 // 間隔の下限
    public float maxSpeedScale = 2.0f;                     // 速度の上限
}

/// <summary>音まわり。音源は Assets/Audio の AudioClip アセット。</summary>
[System.Serializable]
public class AudioTuning
{
    [Range(0f, 1f)] public float masterVolume = 0.9f;
    [Range(0f, 1f)] public float bgmVolume = 0.35f;
    [Range(0f, 1f)] public float sfxVolume = 0.7f;
    public bool bgmEnabled = true;
    public bool sfxEnabled = true;
}

[System.Serializable]
public class PlatformTuning
{
    [Header("Size (ランダム)")]
    public float minPlatformDiameter = 0.9f;
    public float maxPlatformDiameter = 2.8f;

    [Header("Density (密度)")]
    public int aheadCount = 16;
    public int scatterPerStep = 2;
    public float scatterSpread = 3.2f;
    public float scatterMinDiameter = 0.7f;
    public float scatterMaxDiameter = 1.8f;
    public float cullBehind = 14f;

    [Header("Path (必ず届く保証は維持)")]
    public float maxTurnAngleDeg = 30f;
    public float maxHeadingDeg = 55f;
    public float minGapExtra = 0.4f;
    [Range(0.5f, 1f)]
    public float reachSafety = 0.9f;

    [Header("区間 (大灯籠のチェックポイント)")]
    public int sectionLength = 10;             // この数だけ灯籠を通ると区間クリア
    public float greatLanternScale = 1.8f;     // 大灯籠の大きさ(maxPlatformDiameter の倍率)
    public Color greatStone = new Color(0.52f, 0.42f, 0.34f);
    public Color greatCore = new Color(1f, 0.92f, 0.62f);

    [Header("Clear 条件")]
    public int clearPlatformCount = 30;        // これだけ通過でクリア(0 = エンドレス)

    [Header("Look (灯篭) - 石の円盤が接地範囲、縁のリングがその境界")]
    public Color litStone = new Color(0.36f, 0.26f, 0.30f);
    public Color dimStone = new Color(0.30f, 0.24f, 0.28f);
    // 脇道(背景)も「灯っている」ことがはっきり分かる明るさにする。
    // 乗れない灯篭は完全に灯が落ちて ✕ が出るので、これらとは混同しない。
    [Range(0.05f, 1f)] public float dimGlowAlpha = 0.34f;
    [Range(0.1f, 1f)] public float dimCoreFade = 0.78f;

    [Header("Palette - 灯の色。色替えイベントで有効な色が切り替わる")]
    public Color[] palette =
    {
        new Color(1f, 0.70f, 0.30f),
        new Color(0.80f, 0.56f, 1f),
        new Color(0.42f, 0.92f, 0.86f),
    };
    [Tooltip("乗れない灯篭の色。灯が完全に落ちて ✕ が浮かぶ")]
    public Color disabledColor = new Color(0.34f, 0.34f, 0.40f);
    [Tooltip("ボーナスタイム(黄金の道)の色")]
    public Color goldColor = new Color(1f, 0.80f, 0.30f);
}

[System.Serializable]
public class EnemyTuning
{
    [Header("Spawn interval (秒) - 時間経過で base -> min へ")]
    public float baseSpawnInterval = 3.0f;
    public float minSpawnInterval = 0.8f;

    [Header("Speed - 時間経過で base -> max へ")]
    public float baseEnemySpeed = 2.2f;
    public float maxEnemySpeed = 5.0f;

    [Header("Difficulty")]
    public float difficultyRampTime = 60f;

    [Header("Placement")]
    public float spawnRadius = 11f;
    public float firstSpawnDelay = 2.5f;
    public float enemyDiameter = 0.7f;

    [Header("Weave (ゆらゆら追跡)")]
    public float weaveAmplitude = 1.2f;
    public float weaveFrequency = 3.0f;

    [Header("Lifecycle / Look")]
    public float despawnDistance = 45f;
    public float vanishTime = 0.25f;
    public Color soulColor = new Color(0.48f, 0.74f, 1f, 0.85f);   // 冷たい青の人魂
}

[System.Serializable]
public class CameraTuning
{
    public float orthographicSize = 7f;
    public Vector2 offset = new Vector2(0f, 2.5f);
    public float followLerp = 6f;
    public Color backgroundColor = new Color(0.055f, 0.045f, 0.085f);  // 夜

    [Header("Shake")]
    public float landShakeDuration = 0.18f;
    public float landShakeMagnitude = 0.28f;
    public float hitShakeDuration = 0.45f;
    public float hitShakeMagnitude = 0.7f;
    public float mergeShakeDuration = 0.5f;
    public float mergeShakeMagnitude = 0.9f;
}

[System.Serializable]
public class AtmosphereTuning
{
    public bool enabled = true;
    public int moteCount = 46;                 // ただよう灯の粉
    public Vector2 sizeRange = new Vector2(0.06f, 0.2f);
    public Vector2 riseSpeedRange = new Vector2(0.25f, 0.95f);
    public float wobble = 0.5f;
    public Color moteColor = new Color(1f, 0.72f, 0.35f, 0.5f);
    public float padding = 3f;                 // 画面外にどれだけはみ出して湧かせるか
}

/// <summary>
/// スマホ(タッチ)対応の調整値。バーチャルパッドの見た目と大きさ。
/// 判定そのものは InputHub / VirtualPad 側で行う。
/// </summary>
[System.Serializable]
public class TouchTuning
{
    [Tooltip("ビルドでも常にバーチャルパッドを出す(タッチ対応 PC 向けの実運用スイッチ)。 エディタで試すだけなら Tamawatari > 開発用 > スマホ環境をまねる の方を使う")]
    public bool forceVirtualPad = false;

    [Header("SHIFT ボタン (右下)")]
    public float buttonDiameter = 250f;        // 1920x1080 換算の直径
    public Vector2 buttonMargin = new Vector2(74f, 74f);   // 画面右下からの余白
    [Range(0.2f, 1f)]
    public float opacity = 0.9f;
    public Color buttonColor = new Color(0.72f, 0.55f, 0.95f);

    [Header("タップ領域のヒント")]
    public bool showTapHint = true;
    public string tapHint = "どこでもタップでジャンプ";
}
