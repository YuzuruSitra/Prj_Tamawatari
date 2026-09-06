using System.Collections;
using UnityEngine;

/// <summary>
/// 区間(セクション)の進行役。ゲームに「緊張 → 到達 → 一息 → 再加速」のメリハリを作る。
///
///  - 道の途中に大灯篭が立つ。それが常に「次の目的地」になる
///  - 大灯篭に “乗った” 瞬間に区間クリア。スロー + 光 + 鐘、そして一息タイム
///  - 乗らずに通り過ぎてしまった大灯篭は失効し、目的地は自動的に次の大灯篭へ移る
///  - 一息が明けると難易度が一段上がる(敵の速度 ×speedStep / 出現間隔 ×intervalStep)
/// </summary>
public class SectionDirector : MonoBehaviour
{
    public static SectionDirector Instance { get; private set; }

    [SerializeField] private SectionTuning tuning = new SectionTuning();

    private PlatformSpawner _platforms;
    private EnemySpawner _enemies;
    private EventDirector _events;
    private UIManager _ui;
    private Transform _player;

    private float _restTimer;
    private LanternPlatform _target;

    public SectionTuning Tuning { get => tuning; set => tuning = value; }

    /// <summary>いま挑戦中の区間番号(1 始まり)。大灯篭に乗るたびに増える。</summary>
    public int Section { get; private set; } = 1;

    /// <summary>一息タイム中か。</summary>
    public bool IsResting => _restTimer > 0f;
    public float RestRemain01 => tuning.restDuration > 0f ? Mathf.Clamp01(_restTimer / tuning.restDuration) : 0f;

    /// <summary>目的地の大灯篭が見つかっているか。</summary>
    public bool HasTarget => _target != null;

    /// <summary>目的地までの距離(m)。</summary>
    public float DistanceToTarget =>
        (_target != null && _player != null)
            ? Vector2.Distance(_player.position, _target.transform.position)
            : 0f;

    public void Init(SectionTuning t, PlatformSpawner platforms, EnemySpawner enemies,
                     EventDirector events, UIManager ui, Transform player)
    {
        Instance = this;
        if (t != null) tuning = t;
        _platforms = platforms;
        _enemies = enemies;
        _events = events;
        _ui = ui;
        _player = player;
        Section = 1;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>プレイヤーが大灯篭に着地したときに PlayerController から呼ばれる。</summary>
    public void OnLandOnGreat(LanternPlatform great)
    {
        if (!tuning.enabled || great == null || great.Consumed) return;
        great.Consumed = true;
        Checkpoint();
    }

    private void Checkpoint()
    {
        int cleared = Section;
        Section++;

        // 報酬
        int bonus = tuning.sectionBonus + (cleared - 1) * tuning.sectionBonusStep;
        GameManager.Instance?.AddBonus(bonus);

        // 先にイベントを畳む。ここを後にすると OnEnd が湧きを戻してしまい一息が台無しになる
        _restTimer = Mathf.Max(0.2f, tuning.restDuration);
        _events?.Suspend(_restTimer + 1.5f);

        // 場を一掃して一息
        EnemyController.FadeOutAll(0.5f);
        if (_enemies != null)
        {
            _enemies.SpawnEnabled = false;
            _enemies.SpawnIntervalScale = 1f;     // イベント由来の倍率も戻しておく
        }

        // 演出
        Vector3 at = _player != null ? _player.position : Vector3.zero;
        ShockwaveEffect.Spawn(at, 7.5f, 0.85f, new Color(1f, 0.92f, 0.6f, 0.95f));
        for (int i = 0; i < 5; i++)
            GhostPoof.Spawn(at + (Vector3)(Random.insideUnitCircle * 1.4f),
                            new Color(1f, 0.94f, 0.7f, 0.95f), 0.9f, 1.1f);
        CameraFollow.Instance?.ShakeMerge();
        AudioManager.PlayCheckpoint();
        _ui?.ShowSectionBanner(cleared, bonus);

        StopAllCoroutines();
        StartCoroutine(SlowMo());
    }

    private IEnumerator SlowMo()
    {
        float hold = Mathf.Max(0.05f, tuning.slowMoTime);
        float from = Mathf.Clamp(tuning.slowMoScale, 0.05f, 1f);
        Time.timeScale = from;

        float t = 0f;
        while (t < hold)
        {
            t += Time.unscaledDeltaTime;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) break;
            Time.timeScale = Mathf.Lerp(from, 1f, Mathf.SmoothStep(0f, 1f, t / hold));
            yield return null;
        }
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (!tuning.enabled) return;

        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            if (_restTimer > 0f) EndRest();
            Time.timeScale = 1f;
            return;
        }

        RefreshTarget();

        if (_restTimer > 0f)
        {
            // スロー中でも一息の長さが変わらないよう非スケール時間で数える
            _restTimer -= Time.unscaledDeltaTime;
            if (_restTimer <= 0f) EndRest();
        }
    }

    /// <summary>
    /// 目的地の更新。乗らずに Y を通り過ぎた大灯篭は候補から外れるので、
    /// 自然と「次の大灯篭」が目的地になる。
    /// </summary>
    private void RefreshTarget()
    {
        if (_platforms == null || _player == null) return;
        _target = _platforms.NextGreatAbove(_player.position.y - 0.5f);
    }

    /// <summary>休憩終了。難易度を一段上げて再開する。</summary>
    private void EndRest()
    {
        _restTimer = 0f;
        if (_enemies == null) return;

        _enemies.SpawnEnabled = true;
        _enemies.SectionSpeedScale = Mathf.Min(tuning.maxSpeedScale,
            _enemies.SectionSpeedScale * Mathf.Max(1f, tuning.speedStep));
        _enemies.SectionIntervalScale = Mathf.Max(tuning.minIntervalScale,
            _enemies.SectionIntervalScale * Mathf.Clamp(tuning.intervalStep, 0.1f, 1f));
    }
}
