using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// インゲームの HUD とリザルト(<c>Assets/Prefabs/UI/HudCanvas.prefab</c> のルートに付く)。
/// 画面の組み立てはプレハブ側が持っていて、ここは値の出し入れと演出だけを行う。
///  - 左上: DEPTH と KILLS のみ + 揺れるグラフ
///  - 左下: Shift の円形ゲージ
///  - 中央上: イベントの予告(2秒)と発生中バナー
///  - 終了時: RESULT(深度 ×10 / 成仏 ×50 とその合計スコア)
/// </summary>
public class UIManager : MonoBehaviour
{
    /// <summary>リザルトのスコア内訳(ラベル左寄せ / 数値右寄せで桁をそろえる)。</summary>
    [System.Serializable]
    public class ScorePanel
    {
        public Text Labels;
        public Text Values;
        public Text Total;
    }

    [Header("Refs (Bootstrap が配線する)")]
    [SerializeField] private PlayerController player;
    [SerializeField] private PlatformSpawner platformSpawner;

    public PlayerController Player { get => player; set => player = value; }
    public PlatformSpawner PlatformSpawner { get => platformSpawner; set => platformSpawner = value; }
    public SoulSystem Souls { get; set; }
    public EventDirector Events { get; set; }
    public SectionDirector Sections { get; set; }

    [Header("HUD")]
    [SerializeField] private Text depthText;
    [SerializeField] private Text killText;
    [SerializeField] private Text chargeText;
    [SerializeField] private RectTransform chargeFill;
    [SerializeField] private Image chargeGlow;
    [Tooltip("左上の揺れるグラフの棒")]
    [SerializeField] private Image[] graphBars;

    [Header("Shift 円形ゲージ")]
    [SerializeField] private Image gaugeGlow;
    [SerializeField] private Image gaugeFill;
    [SerializeField] private Image gaugeCool;
    [SerializeField] private Image gaugeTrack;
    [SerializeField] private Image[] gaugePips;
    [SerializeField] private Text gaugeLabel;

    [Header("イベント")]
    [SerializeField] private GameObject banner;
    [SerializeField] private Image bannerPlate;
    [SerializeField] private RectTransform bannerBarFill;
    [SerializeField] private Text bannerName;
    [SerializeField] private Text bannerDesc;
    [SerializeField] private GameObject warn;
    [SerializeField] private Image warnFlash;
    [SerializeField] private Image warnBandL;
    [SerializeField] private Image warnBandR;
    [SerializeField] private RectTransform warnBox;
    [SerializeField] private Text warnLead;
    [SerializeField] private Text warnName;
    [SerializeField] private Text warnDesc;

    [Header("区間 (セクション)")]
    [SerializeField] private Text sectionText;
    [SerializeField] private Text restText;
    [SerializeField] private GameObject sectionBanner;
    [SerializeField] private RectTransform sectionBannerRt;
    [SerializeField] private Text sectionBigText;
    [SerializeField] private Text sectionSubText;
    [SerializeField] private Image sectionRule;

    [Header("ポップ / 連続キル")]
    [SerializeField] private Text perfectText;
    [SerializeField] private Text streakText;
    [SerializeField] private Image streakBar;

    [Header("覆い (黄金の道 / 濃霧)")]
    [SerializeField] private Image warmGlow;
    [SerializeField] private Image fog;

    [Header("リザルト")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private GameObject clearContent;
    [SerializeField] private GameObject goContent;
    [SerializeField] private ScorePanel clearScore = new ScorePanel();
    [SerializeField] private ScorePanel goScore = new ScorePanel();
    [Tooltip("ゲームオーバー時にお化けがしゃべる吹き出しの文字")]
    [SerializeField] private Text goTaunt;

    private RectTransform _perfectRt, _streakRt, _fogRt, _canvasRt;
    private Vector2 _warnBoxBase;
    private bool _built;

    // 揺れるグラフ
    private float _climbRate, _prevDepth, _graphPulse;
    private int _prevKills;

    private RankingView _ranking;
    private bool _submitted;
    private int _submittedRank = -1;
    private float _bannerTime = -99f;
    private int _shownStreak = -1;

    // 黄金の道の暖色オーバーレイ / 汎用ポップ
    private bool _warmOn;
    private float _warm01;
    private float _popTime = -99f;
    private string _popText = "";
    private Color _popColor = Color.white;

    // 濃霧
    private bool _fogOn;
    private float _fog01;
    public EventTuning EventTuning { get; set; } = new EventTuning();

    private static readonly Color Cream = new Color(0.94f, 0.90f, 0.83f);
    private static readonly Color Ember = new Color(1f, 0.68f, 0.28f);
    private static readonly Color Plum = new Color(0.72f, 0.55f, 0.95f);

    private static readonly string[] Taunts =
    {
        "またおちたの?  へたっぴ〜!",
        "もう おしまい?  はやいね〜",
        "あははっ、そこ あぶないよ?",
        "たまわたり、しっぱ〜い!",
        "つぎは がんばってね〜  くくく",
        "ざんね〜ん!  また あそぼ?",
    };

    /// <summary>ランキング上位に入ったときは煽らずに褒める。</summary>
    private static readonly string[] Praises =
    {
        "やるじゃない!  みごとな たまわたり!",
        "すごい…  ここまで来るなんて!",
        "みとめる!  きみは たいしたものだ",
        "あっぱれ!  灯篭が よろこんでる",
        "おみごと〜!  また 見せてね",
        "つよい…  つぎは 本気で いくからね!",
    };

    // ==================== 立ち上げ ====================
    private void Start()
    {
        if (_built) return;
        _built = true;

        _canvasRt = (RectTransform)transform;
        _perfectRt = perfectText != null ? perfectText.rectTransform : null;
        _streakRt = streakText != null ? streakText.rectTransform : null;
        _fogRt = fog != null ? fog.rectTransform : null;
        if (warnBox != null) _warnBoxBase = warnBox.anchoredPosition;

        SizeFog();
        _ranking = RankingView.Create(transform, "{S} : 閉じる      {C} : タイトルへ");

        resultPanel.SetActive(false);
        banner.SetActive(false);
        warn.SetActive(false);
        sectionBanner.SetActive(false);
        warmGlow.gameObject.SetActive(false);
        fog.gameObject.SetActive(false);
    }

    /// <summary>
    /// 濃霧イベント用の覆い。中心に小さな穴が開いた画像なので、
    /// 見せたい半径から必要な大きさを逆算して置き直す。
    /// </summary>
    private void SizeFog()
    {
        if (_fogRt == null) return;
        float hole = Mathf.Max(60f, EventTuning != null ? EventTuning.fogHoleRadius : 250f);
        float size = hole / GameArt.FogCoreT * 2f;
        _fogRt.sizeDelta = new Vector2(size, size);
    }

    /// <summary>イベントから呼ぶ。濃霧の ON/OFF。</summary>
    public void SetFog(bool on) => _fogOn = on;

    /// <summary>イベントから呼ぶ。黄金の道の暖かい発光。</summary>
    public void SetWarmGlow(bool on) => _warmOn = on;

    /// <summary>イベントから呼ぶ。画面中央に一言ポップさせる。</summary>
    public void PopMessage(string text, Color color)
    {
        _popText = text;
        _popColor = color;
        _popTime = Time.time;
    }

    private void UpdateWarmGlow()
    {
        _warm01 = Mathf.MoveTowards(_warm01, _warmOn ? 1f : 0f, Time.deltaTime / 0.6f);
        bool vis = _warm01 > 0.001f;
        if (warmGlow.gameObject.activeSelf != vis) warmGlow.gameObject.SetActive(vis);
        if (!vis) return;
        float pulse = 0.82f + 0.18f * Mathf.Sin(Time.time * 3.2f);
        warmGlow.color = new Color(1f, 0.72f, 0.28f, 0.34f * _warm01 * pulse);
    }

    private void UpdateFog()
    {
        float fade = Mathf.Max(0.05f, EventTuning != null ? EventTuning.fogFadeTime : 0.8f);
        _fog01 = Mathf.MoveTowards(_fog01, _fogOn ? 1f : 0f, Time.deltaTime / fade);

        bool visible = _fog01 > 0.001f;
        if (fog.gameObject.activeSelf != visible) fog.gameObject.SetActive(visible);
        if (!visible) return;

        Color c = EventTuning != null ? EventTuning.fogColor : new Color(0.05f, 0.05f, 0.09f, 0.97f);
        fog.color = new Color(c.r, c.g, c.b, c.a * _fog01);

        // 穴をプレイヤーの画面位置に合わせる
        var cam = Camera.main;
        if (cam == null || player == null || _canvasRt == null) return;
        Vector3 sp = cam.WorldToScreenPoint(player.transform.position);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, sp, null, out Vector2 lp))
            _fogRt.anchoredPosition = lp;
    }

    /// <summary>連続キルの演出。段が上がるほど大きく・熱く・画面が発光する。</summary>
    private void UpdateKillStreak(GameManager gm)
    {
        int s = gm.KillStreak;
        float since = Time.time - gm.LastKillTime;

        if (s <= 1 && since > 0.9f)
        {
            _shownStreak = s;
            Fade(streakText, 0f);
            Fade(streakBar, 0f);
            return;
        }

        Color hot;
        string tag;
        if (s >= 10) { hot = new Color(1f, 0.95f, 0.85f); tag = " !!!"; }
        else if (s >= 7) { hot = new Color(1f, 0.42f, 0.18f); tag = " !!"; }
        else if (s >= 4) { hot = new Color(1f, 0.58f, 0.16f); tag = " !"; }
        else { hot = new Color(1f, 0.78f, 0.3f); tag = ""; }

        float pop = Mathf.Exp(-since * 9f);                        // 倒した瞬間に弾む
        float grow = Mathf.Min(1.9f, 1f + 0.07f * (s - 1));        // 段が上がるほど大きく
        float alpha = Mathf.Clamp01(gm.KillStreakRemain01 * 1.6f);

        if (s != _shownStreak)
        {
            _shownStreak = s;
            streakText.text = $"{s}  KILL{tag}";
            streakText.fontSize = Mathf.RoundToInt(Mathf.Min(96f, 40f + 5.2f * s));
        }
        streakText.color = new Color(hot.r, hot.g, hot.b, alpha);
        float sc = grow * (1f + 0.28f * pop);
        _streakRt.localScale = new Vector3(sc, sc, 1f);
        _streakRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 22f) * 2.2f * pop);

        streakBar.color = new Color(hot.r, hot.g, hot.b, alpha * 0.85f);
        streakBar.rectTransform.anchorMin = new Vector2(1f - gm.KillStreakRemain01, 0f);
    }

    private static void Fade(Graphic g, float a)
    {
        if (g != null) g.color = GameArt.WithAlpha(g.color, a);
    }

    /// <summary>SectionDirector から呼ばれる。</summary>
    public void ShowSectionBanner(int clearedSection, int bonus)
    {
        if (sectionBigText == null) return;
        _bannerTime = Time.unscaledTime;
        sectionBigText.text = $"SECTION  {clearedSection}  CLEAR";
        sectionSubText.text = $"区間ボーナス  +{bonus}";
        sectionBanner.SetActive(true);
    }

    private void UpdateSectionUi()
    {
        if (Sections == null) return;

        // HUD の区間表示
        if (Sections.IsResting)
            sectionText.text = $"SECTION {Sections.Section}   —  一息  —";
        else if (Sections.HasTarget)
            sectionText.text = $"SECTION {Sections.Section}   次の大灯籠まで {Sections.DistanceToTarget:F0} m";
        else
            sectionText.text = $"SECTION {Sections.Section}";

        if (sectionBanner == null) return;

        float age = Time.unscaledTime - _bannerTime;
        // 一息タイムが終わるまでは幕を残す
        float life = Mathf.Max(2.6f, Sections.Tuning.restDuration + 0.7f);
        bool show = age >= 0f && age < life;
        if (sectionBanner.activeSelf != show) sectionBanner.SetActive(show);
        if (!show) return;

        float pop = Mathf.Exp(-age * 7f);
        float fade = Mathf.Clamp01(Mathf.Min(age / 0.15f, (life - age) / 0.5f));
        float sc = 1f + 0.3f * pop;
        sectionBannerRt.localScale = new Vector3(sc, sc, 1f);
        sectionBigText.color = new Color(1f, 0.9f, 0.55f, fade);
        sectionSubText.color = GameArt.WithAlpha(Cream, fade);
        sectionRule.color = GameArt.WithAlpha(Ember, 0.9f * fade);
        restText.color = new Color(0.8f, 0.76f, 0.72f, fade);
        restText.text = Sections.IsResting
            ? $"一息...  {Sections.RestRemain01 * 100f:F0}%"
            : "次の区間へ";
    }

    // ==================== Update ====================
    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || !_built) return;

        if (gm.IsPlaying)
        {
            if (resultPanel.activeSelf) resultPanel.SetActive(false);

            depthText.text = $"DEPTH  {gm.DepthMeters:F1} m";
            killText.text = $"KILLS  {gm.GhostsDefeated}";
            UpdateGraph(gm);

            float ch = player != null ? player.Charge01 : 0f;
            bool charging = player != null && player.IsCharging;
            chargeFill.anchorMax = new Vector2(Mathf.Clamp01(ch), 1f);
            chargeGlow.color = new Color(1f, 0.6f, 0.2f, charging ? 0.10f + 0.28f * ch : 0f);
            chargeText.text = charging
                ? $"CHARGE  {ch:0.00}   [LOCKED]   {InputHub.SpecialLabel} = burst"
                : $"CHARGE  {ch:0.00}";

            UpdateGauge();
            UpdateEventUi();
            UpdatePerfectPop();
            UpdateKillStreak(gm);
            UpdateSectionUi();
            UpdateFog();
            UpdateWarmGlow();
        }
        else
        {
            _fogOn = false;
            _warmOn = false;
            Fade(streakText, 0f);
            Fade(streakBar, 0f);
            UpdateFog();
            UpdateWarmGlow();
            bool clear = gm.IsCleared;
            if (!resultPanel.activeSelf)
            {
                resultPanel.SetActive(true);
                clearContent.SetActive(clear);
                goContent.SetActive(!clear);
                banner.SetActive(false);
                warn.SetActive(false);
                sectionBanner.SetActive(false);
                perfectText.color = GameArt.WithAlpha(perfectText.color, 0f);

                // セリフは順位が確定してから決めたいので、先に登録する
                if (!_submitted)
                {
                    _submitted = true;
                    _submittedRank = ScoreBoard.Submit(new ScoreEntry
                    {
                        Name = ScoreBoard.PlayerName,
                        Score = gm.TotalScore,
                        DepthX10 = Mathf.RoundToInt(gm.DepthMeters * 10f),
                        Kills = gm.GhostsDefeated,
                        MaxCombo = gm.MaxCombo,
                        Cleared = clear,
                    });
                }

                if (!clear && goTaunt != null)
                {
                    bool top3 = _submittedRank >= 0 && _submittedRank < 3;
                    goTaunt.text = top3
                        ? $"{_submittedRank + 1}位!  " + Praises[Random.Range(0, Praises.Length)]
                        : Taunts[Random.Range(0, Taunts.Length)];
                    goTaunt.color = top3
                        ? new Color(0.55f, 0.30f, 0.06f)     // 褒めるときは温かい色
                        : new Color(0.16f, 0.13f, 0.2f);
                }
            }
            FillScore(clear ? clearScore : goScore, gm);

            // Shift でランキングを開閉(画像のコピーはランキング側が持つ)
            if (InputHub.SpecialPressed && _ranking != null)
            {
                AudioManager.PlayUi();
                _ranking.Toggle(_submittedRank);
            }
        }
    }

    private void UpdatePerfectPop()
    {
        float perfTime = player != null ? player.LastPerfectTime : -99f;
        bool useOverride = _popTime > perfTime;
        float src = useOverride ? _popTime : perfTime;

        float age = Time.time - src;
        const float life = 0.95f;
        if (src < 0f || age < 0f || age > life)
        {
            if (perfectText.color.a > 0f) perfectText.color = GameArt.WithAlpha(perfectText.color, 0f);
            return;
        }

        float k = age / life;
        Color col = useOverride ? _popColor : new Color(1f, 0.88f, 0.42f);
        perfectText.text = useOverride
            ? _popText
            : $"PERFECT !   x{player.LastPerfectCombo}   +{player.LastPerfectAdd}";
        perfectText.color = new Color(col.r, col.g, col.b, Mathf.Clamp01(1f - k * k));
        float pop = 1f + 0.35f * Mathf.Exp(-age * 12f);
        _perfectRt.localScale = new Vector3(pop, pop, 1f);
        _perfectRt.anchoredPosition = new Vector2(0f, 150f + 40f * k);
    }

    private static void FillScore(ScorePanel s, GameManager gm)
    {
        if (s == null || s.Labels == null) return;
        s.Labels.text = $"深度  {gm.DepthMeters:F1} m   × {gm.Score.depthMultiplier}\n" +
                        $"成仏  {gm.GhostsDefeated} 体   × {gm.Score.killMultiplier}\n" +
                        "ボーナス";
        s.Values.text = $"{gm.DepthScore}\n{gm.KillScore}\n{gm.BonusScore}";
        s.Total.text = $"SCORE   {gm.TotalScore}";
    }

    private void UpdateGraph(GameManager gm)
    {
        float dt = Mathf.Max(0.0001f, Time.deltaTime);
        float rate = (gm.DepthMeters - _prevDepth) / dt;
        _prevDepth = gm.DepthMeters;
        _climbRate = Mathf.Lerp(_climbRate, rate, 1f - Mathf.Exp(-4f * dt));

        if (gm.GhostsDefeated > _prevKills) _graphPulse = 1f;
        _prevKills = gm.GhostsDefeated;
        _graphPulse = Mathf.MoveTowards(_graphPulse, 0f, dt * 1.4f);

        float act = Mathf.Clamp01(_climbRate * 0.22f) + _graphPulse;
        float t = Time.time;

        for (int i = 0; i < graphBars.Length; i++)
        {
            float wave = Mathf.Abs(Mathf.Sin(t * (2.4f + i * 0.21f) + i * 0.62f));
            float noise = Mathf.PerlinNoise(i * 0.35f, t * 1.7f);
            float h = 5f + (7f + 40f * act) * (0.30f + 0.70f * wave) * (0.55f + 0.65f * noise);
            graphBars[i].rectTransform.sizeDelta = new Vector2(13f, Mathf.Clamp(h, 4f, 62f));
            graphBars[i].color = Color.Lerp(GameArt.WithAlpha(Ember, 0.55f),
                                            new Color(1f, 0.9f, 0.55f, 0.95f), Mathf.Clamp01(act));
        }
    }

    private void UpdateGauge()
    {
        int souls = Souls != null ? Souls.Count : 0;
        int maxS = Souls != null ? Souls.MaxSouls : 3;
        bool ready = Souls != null && Souls.IsFull;
        float hold = Souls != null ? Souls.MergeCharge01 : 0f;
        float cool = player != null ? player.ShockReady01 : 1f;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4.5f);

        gaugeFill.fillAmount = hold;
        gaugeFill.color = ready ? Color.Lerp(Plum, new Color(1f, 0.85f, 0.35f), 0.35f + 0.65f * pulse) : Plum;
        gaugeTrack.color = new Color(1f, 1f, 1f, ready ? 0.22f : 0.12f);
        gaugeCool.fillAmount = cool;
        gaugeCool.color = new Color(1f, 0.68f, 0.28f, cool >= 1f ? 0.7f : 0.3f);
        gaugeGlow.color = new Color(1f, 0.78f, 0.35f, ready ? 0.14f + 0.2f * pulse : 0.03f);

        int n = Mathf.Clamp(maxS, 1, gaugePips.Length);
        for (int i = 0; i < gaugePips.Length; i++)
        {
            bool use = i < n;
            if (gaugePips[i].gameObject.activeSelf != use) gaugePips[i].gameObject.SetActive(use);
            if (!use) continue;

            float ang = (90f - i * (360f / n)) * Mathf.Deg2Rad;
            gaugePips[i].rectTransform.anchoredPosition = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 40f;
            gaugePips[i].color = i < souls ? new Color(1f, 0.78f, 0.32f, 0.95f) : new Color(1f, 1f, 1f, 0.16f);
        }

        gaugeLabel.text = ready
            ? $"HOLD {InputHub.SpecialLabel}  {hold * 100f:F0}%"
            : $"{InputHub.SpecialLabel}  {souls}/{n}";
        gaugeLabel.color = ready ? new Color(1f, 0.85f, 0.4f) : Cream;
    }

    private void UpdateEventUi()
    {
        bool warning = Events != null && Events.IsWarning;
        bool active = Events != null && Events.HasActive;

        if (warn.activeSelf != warning) warn.SetActive(warning);
        if (banner.activeSelf != active) banner.SetActive(active);

        if (warning)
        {
            float w = Events.WarningProgress01;         // 0 -> 1
            Color tint = Events.PendingTint;
            float t = Time.time;

            warnName.text = Events.PendingName;
            warnName.color = tint;
            warnDesc.text = Events.PendingDescription;
            warnLead.color = Color.Lerp(Cream, tint, 0.5f + 0.5f * Mathf.Sin(t * 12f));

            // 出てくる勢い + 小刻みな震え + 明滅
            float pop = 1f + 0.22f * Mathf.Exp(-w * 9f) + 0.03f * Mathf.Sin(t * 16f);
            warnBox.localScale = new Vector3(pop, pop, 1f);
            float shake = (0.35f + 0.65f * w) * 5f;
            warnBox.anchoredPosition = _warnBoxBase + new Vector2(Random.Range(-shake, shake), Random.Range(-shake, shake));

            float band = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(w * 4f));
            warnBandL.rectTransform.sizeDelta = new Vector2(1040f * band, 6f);
            warnBandR.rectTransform.sizeDelta = new Vector2(1040f * band, 6f);
            warnBandL.color = tint;
            warnBandR.color = tint;

            float flash = 0.06f + 0.08f * Mathf.Abs(Mathf.Sin(t * 14f)) * (0.4f + 0.6f * w);
            warnFlash.color = new Color(tint.r, tint.g, tint.b, flash);
        }

        if (active)
        {
            Color tint = Events.ActiveTint;
            bannerName.text = Events.ActiveName;
            bannerName.color = tint;
            bannerDesc.text = Events.ActiveDescription;
            bannerPlate.color = new Color(tint.r * 0.16f, tint.g * 0.16f, tint.b * 0.2f, 0.92f);
            bannerBarFill.anchorMax = new Vector2(Mathf.Clamp01(Events.ActiveRemain01), 1f);
            var fillImg = bannerBarFill.GetComponent<Image>();
            if (fillImg != null) fillImg.color = tint;
        }
    }

#if UNITY_EDITOR
    // ==================== 焼き直しツール用 ====================
    // プレハブを組み立てたあとに、各パーツの参照をここから差し込む。

    public void SetHudRefs(Text depth, Text kills, Text charge, RectTransform chargeBar, Image chargeHalo, Image[] bars)
    {
        depthText = depth; killText = kills; chargeText = charge;
        chargeFill = chargeBar; chargeGlow = chargeHalo; graphBars = bars;
    }

    public void SetGaugeRefs(Image halo, Image fill, Image cool, Image track, Image[] pips, Text label)
    {
        gaugeGlow = halo; gaugeFill = fill; gaugeCool = cool; gaugeTrack = track; gaugePips = pips; gaugeLabel = label;
    }

    public void SetEventRefs(GameObject bannerRoot, Image plate, RectTransform barFill, Text nameText, Text descText,
                             GameObject warnRoot, Image flash, Image bandL, Image bandR, RectTransform box,
                             Text lead, Text warningName, Text warningDesc)
    {
        banner = bannerRoot; bannerPlate = plate; bannerBarFill = barFill; bannerName = nameText; bannerDesc = descText;
        warn = warnRoot; warnFlash = flash; warnBandL = bandL; warnBandR = bandR; warnBox = box;
        warnLead = lead; warnName = warningName; warnDesc = warningDesc;
    }

    public void SetSectionRefs(Text hud, Text rest, GameObject bannerRoot, RectTransform bannerRt,
                               Text big, Text sub, Image rule)
    {
        sectionText = hud; restText = rest; sectionBanner = bannerRoot; sectionBannerRt = bannerRt;
        sectionBigText = big; sectionSubText = sub; sectionRule = rule;
    }

    public void SetPopRefs(Text perfect, Text streak, Image streakFill, Image warm, Image fogOverlay)
    {
        perfectText = perfect; streakText = streak; streakBar = streakFill; warmGlow = warm; fog = fogOverlay;
    }

    public void SetResultRefs(GameObject panel, GameObject clear, GameObject over,
                              ScorePanel clearPanel, ScorePanel overPanel, Text taunt)
    {
        resultPanel = panel; clearContent = clear; goContent = over;
        clearScore = clearPanel; goScore = overPanel; goTaunt = taunt;
    }
#endif
}
