using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲーム全体の状態(Playing / GameOver / Clear)を一元管理する。
/// 各スクリプトは GameManager.Instance 経由で状態・スコアを参照する。
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, GameOver, Clear }

    public static GameManager Instance { get; private set; }

    [Header("Runtime state (read only)")]
    [SerializeField] private GameState state = GameState.Playing;
    [SerializeField] private float survivalTime;

    [SerializeField] private string titleSceneName = "Title";
    [SerializeField] private float returnInputDelay = 0.4f;
    [SerializeField] private float metersPerUnit = 1f;   // 深度表示のワールド→メートル換算

    private float endedAtTime;

    public GameState State => state;
    public float SurvivalTime => survivalTime;
    public bool IsPlaying => state == GameState.Playing;
    public bool IsCleared => state == GameState.Clear;

    /// <summary>通過した足場の数(PlatformSpawner から設定される)。</summary>
    public int PlatformsPassed { get; set; }

    /// <summary>到達した最大深度(メートル)。</summary>
    public float DepthMeters { get; private set; }

    /// <summary>倒した(成仏させた)お化けの累計。</summary>
    public int GhostsDefeated { get; private set; }

    /// <summary>スコア倍率(Bootstrap から差し込む)。</summary>
    public ScoreTuning Score { get; set; } = new ScoreTuning();

    public int DepthScore => Mathf.RoundToInt(DepthMeters * Score.depthMultiplier);
    public int KillScore => GhostsDefeated * Score.killMultiplier;

    /// <summary>ど真ん中着地(PERFECT)のコンボ。</summary>
    public int Combo { get; private set; }
    public int MaxCombo { get; private set; }
    public int BonusScore { get; private set; }

    public int TotalScore => DepthScore + KillScore + BonusScore;

    /// <summary>PERFECT 着地。加算した点を返す。</summary>
    public int AddPerfect(int baseScore, int comboStep)
    {
        Combo++;
        if (Combo > MaxCombo) MaxCombo = Combo;
        int add = baseScore + (Combo - 1) * comboStep;
        BonusScore += add;
        return add;
    }

    public void BreakCombo() => Combo = 0;

    /// <summary>イベント等からのボーナス加点。加算した点を返す。</summary>
    public int AddBonus(int add)
    {
        if (add <= 0) return 0;
        BonusScore += add;
        return add;
    }

    private void Awake()
    {
        Instance = this;
        state = GameState.Playing;
        survivalTime = 0f;
        PlatformsPassed = 0;
        DepthMeters = 0f;
        GhostsDefeated = 0;
        Combo = 0;
        MaxCombo = 0;
        BonusScore = 0;
        KillStreak = 0;
        MaxKillStreak = 0;
        KillStreakRemain01 = 0f;
        LastKillTime = -99f;
        EnemyController.ResetCount();
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (state == GameState.Playing)
        {
            survivalTime += Time.deltaTime;
            TickStreak(Time.deltaTime);
            return;
        }

        if (InputHub.ConfirmPressed && Time.unscaledTime - endedAtTime >= returnInputDelay)
        {
            AudioManager.PlayUi();
            SceneManager.LoadScene(titleSceneName);
        }
    }

    /// <summary>スタート地点からの前進距離(ワールド)を渡すと、最大深度(m)を更新する。</summary>
    public void ReportDepth(float unitsFromStart)
    {
        float m = Mathf.Max(0f, unitsFromStart) * metersPerUnit;
        if (m > DepthMeters) DepthMeters = m;
    }

    public void AddGhostsDefeated(int n)
    {
        if (n > 0) GhostsDefeated += n;
    }

    // ---------------- 連続キル ----------------
    /// <summary>いま continuing 中の連鎖数。</summary>
    public int KillStreak { get; private set; }
    public int MaxKillStreak { get; private set; }
    /// <summary>連鎖が途切れるまでの残り割合(1 → 0)。</summary>
    public float KillStreakRemain01 { get; private set; }
    /// <summary>直近に連鎖が伸びた時刻(UI のポップ用)。</summary>
    public float LastKillTime { get; private set; } = -99f;
    public int LastKillBonus { get; private set; }

    private float _streakTimer;

    /// <summary>
    /// 敵を n 体倒したことを登録する。連鎖を伸ばしてボーナスを加算し、
    /// 到達した連鎖数を返す(演出はこの値で強さを決める)。
    /// </summary>
    public int RegisterKills(int n)
    {
        if (n <= 0) return KillStreak;
        GhostsDefeated += n;

        int bonus = 0;
        for (int i = 0; i < n; i++)
        {
            KillStreak++;
            bonus += KillStreak * Score.killStreakBonus;
        }
        if (KillStreak > MaxKillStreak) MaxKillStreak = KillStreak;

        BonusScore += bonus;
        LastKillBonus = bonus;
        LastKillTime = Time.time;
        _streakTimer = Mathf.Max(0.2f, Score.killStreakWindow);
        KillStreakRemain01 = 1f;
        return KillStreak;
    }

    private void TickStreak(float dt)
    {
        if (KillStreak <= 0) { KillStreakRemain01 = 0f; return; }
        _streakTimer -= dt;
        KillStreakRemain01 = Mathf.Clamp01(_streakTimer / Mathf.Max(0.2f, Score.killStreakWindow));
        if (_streakTimer <= 0f) { KillStreak = 0; KillStreakRemain01 = 0f; }
    }

    public void GameOver(string reason = "")
    {
        if (state != GameState.Playing) return;
        state = GameState.GameOver;
        endedAtTime = Time.unscaledTime;
        KillStreak = 0;
        Time.timeScale = 1f;
        AudioManager.PlayGameOver();
        Debug.Log($"[GameManager] GAME OVER ({reason})  time={survivalTime:F1}s  depth={DepthMeters:F1}m");
    }

    public void Clear()
    {
        if (state != GameState.Playing) return;
        state = GameState.Clear;
        endedAtTime = Time.unscaledTime;
        KillStreak = 0;
        Time.timeScale = 1f;
        AudioManager.PlayClear();
        Debug.Log($"[GameManager] CLEAR!  time={survivalTime:F1}s  depth={DepthMeters:F1}m");
    }
}
