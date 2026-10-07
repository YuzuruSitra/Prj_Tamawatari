using UnityEngine;

/// <summary>
/// InGame シーンにこのコンポーネントを1個置くだけで、必要なオブジェクトを出して配線する
/// (完全2D / ハロウィン「灯篭の道」)。
///
/// 見た目を持つものは <see cref="GameAssets"/> のプレハブを Instantiate するだけで、
/// ここで組み立てはしない。数値・色はこのコンポーネント上の各 Tuning でまとめて調整できる。
/// </summary>
public class InGameBootstrap : MonoBehaviour
{
    [Header("Tuning (Inspector で調整可)")]
    [SerializeField] private PlayerTuning playerTuning = new PlayerTuning();
    [SerializeField] private PlayerAnimTuning playerAnimTuning = new PlayerAnimTuning();
    [SerializeField] private IndicatorTuning indicatorTuning = new IndicatorTuning();
    [SerializeField] private PlatformTuning platformTuning = new PlatformTuning();
    [SerializeField] private EnemyTuning enemyTuning = new EnemyTuning();
    [SerializeField] private CameraTuning cameraTuning = new CameraTuning();
    [SerializeField] private AtmosphereTuning atmosphereTuning = new AtmosphereTuning();
    [SerializeField] private EventTuning eventTuning = new EventTuning();
    [SerializeField] private ScoreTuning scoreTuning = new ScoreTuning();
    [SerializeField] private AudioTuning audioTuning = new AudioTuning();
    [SerializeField] private SectionTuning sectionTuning = new SectionTuning();
    [SerializeField] private TouchTuning touchTuning = new TouchTuning();

    [Header("Toggles")]
    [Tooltip("まずジャンプ+足場だけを確認したいときは OFF")]
    [SerializeField] private bool spawnEnemies = true;

    private void Awake()
    {
        // シーンのデシリアライズで Tuning が壊れていた場合の保険
        if (playerTuning == null || playerTuning.maxJumpDistance <= 0f) playerTuning = new PlayerTuning();
        if (playerAnimTuning == null) playerAnimTuning = new PlayerAnimTuning();
        if (indicatorTuning == null || indicatorTuning.swingSpeed <= 0f) indicatorTuning = new IndicatorTuning();
        if (platformTuning == null || platformTuning.aheadCount <= 0) platformTuning = new PlatformTuning();
        if (enemyTuning == null || enemyTuning.spawnRadius <= 0f) enemyTuning = new EnemyTuning();
        if (cameraTuning == null || cameraTuning.orthographicSize <= 0f) cameraTuning = new CameraTuning();
        if (atmosphereTuning == null) atmosphereTuning = new AtmosphereTuning();
        if (eventTuning == null) eventTuning = new EventTuning();
        if (scoreTuning == null) scoreTuning = new ScoreTuning();
        if (audioTuning == null) audioTuning = new AudioTuning();
        if (sectionTuning == null) sectionTuning = new SectionTuning();
        if (touchTuning == null || touchTuning.buttonDiameter <= 0f) touchTuning = new TouchTuning();
        AudioManager.Tuning = audioTuning;
        AudioManager.StartBgm();
        if (platformTuning.palette == null || platformTuning.palette.Length == 0)
            platformTuning.palette = new PlatformTuning().palette;

        // --- GameManager ---
        var gm = new GameObject("GameManager").AddComponent<GameManager>();
        gm.Score = scoreTuning;

        // --- Player (プレハブ。root はスケール1で、見た目は Visual 子オブジェクト) ---
        var playerGo = GameAssets.Spawn(GameAssets.I != null ? GameAssets.I.player : null);
        if (playerGo == null)
        {
            Debug.LogError("[InGameBootstrap] Player プレハブが無いので開始できません。"
                           + " Tamawatari > アセット > 焼き直す を実行してください。");
            return;
        }

        var pc = playerGo.GetComponent<PlayerController>();
        pc.Tuning = playerTuning;

        var pCol = playerGo.GetComponent<CircleCollider2D>();
        pCol.radius = playerTuning.playerDiameter * GameArt.CircleVisualRadius;

        // 見た目の大きさと色は Tuning から流し込む(Visual のスケール = 直径)
        pc.Visual.localScale = Vector3.one * playerTuning.playerDiameter;
        pc.Visual.GetComponent<TintedParts>()?.SetTint(playerTuning.bodyColor);
        pc.CaptureRendererAlphas();          // 色を差し替えたので点滅の基準を取り直す

        playerGo.GetComponent<PlayerAnimator>().Init(pc, pc.Visual, playerAnimTuning);

        // --- 方向カーソル(矢印・振り子) ---
        var indicator = playerGo.GetComponentInChildren<JumpIndicator>(true);
        indicator.Center = playerGo.transform;
        indicator.Tuning = indicatorTuning;

        // --- PlatformSpawner (灯篭の道) ---
        var ps = new GameObject("PlatformSpawner").AddComponent<PlatformSpawner>();
        ps.Tuning = platformTuning;
        ps.Player = pc;

        // --- SoulSystem ---
        var soul = playerGo.GetComponent<SoulSystem>();
        soul.Init(playerTuning, ps, indicator, pc);

        pc.Indicator = indicator;
        pc.PlatformSpawner = ps;
        pc.Souls = soul;

        // 足場0をプレイヤー開始地点に作り、その上にプレイヤーを置く(z=0固定)
        Vector3 start = ps.Initialize(Vector2.zero);
        playerGo.transform.position = new Vector3(start.x, start.y, 0f);

        // --- EnemySpawner ---
        EnemySpawner es = null;
        if (spawnEnemies)
        {
            es = new GameObject("EnemySpawner").AddComponent<EnemySpawner>();
            es.Tuning = enemyTuning;
            es.Player = playerGo.transform;
        }

        // --- Camera (Orthographic 追従 + シェイク) ---
        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }
        var follow = cam.GetComponent<CameraFollow>() ?? cam.gameObject.AddComponent<CameraFollow>();
        follow.Tuning = cameraTuning;
        follow.Target = playerGo.transform;
        cam.transform.position = new Vector3(0f, cameraTuning.offset.y, -10f);

        // --- 空気感(灯の粉) ---
        AtmosphereFx atmos = null;
        if (atmosphereTuning.enabled)
        {
            atmos = new GameObject("AtmosphereFx").AddComponent<AtmosphereFx>();
            atmos.Init(cam, atmosphereTuning);
        }

        // --- スマホ用バーチャルパッド(タッチ環境でなければ隠れたまま) ---
        VirtualPad.Create(touchTuning);

        // --- HUD (プレハブ。UIManager はその Canvas に付いている) ---
        var hud = GameAssets.Spawn(GameAssets.I != null ? GameAssets.I.hudCanvas : null);
        var ui = hud != null ? hud.GetComponent<UIManager>() : null;
        if (ui != null)
        {
            ui.Player = pc;
            ui.PlatformSpawner = ps;
            ui.Souls = soul;
            ui.EventTuning = eventTuning;
        }

        // --- イベント(磁気嵐 / 大量発生 / 色替え / 濃霧) ---
        EventDirector events = null;
        if (eventTuning.enabled)
        {
            events = new GameObject("EventDirector").AddComponent<EventDirector>();
            events.Init(new GameEventContext
            {
                Player = pc,
                PlayerTransform = playerGo.transform,
                PlatformSpawner = ps,
                EnemySpawner = es,
                Atmosphere = atmos,
                Ui = ui,
            }, eventTuning);
            if (ui != null) ui.Events = events;
        }

        // --- 区間(大灯籠のチェックポイント) ---
        var section = new GameObject("SectionDirector").AddComponent<SectionDirector>();
        section.Init(sectionTuning, ps, es, events, ui, playerGo.transform);
        if (ui != null) ui.Sections = section;
    }
}
