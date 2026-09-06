using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ランダムイベントの進行役。
///   待機 → 予告(warningTime 秒・UI で強調表示) → 発動(duration 秒) → 待機
/// 新しいイベントは Register(new XxxEvent()) を足すだけで抽選対象になる。
/// </summary>
public class EventDirector : MonoBehaviour
{
    private enum Phase { Idle, Warning, Active }

    [SerializeField] private EventTuning tuning = new EventTuning();

    private readonly List<GameEvent> _pool = new List<GameEvent>();
    private GameEventContext _ctx;
    private GameEvent _active, _pending;
    private Phase _phase = Phase.Idle;
    private float _timer;
    private float _duration;

    public EventTuning Tuning { get => tuning; set => tuning = value; }

    public bool HasActive => _phase == Phase.Active && _active != null;
    public string ActiveName => _active != null ? _active.DisplayName : "";
    public string ActiveDescription => _active != null ? _active.Description : "";
    public Color ActiveTint => _active != null ? _active.Tint : Color.white;
    public float ActiveRemain01 => (HasActive && _duration > 0f) ? Mathf.Clamp01(_timer / _duration) : 0f;

    public bool IsWarning => _phase == Phase.Warning && _pending != null;
    public string PendingName => _pending != null ? _pending.DisplayName : "";
    public string PendingDescription => _pending != null ? _pending.Description : "";
    public Color PendingTint => _pending != null ? _pending.Tint : Color.white;
    /// <summary>予告の進み具合(0 → 1 で発動)。</summary>
    public float WarningProgress01 =>
        (IsWarning && tuning.warningTime > 0f) ? Mathf.Clamp01(1f - _timer / tuning.warningTime) : 0f;

    public void Init(GameEventContext ctx, EventTuning t = null)
    {
        _ctx = ctx;
        if (t != null) tuning = t;
        _ctx.Tuning = tuning;

        Register(new MagneticStormEvent());
        Register(new GhostSwarmEvent());
        Register(new ColorRouletteEvent());
        Register(new DeepFogEvent());
        Register(new GoldenPathEvent());
        // ここに他のイベントを追加していく

        _timer = Mathf.Max(0.1f, tuning.firstDelay);
        _phase = Phase.Idle;
    }

    /// <summary>区間クリアの一息の間など、しばらくイベントを起こさない。</summary>
    public void Suspend(float seconds)
    {
        if (_phase == Phase.Active) StopActive();
        else { _pending = null; _phase = Phase.Idle; }
        _timer = Mathf.Max(_timer, seconds);
    }

    public void Register(GameEvent e)
    {
        if (e != null && !_pool.Contains(e)) _pool.Add(e);
    }

    private void Update()
    {
        if (_ctx == null || !tuning.enabled) return;

        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            if (_phase == Phase.Active) StopActive();
            else { _pending = null; _phase = Phase.Idle; }
            return;
        }

        float dt = Time.deltaTime;
        _timer -= dt;

        switch (_phase)
        {
            case Phase.Idle:
                if (_timer <= 0f) BeginWarning();
                break;

            case Phase.Warning:
                if (_timer <= 0f) StartPending();
                break;

            case Phase.Active:
                _active.OnTick(_ctx, dt);
                if (_timer <= 0f) StopActive();
                break;
        }
    }

    private void BeginWarning()
    {
        var candidates = new List<GameEvent>();
        for (int i = 0; i < _pool.Count; i++)
            if (_pool[i].CanStart(_ctx)) candidates.Add(_pool[i]);

        if (candidates.Count == 0)
        {
            _timer = Random.Range(tuning.intervalRange.x, tuning.intervalRange.y);
            return;
        }

        _pending = candidates[Random.Range(0, candidates.Count)];

        if (tuning.warningTime <= 0f) { StartPending(); return; }

        _phase = Phase.Warning;
        _timer = tuning.warningTime;
        AudioManager.PlayWarning();
        Debug.Log($"[EventDirector] WARNING {_pending.DisplayName}");
    }

    private void StartPending()
    {
        if (_pending == null) { _phase = Phase.Idle; _timer = 1f; return; }

        _active = _pending;
        _pending = null;
        _phase = Phase.Active;
        float fixedDur = _active.FixedDuration(_ctx);
        _duration = fixedDur > 0f ? fixedDur : Random.Range(tuning.durationRange.x, tuning.durationRange.y);
        _timer = _duration;
        _active.OnBegin(_ctx);
        Debug.Log($"[EventDirector] BEGIN {_active.DisplayName} ({_duration:F1}s)");
    }

    private void StopActive()
    {
        if (_active != null)
        {
            _active.OnEnd(_ctx);
            Debug.Log($"[EventDirector] END {_active.DisplayName}");
        }
        _active = null;
        _duration = 0f;
        _phase = Phase.Idle;
        _timer = Random.Range(tuning.intervalRange.x, tuning.intervalRange.y);
    }

    private void OnDestroy()
    {
        if (_active != null && _ctx != null) _active.OnEnd(_ctx);
    }
}
