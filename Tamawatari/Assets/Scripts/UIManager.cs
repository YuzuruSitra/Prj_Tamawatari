using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ランタイムで uGUI(Canvas)を組み立てる UI(ハロウィン「灯篭の道」トーン)。
///  - 左上: DEPTH と KILLS のみ + 揺れるグラフ
///  - 左下: Shift の円形ゲージ
///  - 中央上: イベントの予告(2秒)と発生中バナー
///  - 終了時: RESULT(深度 ×10 / 成仏 ×50 とその合計スコア)
/// </summary>
public class UIManager : MonoBehaviour
{
    private class ScorePanel { public Text Labels, Values, Total; }

    [SerializeField] private PlayerController player;
    [SerializeField] private PlatformSpawner platformSpawner;

    public PlayerController Player { get => player; set => player = value; }
    public PlatformSpawner PlatformSpawner { get => platformSpawner; set => platformSpawner = value; }
    public SoulSystem Souls { get; set; }
    public EventDirector Events { get; set; }
    public SectionDirector Sections { get; set; }

    private Text _depthText, _killText, _chargeText;
    private RectTransform _chargeFill;
    private Image _chargeGlow;

    // 揺れるグラフ
    private RectTransform[] _graphBars;
    private Image[] _graphImgs;
    private float _climbRate, _prevDepth, _graphPulse;
    private int _prevKills;

    // Shift 円形ゲージ
    private Image _gaugeGlow, _gaugeFill, _gaugeCool, _gaugeTrack;
    private Image[] _gaugePips = new Image[8];
    private Text _gaugeLabel;

    // イベント
    private GameObject _banner, _warn;
    private Image _bannerPlate, _warnFlash, _warnBandL, _warnBandR;
    private RectTransform _bannerBarFill, _warnBox;
    private Text _bannerName, _bannerDesc, _warnLead, _warnName, _warnDesc;
    private Vector2 _warnBoxBase;

    private GameObject _resultPanel, _clearContent, _goContent;
    private ScorePanel _clearScore, _goScore;
    private Text _goTaunt;
    private bool _built;

    // PERFECT ポップと ランキング
    private Text _perfectText;
    private RectTransform _perfectRt;
    private RankingView _ranking;
    private bool _submitted;
    private int _submittedRank = -1;

    // 区間(セクション)
    private Text _sectionText, _restText;
    private GameObject _sectionBanner;
    private Text _sectionBigText, _sectionSubText;
    private RectTransform _sectionBannerRt;
    private Image _sectionRule;
    private float _bannerTime = -99f;

    // 連続キル演出
    private Text _streakText;
    private RectTransform _streakRt;
    private Image _streakBar;
    private int _shownStreak = -1;

    // 黄金の道の暖色オーバーレイ / 汎用ポップ
    private Image _warmGlow;
    private bool _warmOn;
    private float _warm01;
    private float _popTime = -99f;
    private string _popText = "";
    private Color _popColor = Color.white;

    // 濃霧
    private Image _fog;
    private RectTransform _fogRt, _canvasRt;
    private bool _fogOn;
    private float _fog01;
    public EventTuning EventTuning { get; set; } = new EventTuning();

    private static readonly Color Cream = new Color(0.94f, 0.90f, 0.83f);
    private static readonly Color Ember = new Color(1f, 0.68f, 0.28f);
    private static readonly Color Wisp = new Color(0.62f, 0.80f, 1f);
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

    private void Start() => Build();

    // ==================== Build ====================
    private void Build()
    {
        if (_built) return;
        _built = true;

        var canvas = MockUtil.CreateCanvas("HUDCanvas");
        var root = canvas.transform;
        _canvasRt = (RectTransform)canvas.transform;

        MockUtil.CreateImage(root, new Color(0.02f, 0.01f, 0.04f, 0.92f),
            Vector2.zero, Vector2.one, new Vector2(-140, -140), new Vector2(140, 140),
            "Vignette", MockUtil.VignetteSprite);

        BuildFog(root);
        _warmGlow = MockUtil.CreateImage(root, new Color(1f, 0.72f, 0.28f, 0f),
            Vector2.zero, Vector2.one, new Vector2(-220, -220), new Vector2(220, 220),
            "WarmGlow", MockUtil.VignetteSprite);
        _warmGlow.gameObject.SetActive(false);
        BuildHud(root);
        BuildShiftGauge(root);
        BuildBanner(root);
        BuildWarning(root);
        BuildSectionBanner(root);
        BuildResult(root);
        _ranking = RankingView.Create(root, "{S} : 閉じる      {C} : タイトルへ");

        _resultPanel.SetActive(false);
        _banner.SetActive(false);
        _warn.SetActive(false);
        _sectionBanner.SetActive(false);
    }

    /// <summary>濃霧イベント用の覆い。中心の穴だけが見え、それをプレイヤーに追従させる。</summary>
    private void BuildFog(Transform root)
    {
        float hole = Mathf.Max(60f, EventTuning != null ? EventTuning.fogHoleRadius : 250f);
        float size = hole / MockUtil.FogCoreT * 2f;      // 穴の半径から必要な画像サイズを逆算

        _fog = MockUtil.CreateBox(root, new Color(0.05f, 0.05f, 0.09f, 0f), Vector2.zero,
            new Vector2(size, size), "Fog", sprite: MockUtil.FogSprite);
        _fogRt = _fog.rectTransform;
        _fog.gameObject.SetActive(false);
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
        if (_warmGlow.gameObject.activeSelf != vis) _warmGlow.gameObject.SetActive(vis);
        if (!vis) return;
        float pulse = 0.82f + 0.18f * Mathf.Sin(Time.time * 3.2f);
        _warmGlow.color = new Color(1f, 0.72f, 0.28f, 0.34f * _warm01 * pulse);
    }

    private void UpdateFog()
    {
        float fade = Mathf.Max(0.05f, EventTuning != null ? EventTuning.fogFadeTime : 0.8f);
        _fog01 = Mathf.MoveTowards(_fog01, _fogOn ? 1f : 0f, Time.deltaTime / fade);

        bool visible = _fog01 > 0.001f;
        if (_fog.gameObject.activeSelf != visible) _fog.gameObject.SetActive(visible);
        if (!visible) return;

        Color c = EventTuning != null ? EventTuning.fogColor : new Color(0.05f, 0.05f, 0.09f, 0.97f);
        _fog.color = new Color(c.r, c.g, c.b, c.a * _fog01);

        // 穴をプレイヤーの画面位置に合わせる
        var cam = Camera.main;
        if (cam == null || player == null || _canvasRt == null) return;
        Vector3 sp = cam.WorldToScreenPoint(player.transform.position);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, sp, null, out Vector2 lp))
            _fogRt.anchoredPosition = lp;
    }

    private void BuildHud(Transform root)
    {
        MockUtil.CreateImage(root, new Color(0.05f, 0.04f, 0.08f, 0.5f),
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -196), new Vector2(438, -12), "HudPlate");
        MockUtil.CreateImage(root, MockUtil.WithAlpha(Ember, 0.85f),
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -196), new Vector2(17, -12), "HudAccent");

        _depthText = MockUtil.CreateText(root, "DEPTH  0.0 m", 40, TextAnchor.UpperLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -74), new Vector2(430, -24), Ember, display: true);
        _killText = MockUtil.CreateText(root, "KILLS  0", 28, TextAnchor.UpperLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -112), new Vector2(430, -72), Wisp, display: true);
        _sectionText = MockUtil.CreateText(root, "", 22, TextAnchor.UpperLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -144), new Vector2(430, -110),
            new Color(0.86f, 0.78f, 0.62f));

        // 揺れるグラフ
        var graph = MockUtil.CreateRect(root, "HudGraph",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -186), new Vector2(426, -118));
        MockUtil.CreateImage(graph, new Color(1f, 1f, 1f, 0.10f),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 2), "baseline");

        const int N = 18;
        _graphBars = new RectTransform[N];
        _graphImgs = new Image[N];
        for (int i = 0; i < N; i++)
        {
            var img = MockUtil.CreateBox(graph, Ember, Vector2.zero, new Vector2(13, 10), $"bar{i}");
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(11f + i * 21.5f, 2f);
            _graphBars[i] = rt;
            _graphImgs[i] = img;
        }

        // チャージ
        _chargeGlow = MockUtil.CreateImage(root, new Color(1f, 0.6f, 0.2f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-380, -30), new Vector2(380, 190),
            "ChargeGlow", MockUtil.GlowSprite);
        var barBg = MockUtil.CreateImage(root, new Color(0.05f, 0.04f, 0.08f, 0.8f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-262, 46), new Vector2(262, 86), "ChargeTrack");
        _chargeFill = MockUtil.CreateImage(barBg.transform, Ember,
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(5, 5), new Vector2(-5, -5), "ChargeFill").rectTransform;
        _chargeText = MockUtil.CreateText(root, "CHARGE  0.00", 21, TextAnchor.LowerCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-300, 88), new Vector2(300, 120), Cream);

        // PERFECT ポップ(画面中央やや上)
        _perfectText = MockUtil.CreateText(root, "", 46, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420, 110), new Vector2(420, 190),
            new Color(1f, 0.88f, 0.42f, 0f), display: true);
        _perfectRt = _perfectText.rectTransform;

        // 連続キルの盛り上がり（画面右手）
        _streakText = MockUtil.CreateText(root, "", 54, TextAnchor.MiddleRight,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-620, -30), new Vector2(-40, 80),
            new Color(1f, 0.7f, 0.2f, 0f), display: true);
        _streakRt = _streakText.rectTransform;
        var streakTrack = MockUtil.CreateImage(root, new Color(1f, 1f, 1f, 0.06f),
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-300, -50), new Vector2(-40, -42), "StreakTrack");
        _streakBar = MockUtil.CreateImage(streakTrack.transform, new Color(1f, 0.6f, 0.15f, 0f),
            new Vector2(1, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, "StreakBar");
    }

    /// <summary>連続キルの演出。段が上がるほど大きく・熱く・画面が発光する。</summary>
    private void UpdateKillStreak(GameManager gm)
    {
        int s = gm.KillStreak;
        float since = Time.time - gm.LastKillTime;

        if (s <= 1 && since > 0.9f)
        {
            _shownStreak = s;
            Fade(_streakText, 0f);
            Fade(_streakBar, 0f);
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
            _streakText.text = $"{s}  KILL{tag}";
            _streakText.fontSize = Mathf.RoundToInt(Mathf.Min(96f, 40f + 5.2f * s));
        }
        _streakText.color = new Color(hot.r, hot.g, hot.b, alpha);
        float sc = grow * (1f + 0.28f * pop);
        _streakRt.localScale = new Vector3(sc, sc, 1f);
        _streakRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 22f) * 2.2f * pop);

        _streakBar.color = new Color(hot.r, hot.g, hot.b, alpha * 0.85f);
        _streakBar.rectTransform.anchorMin = new Vector2(1f - gm.KillStreakRemain01, 0f);
    }

    private static void Fade(Graphic g, float a)
    {
        if (g != null) g.color = MockUtil.WithAlpha(g.color, a);
    }

    private void BuildShiftGauge(Transform root)
    {
        var g = MockUtil.CreateRect(root, "ShiftGauge",
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 34), new Vector2(228, 234));

        _gaugeGlow = MockUtil.CreateBox(g, new Color(1f, 0.75f, 0.3f, 0f), Vector2.zero, new Vector2(300, 300),
            "gaugeGlow", sprite: MockUtil.GlowSprite);
        MockUtil.CreateBox(g, new Color(0.05f, 0.04f, 0.09f, 0.72f), Vector2.zero, new Vector2(190, 190),
            "gaugeBack", circle: true);
        _gaugeTrack = MockUtil.CreateBox(g, new Color(1f, 1f, 1f, 0.12f), Vector2.zero, new Vector2(176, 176),
            "gaugeTrack", sprite: MockUtil.RingSprite);

        _gaugeFill = MockUtil.CreateBox(g, Plum, Vector2.zero, new Vector2(176, 176),
            "gaugeFill", sprite: MockUtil.RingSprite);
        MakeRadial(_gaugeFill);

        _gaugeCool = MockUtil.CreateBox(g, new Color(1f, 0.68f, 0.28f, 0.65f), Vector2.zero, new Vector2(126, 126),
            "gaugeCooldown", sprite: MockUtil.RingSprite);
        MakeRadial(_gaugeCool);

        for (int i = 0; i < _gaugePips.Length; i++)
        {
            _gaugePips[i] = MockUtil.CreateBox(g, new Color(1f, 1f, 1f, 0.18f), Vector2.zero,
                new Vector2(26, 26), $"pip{i}", circle: true);
            _gaugePips[i].gameObject.SetActive(false);
        }

        _gaugeLabel = MockUtil.CreateText(g, "", 20, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-110, -128), new Vector2(110, -96), Cream);
    }

    private static void MakeRadial(Image img)
    {
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial360;
        img.fillOrigin = (int)Image.Origin360.Top;
        img.fillClockwise = true;
        img.fillAmount = 0f;
    }

    private void BuildBanner(Transform root)
    {
        var b = MockUtil.CreateRect(root, "EventBanner",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-390, -128), new Vector2(390, -20));
        _banner = b.gameObject;

        _bannerPlate = MockUtil.CreateBox(b, new Color(0.08f, 0.07f, 0.14f, 0.9f), Vector2.zero, new Vector2(780, 108), "plate");
        MockUtil.CreateBox(b, new Color(1f, 1f, 1f, 0.9f), new Vector2(0, 52), new Vector2(780, 4), "line");

        _bannerName = MockUtil.CreateText(b, "", 30, TextAnchor.UpperCenter,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -46), new Vector2(0, -6), Wisp);
        _bannerDesc = MockUtil.CreateText(b, "", 20, TextAnchor.UpperCenter,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -76), new Vector2(0, -44), Cream);

        var barTrack = MockUtil.CreateBox(b, new Color(1f, 1f, 1f, 0.14f), new Vector2(0, -42), new Vector2(720, 8), "barTrack");
        _bannerBarFill = MockUtil.CreateImage(barTrack.transform, Wisp,
            new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, Vector2.zero, "barFill").rectTransform;
    }

    /// <summary>イベント発生2秒前の強調表示。</summary>
    private void BuildWarning(Transform root)
    {
        var w = MockUtil.CreateRect(root, "EventWarning", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _warn = w.gameObject;

        _warnFlash = MockUtil.CreateImage(w, new Color(1f, 1f, 1f, 0f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "flash");

        _warnBox = MockUtil.CreateRect(w, "warnBox",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520, 40), new Vector2(520, 260));
        _warnBoxBase = _warnBox.anchoredPosition;

        MockUtil.CreateBox(_warnBox, new Color(0.03f, 0.02f, 0.06f, 0.82f), Vector2.zero, new Vector2(1040, 220), "plate");
        _warnBandL = MockUtil.CreateBox(_warnBox, Wisp, new Vector2(0, 96), new Vector2(1040, 6), "bandTop");
        _warnBandR = MockUtil.CreateBox(_warnBox, Wisp, new Vector2(0, -96), new Vector2(1040, 6), "bandBottom");

        _warnLead = MockUtil.CreateText(_warnBox, "!  E V E N T   I N C O M I N G  !", 28, TextAnchor.UpperCenter,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -52), new Vector2(0, -10), Cream, display: true);
        _warnName = MockUtil.CreateText(_warnBox, "", 52, TextAnchor.MiddleCenter,
            new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -34), new Vector2(0, 34), Wisp);
        _warnDesc = MockUtil.CreateText(_warnBox, "", 22, TextAnchor.LowerCenter,
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 18), new Vector2(0, 56), Cream);
    }

    /// <summary>区間クリアの幕。一息つける間だけ出る。</summary>
    private void BuildSectionBanner(Transform root)
    {
        var b = MockUtil.CreateRect(root, "SectionBanner",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520, 30), new Vector2(520, 230));
        _sectionBanner = b.gameObject;
        _sectionBannerRt = b;

        MockUtil.CreateBox(b, new Color(1f, 0.84f, 0.42f, 0.22f), Vector2.zero, new Vector2(1400, 620),
            "glow", sprite: MockUtil.GlowSprite);
        _sectionRule = MockUtil.CreateBox(b, MockUtil.WithAlpha(Ember, 0.9f), new Vector2(0, -66), new Vector2(680, 3), "rule");

        _sectionBigText = MockUtil.CreateText(b, "", 62, TextAnchor.UpperCenter,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -86), new Vector2(0, -6),
            new Color(1f, 0.9f, 0.55f), display: true);
        _sectionSubText = MockUtil.CreateText(b, "", 24, TextAnchor.UpperCenter,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -152), new Vector2(0, -96), Cream);
        _restText = MockUtil.CreateText(b, "", 21, TextAnchor.UpperCenter,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -190), new Vector2(0, -150),
            new Color(0.8f, 0.76f, 0.72f));

        _sectionBanner.SetActive(false);
    }

    /// <summary>SectionDirector から呼ばれる。</summary>
    public void ShowSectionBanner(int clearedSection, int bonus)
    {
        if (_sectionBigText == null) return;
        _bannerTime = Time.unscaledTime;
        _sectionBigText.text = $"SECTION  {clearedSection}  CLEAR";
        _sectionSubText.text = $"区間ボーナス  +{bonus}";
        _sectionBanner.SetActive(true);
    }

    private void UpdateSectionUi()
    {
        if (Sections == null) return;

        // HUD の区間表示
        if (Sections.IsResting)
            _sectionText.text = $"SECTION {Sections.Section}   —  一息  —";
        else if (Sections.HasTarget)
            _sectionText.text = $"SECTION {Sections.Section}   次の大灯籠まで {Sections.DistanceToTarget:F0} m";
        else
            _sectionText.text = $"SECTION {Sections.Section}";

        if (_sectionBanner == null) return;

        float age = Time.unscaledTime - _bannerTime;
        // 一息タイムが終わるまでは幕を残す
        float life = Mathf.Max(2.6f, Sections.Tuning.restDuration + 0.7f);
        bool show = age >= 0f && age < life;
        if (_sectionBanner.activeSelf != show) _sectionBanner.SetActive(show);
        if (!show) return;

        float pop = Mathf.Exp(-age * 7f);
        float fade = Mathf.Clamp01(Mathf.Min(age / 0.15f, (life - age) / 0.5f));
        float sc = 1f + 0.3f * pop;
        _sectionBannerRt.localScale = new Vector3(sc, sc, 1f);
        _sectionBigText.color = new Color(1f, 0.9f, 0.55f, fade);
        _sectionSubText.color = MockUtil.WithAlpha(Cream, fade);
        _sectionRule.color = MockUtil.WithAlpha(Ember, 0.9f * fade);
        _restText.color = new Color(0.8f, 0.76f, 0.72f, fade);
        _restText.text = Sections.IsResting
            ? $"一息...  {Sections.RestRemain01 * 100f:F0}%"
            : "次の区間へ";
    }

    // ==================== Result ====================
    private void BuildResult(Transform root)
    {
        _resultPanel = MockUtil.CreateRect(root, "ResultPanel",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        MockUtil.CreateImage(_resultPanel.transform, new Color(0.02f, 0.015f, 0.04f, 0.88f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "Dim");

        _clearContent = BuildCard(_resultPanel.transform, true, out _clearScore);
        _goContent = BuildCard(_resultPanel.transform, false, out _goScore);
    }

    private GameObject BuildCard(Transform parent, bool clear, out ScorePanel score)
    {
        var card = MockUtil.CreateImage(parent,
            clear ? new Color(0.13f, 0.09f, 0.17f, 0.985f) : new Color(0.08f, 0.07f, 0.10f, 0.985f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420, -318), new Vector2(420, 318), "Card");
        var c = card.transform;

        Color accent = clear ? Ember : new Color(0.85f, 0.72f, 0.62f);

        MockUtil.CreateBox(c, MockUtil.WithAlpha(accent, 0.9f), new Vector2(0, 308), new Vector2(840, 5), "TopLine");
        MockUtil.CreateBox(c, MockUtil.WithAlpha(accent, 0.35f), new Vector2(0, -308), new Vector2(840, 3), "BottomLine");

        // 月・コウモリ・クモの巣
        MockUtil.CreateBox(c, new Color(1f, 0.85f, 0.5f, clear ? 0.26f : 0.12f), new Vector2(268, 168),
            new Vector2(420, 420), "MoonGlow", sprite: MockUtil.GlowSprite);
        MockUtil.CreateBox(c, clear ? new Color(1f, 0.94f, 0.72f) : new Color(0.6f, 0.6f, 0.55f),
            new Vector2(268, 168), new Vector2(140, 140), "Moon", circle: true);
        Bat(c, new Vector2(-262, 190), 44);
        Bat(c, new Vector2(-166, 136), 36);
        Bat(c, new Vector2(152, 220), 38);
        if (!clear) { Bat(c, new Vector2(50, 158), 42); Bat(c, new Vector2(-52, 224), 32); }
        Cobweb(c, new Vector2(-420, 318), 1f);
        if (!clear) Cobweb(c, new Vector2(420, 318), -1f);

        // 足元の灯篭の道
        for (int i = 0; i < 5; i++)
        {
            float x = -310 + i * 155f;
            float a = clear ? 1f : 0.18f;
            MockUtil.CreateBox(c, new Color(1f, 0.62f, 0.22f, 0.3f * a), new Vector2(x, -262), new Vector2(150, 150),
                "lampGlow", sprite: MockUtil.GlowSprite);
            MockUtil.CreateBox(c, new Color(0.22f, 0.17f, 0.24f), new Vector2(x, -262), new Vector2(32, 32), "lamp", circle: true);
            MockUtil.CreateBox(c, new Color(1f, 0.72f, 0.3f, a), new Vector2(x, -262), new Vector2(17, 17), "lampCore", circle: true);
        }

        // 見出し(オシャレに字間を空けた RESULT)
        MockUtil.CreateText(c, "R  E  S  U  L  T", 56, TextAnchor.UpperCenter,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -100), new Vector2(0, -28), accent,
            display: true);
        MockUtil.CreateBox(c, MockUtil.WithAlpha(accent, 0.5f), new Vector2(0, 196), new Vector2(300, 2), "rule");
        if (clear)
            MockUtil.CreateText(c, "灯篭の道を渡りきった", 22, TextAnchor.UpperCenter,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -150), new Vector2(0, -116),
                new Color(0.78f, 0.74f, 0.72f));

        if (clear)
        {
            Pumpkin(c, new Vector2(-296, -60), 150, true);
            MiniGhost(c, new Vector2(300, -60), 132, true);
        }
        else
        {
            var taunter = TauntGhost(c, new Vector2(300, -44), 176);
            UIWiggle.Attach(taunter, new Vector2(7f, 12f), 7f, 0.05f, 2.6f);

            var bubble = SpeechBubble(c, new Vector2(150, 118), new Vector2(392, 148), out _goTaunt);
            UIWiggle.Attach(bubble, new Vector2(4f, 6f), 2.5f, 0.03f, 1.9f);

            MiniGhost(c, new Vector2(-330, -190), 84, true);
        }

        // スコア内訳(ラベル左寄せ / 数値右寄せで桁をそろえる)
        float sx0 = clear ? -300f : -374f;
        float sx1 = clear ? 300f : 56f;
        score = new ScorePanel
        {
            Labels = MockUtil.CreateText(c, "", 23, TextAnchor.UpperLeft,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sx0, -112), new Vector2(sx1, 32), Cream),
            Values = MockUtil.CreateText(c, "", 23, TextAnchor.UpperRight,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sx0, -112), new Vector2(sx1, 32), Cream),
            Total = MockUtil.CreateText(c, "", 40, TextAnchor.UpperRight,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sx0, -178), new Vector2(sx1, -124), accent, display: true),
        };
        MockUtil.CreateBox(c, MockUtil.WithAlpha(accent, 0.35f),
            new Vector2((sx0 + sx1) * 0.5f, -120), new Vector2(sx1 - sx0, 2), "scoreRule");

        InputLabel.Bind(MockUtil.CreateText(c, "", 21, TextAnchor.LowerCenter,
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 20), new Vector2(0, 56),
            new Color(0.72f, 0.68f, 0.66f)), "{C} : タイトルへ      {S} : ランキング");

        return card.gameObject;
    }

    // ==================== パーツ ====================
    private static RectTransform TauntGhost(Transform parent, Vector2 c, float s)
    {
        var root = MockUtil.CreateRect(parent, "TauntGhost",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(c.x - s * 0.9f, c.y - s * 0.9f), new Vector2(c.x + s * 0.9f, c.y + s * 0.9f));
        var p = root.transform;
        Vector2 o = Vector2.zero;
        Color b = new Color(0.86f, 0.88f, 0.98f);

        MockUtil.CreateBox(p, new Color(0.6f, 0.75f, 1f, 0.26f), o, new Vector2(s * 2.1f, s * 2.1f),
            "glow", sprite: MockUtil.GlowSprite);
        MockUtil.CreateBox(p, b, o + new Vector2(-s * 0.56f, s * 0.2f), new Vector2(s * 0.3f, s * 0.3f), "armL", circle: true);
        MockUtil.CreateBox(p, b, o + new Vector2(s * 0.56f, s * 0.2f), new Vector2(s * 0.3f, s * 0.3f), "armR", circle: true);
        MockUtil.CreateBox(p, b, o + new Vector2(0, -s * 0.28f), new Vector2(s * 0.92f, s * 0.62f), "lower");
        MockUtil.CreateBox(p, b, o + new Vector2(-s * 0.3f, -s * 0.5f), new Vector2(s * 0.38f, s * 0.38f), "h0", circle: true);
        MockUtil.CreateBox(p, b, o + new Vector2(0f, -s * 0.55f), new Vector2(s * 0.38f, s * 0.38f), "h1", circle: true);
        MockUtil.CreateBox(p, b, o + new Vector2(s * 0.3f, -s * 0.5f), new Vector2(s * 0.38f, s * 0.38f), "h2", circle: true);
        MockUtil.CreateBox(p, b, o + new Vector2(0, s * 0.08f), new Vector2(s * 0.98f, s * 0.98f), "body", circle: true);

        Color e = new Color(0.12f, 0.11f, 0.17f);
        var eL = MockUtil.CreateBox(p, e, o + new Vector2(-s * 0.19f, s * 0.16f), new Vector2(s * 0.2f, s * 0.2f), "eL", circle: true);
        eL.rectTransform.localScale = new Vector3(1f, 0.28f, 1f);
        var eR = MockUtil.CreateBox(p, e, o + new Vector2(s * 0.19f, s * 0.16f), new Vector2(s * 0.19f, s * 0.19f), "eR", circle: true);
        eR.rectTransform.localScale = new Vector3(1f, 0.55f, 1f);

        MockUtil.CreateBox(p, e, o + new Vector2(0, -s * 0.06f), new Vector2(s * 0.46f, s * 0.24f), "mouth", circle: true);
        MockUtil.CreateBox(p, new Color(1f, 0.5f, 0.58f), o + new Vector2(s * 0.1f, -s * 0.16f),
            new Vector2(s * 0.2f, s * 0.16f), "tongue", circle: true);
        MockUtil.CreateBox(p, new Color(1f, 0.55f, 0.62f, 0.65f), o + new Vector2(-s * 0.36f, s * 0.02f), new Vector2(s * 0.14f, s * 0.14f), "blL", circle: true);
        MockUtil.CreateBox(p, new Color(1f, 0.55f, 0.62f, 0.65f), o + new Vector2(s * 0.36f, s * 0.02f), new Vector2(s * 0.14f, s * 0.14f), "blR", circle: true);
        return root;
    }

    private static RectTransform SpeechBubble(Transform parent, Vector2 c, Vector2 size, out Text label)
    {
        var root = MockUtil.CreateRect(parent, "Bubble",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(c.x - size.x * 0.5f, c.y - size.y * 0.5f),
            new Vector2(c.x + size.x * 0.5f, c.y + size.y * 0.5f));
        var p = root.transform;
        Color w = new Color(0.97f, 0.96f, 0.93f);

        MockUtil.CreateBox(p, w, Vector2.zero, size, "bubble", circle: true);
        MockUtil.CreateBox(p, w, new Vector2(size.x * 0.30f, -size.y * 0.46f), new Vector2(44, 44), "tail1", circle: true);
        MockUtil.CreateBox(p, w, new Vector2(size.x * 0.40f, -size.y * 0.64f), new Vector2(24, 24), "tail2", circle: true);

        label = MockUtil.CreateText(p, "", 25, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-size.x * 0.42f, -size.y * 0.3f), new Vector2(size.x * 0.42f, size.y * 0.3f),
            new Color(0.16f, 0.13f, 0.2f));
        return root;
    }

    private static void Bat(Transform p, Vector2 c, float s)
    {
        Color k = new Color(0.03f, 0.02f, 0.05f, 0.96f);
        MockUtil.CreateBox(p, k, c, new Vector2(s * 0.44f, s * 0.44f), "batBody", circle: true);
        var wl = MockUtil.CreateBox(p, k, c + new Vector2(-s * 0.42f, s * 0.06f), new Vector2(s * 0.72f, s * 0.34f), "wingL", circle: true);
        var wr = MockUtil.CreateBox(p, k, c + new Vector2(s * 0.42f, s * 0.06f), new Vector2(s * 0.72f, s * 0.34f), "wingR", circle: true);
        wl.rectTransform.localRotation = Quaternion.Euler(0, 0, 18);
        wr.rectTransform.localRotation = Quaternion.Euler(0, 0, -18);
    }

    private static void Pumpkin(Transform p, Vector2 c, float s, bool happy)
    {
        Color body = new Color(1f, 0.52f, 0.14f);
        Color glow = new Color(1f, 0.87f, 0.42f);
        Color stem = new Color(0.32f, 0.45f, 0.2f);

        MockUtil.CreateBox(p, new Color(1f, 0.55f, 0.15f, 0.3f), c, new Vector2(s * 2.2f, s * 2.2f),
            "pumpkinGlow", sprite: MockUtil.GlowSprite);
        MockUtil.CreateBox(p, stem, c + new Vector2(0, s * 0.5f), new Vector2(s * 0.15f, s * 0.26f), "stem");
        MockUtil.CreateBox(p, body, c + new Vector2(-s * 0.24f, 0), new Vector2(s * 0.72f, s * 0.92f), "lobeL", circle: true);
        MockUtil.CreateBox(p, body, c + new Vector2(s * 0.24f, 0), new Vector2(s * 0.72f, s * 0.92f), "lobeR", circle: true);
        MockUtil.CreateBox(p, body, c, new Vector2(s * 0.98f, s * 0.94f), "lobeC", circle: true);

        var eL = MockUtil.CreateBox(p, glow, c + new Vector2(-s * 0.2f, s * 0.12f), new Vector2(s * 0.2f, s * 0.2f), "eL");
        var eR = MockUtil.CreateBox(p, glow, c + new Vector2(s * 0.2f, s * 0.12f), new Vector2(s * 0.2f, s * 0.2f), "eR");
        eL.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
        eR.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);

        MockUtil.CreateBox(p, glow, c + new Vector2(0, -s * 0.17f), new Vector2(s * 0.62f, s * 0.16f), "mouth");
        MockUtil.CreateBox(p, body, c + new Vector2(-s * 0.13f, -s * 0.13f), new Vector2(s * 0.12f, s * 0.2f), "t1");
        MockUtil.CreateBox(p, body, c + new Vector2(s * 0.13f, -s * 0.13f), new Vector2(s * 0.12f, s * 0.2f), "t2");
    }

    private static void MiniGhost(Transform p, Vector2 c, float s, bool happy)
    {
        Color b = happy ? new Color(1f, 0.97f, 0.88f) : new Color(0.66f, 0.70f, 0.82f);
        MockUtil.CreateBox(p, MockUtil.WithAlpha(b, 0.24f), c, new Vector2(s * 2.1f, s * 2.1f),
            "ghostGlow", sprite: MockUtil.GlowSprite);
        MockUtil.CreateBox(p, b, c + new Vector2(0, -s * 0.28f), new Vector2(s * 0.92f, s * 0.62f), "lower");
        MockUtil.CreateBox(p, b, c + new Vector2(-s * 0.3f, -s * 0.5f), new Vector2(s * 0.38f, s * 0.38f), "h0", circle: true);
        MockUtil.CreateBox(p, b, c + new Vector2(0f, -s * 0.55f), new Vector2(s * 0.38f, s * 0.38f), "h1", circle: true);
        MockUtil.CreateBox(p, b, c + new Vector2(s * 0.3f, -s * 0.5f), new Vector2(s * 0.38f, s * 0.38f), "h2", circle: true);
        MockUtil.CreateBox(p, b, c + new Vector2(0, s * 0.08f), new Vector2(s * 0.98f, s * 0.98f), "body", circle: true);

        Color e = new Color(0.12f, 0.11f, 0.17f);
        var eL = MockUtil.CreateBox(p, e, c + new Vector2(-s * 0.18f, s * 0.14f), new Vector2(s * 0.16f, s * 0.16f), "eL", circle: true);
        var eR = MockUtil.CreateBox(p, e, c + new Vector2(s * 0.18f, s * 0.14f), new Vector2(s * 0.16f, s * 0.16f), "eR", circle: true);
        if (happy)
        {
            eL.rectTransform.localScale = new Vector3(1f, 0.5f, 1f);
            eR.rectTransform.localScale = new Vector3(1f, 0.5f, 1f);
        }
        MockUtil.CreateBox(p, new Color(1f, 0.55f, 0.62f, 0.7f), c + new Vector2(-s * 0.34f, 0f), new Vector2(s * 0.13f, s * 0.13f), "blL", circle: true);
        MockUtil.CreateBox(p, new Color(1f, 0.55f, 0.62f, 0.7f), c + new Vector2(s * 0.34f, 0f), new Vector2(s * 0.13f, s * 0.13f), "blR", circle: true);
    }

    private static void Cobweb(Transform p, Vector2 corner, float sign)
    {
        Color w = new Color(1f, 0.95f, 0.9f, 0.16f);
        for (int i = 0; i < 3; i++)
        {
            var line = MockUtil.CreateBox(p, w, corner + new Vector2(sign * (70f + i * 12f), -(70f + i * 12f)),
                new Vector2(170f, 3f), "web");
            line.rectTransform.localRotation = Quaternion.Euler(0, 0, sign * (18f + i * 26f));
        }
        for (int i = 1; i <= 2; i++)
        {
            var arc = MockUtil.CreateBox(p, w, corner + new Vector2(sign * 44f * i, -44f * i),
                new Vector2(78f, 3f), "webArc");
            arc.rectTransform.localRotation = Quaternion.Euler(0, 0, sign * 45f);
        }
    }

    // ==================== Update ====================
    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || !_built) return;

        if (gm.IsPlaying)
        {
            if (_resultPanel.activeSelf) _resultPanel.SetActive(false);

            _depthText.text = $"DEPTH  {gm.DepthMeters:F1} m";
            _killText.text = $"KILLS  {gm.GhostsDefeated}";
            UpdateGraph(gm);

            float ch = player != null ? player.Charge01 : 0f;
            bool charging = player != null && player.IsCharging;
            _chargeFill.anchorMax = new Vector2(Mathf.Clamp01(ch), 1f);
            _chargeGlow.color = new Color(1f, 0.6f, 0.2f, charging ? 0.10f + 0.28f * ch : 0f);
            _chargeText.text = charging
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
            Fade(_streakText, 0f);
            Fade(_streakBar, 0f);
            UpdateFog();
            UpdateWarmGlow();
            bool clear = gm.IsCleared;
            if (!_resultPanel.activeSelf)
            {
                _resultPanel.SetActive(true);
                _clearContent.SetActive(clear);
                _goContent.SetActive(!clear);
                _banner.SetActive(false);
                _warn.SetActive(false);
                _sectionBanner.SetActive(false);
                _perfectText.color = MockUtil.WithAlpha(_perfectText.color, 0f);

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

                if (!clear && _goTaunt != null)
                {
                    bool top3 = _submittedRank >= 0 && _submittedRank < 3;
                    _goTaunt.text = top3
                        ? $"{_submittedRank + 1}位!  " + Praises[Random.Range(0, Praises.Length)]
                        : Taunts[Random.Range(0, Taunts.Length)];
                    _goTaunt.color = top3
                        ? new Color(0.55f, 0.30f, 0.06f)     // 褒めるときは温かい色
                        : new Color(0.16f, 0.13f, 0.2f);
                }
            }
            FillScore(clear ? _clearScore : _goScore, gm);

            // Shift でランキングを開閉
            if (InputHub.SpecialPressed) { AudioManager.PlayUi(); _ranking.Toggle(_submittedRank); }
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
            if (_perfectText.color.a > 0f) _perfectText.color = MockUtil.WithAlpha(_perfectText.color, 0f);
            return;
        }

        float k = age / life;
        Color col = useOverride ? _popColor : new Color(1f, 0.88f, 0.42f);
        _perfectText.text = useOverride
            ? _popText
            : $"PERFECT !   x{player.LastPerfectCombo}   +{player.LastPerfectAdd}";
        _perfectText.color = new Color(col.r, col.g, col.b, Mathf.Clamp01(1f - k * k));
        float pop = 1f + 0.35f * Mathf.Exp(-age * 12f);
        _perfectRt.localScale = new Vector3(pop, pop, 1f);
        _perfectRt.anchoredPosition = new Vector2(0f, 150f + 40f * k);
    }

    private static void FillScore(ScorePanel s, GameManager gm)
    {
        if (s == null) return;
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

        for (int i = 0; i < _graphBars.Length; i++)
        {
            float wave = Mathf.Abs(Mathf.Sin(t * (2.4f + i * 0.21f) + i * 0.62f));
            float noise = Mathf.PerlinNoise(i * 0.35f, t * 1.7f);
            float h = 5f + (7f + 40f * act) * (0.30f + 0.70f * wave) * (0.55f + 0.65f * noise);
            _graphBars[i].sizeDelta = new Vector2(13f, Mathf.Clamp(h, 4f, 62f));
            _graphImgs[i].color = Color.Lerp(MockUtil.WithAlpha(Ember, 0.55f),
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

        _gaugeFill.fillAmount = hold;
        _gaugeFill.color = ready ? Color.Lerp(Plum, new Color(1f, 0.85f, 0.35f), 0.35f + 0.65f * pulse) : Plum;
        _gaugeTrack.color = new Color(1f, 1f, 1f, ready ? 0.22f : 0.12f);
        _gaugeCool.fillAmount = cool;
        _gaugeCool.color = new Color(1f, 0.68f, 0.28f, cool >= 1f ? 0.7f : 0.3f);
        _gaugeGlow.color = new Color(1f, 0.78f, 0.35f, ready ? 0.14f + 0.2f * pulse : 0.03f);

        int n = Mathf.Clamp(maxS, 1, _gaugePips.Length);
        for (int i = 0; i < _gaugePips.Length; i++)
        {
            bool use = i < n;
            if (_gaugePips[i].gameObject.activeSelf != use) _gaugePips[i].gameObject.SetActive(use);
            if (!use) continue;

            float ang = (90f - i * (360f / n)) * Mathf.Deg2Rad;
            _gaugePips[i].rectTransform.anchoredPosition = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 40f;
            _gaugePips[i].color = i < souls ? new Color(1f, 0.78f, 0.32f, 0.95f) : new Color(1f, 1f, 1f, 0.16f);
        }

        _gaugeLabel.text = ready
            ? $"HOLD {InputHub.SpecialLabel}  {hold * 100f:F0}%"
            : $"{InputHub.SpecialLabel}  {souls}/{n}";
        _gaugeLabel.color = ready ? new Color(1f, 0.85f, 0.4f) : Cream;
    }

    private void UpdateEventUi()
    {
        bool warning = Events != null && Events.IsWarning;
        bool active = Events != null && Events.HasActive;

        if (_warn.activeSelf != warning) _warn.SetActive(warning);
        if (_banner.activeSelf != active) _banner.SetActive(active);

        if (warning)
        {
            float w = Events.WarningProgress01;         // 0 -> 1
            Color tint = Events.PendingTint;
            float t = Time.time;

            _warnName.text = Events.PendingName;
            _warnName.color = tint;
            _warnDesc.text = Events.PendingDescription;
            _warnLead.color = Color.Lerp(Cream, tint, 0.5f + 0.5f * Mathf.Sin(t * 12f));

            // 出てくる勢い + 小刻みな震え + 明滅
            float pop = 1f + 0.22f * Mathf.Exp(-w * 9f) + 0.03f * Mathf.Sin(t * 16f);
            _warnBox.localScale = new Vector3(pop, pop, 1f);
            float shake = (0.35f + 0.65f * w) * 5f;
            _warnBox.anchoredPosition = _warnBoxBase + new Vector2(Random.Range(-shake, shake), Random.Range(-shake, shake));

            float band = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(w * 4f));
            _warnBandL.rectTransform.sizeDelta = new Vector2(1040f * band, 6f);
            _warnBandR.rectTransform.sizeDelta = new Vector2(1040f * band, 6f);
            _warnBandL.color = tint;
            _warnBandR.color = tint;

            float flash = 0.06f + 0.08f * Mathf.Abs(Mathf.Sin(t * 14f)) * (0.4f + 0.6f * w);
            _warnFlash.color = new Color(tint.r, tint.g, tint.b, flash);
        }

        if (active)
        {
            Color tint = Events.ActiveTint;
            _bannerName.text = Events.ActiveName;
            _bannerName.color = tint;
            _bannerDesc.text = Events.ActiveDescription;
            _bannerPlate.color = new Color(tint.r * 0.16f, tint.g * 0.16f, tint.b * 0.2f, 0.92f);
            _bannerBarFill.anchorMax = new Vector2(Mathf.Clamp01(Events.ActiveRemain01), 1f);
            var fillImg = _bannerBarFill.GetComponent<Image>();
            if (fillImg != null) fillImg.color = tint;
        }
    }
}
