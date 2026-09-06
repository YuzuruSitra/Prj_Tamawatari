using UnityEngine;

/// <summary>
/// プレイヤーのチャージジャンプ操作(完全2D / XY平面, Z固定)。
///  1. スペースを押した瞬間の角度で方向をロック + インジケーター停止
///  2. 押している時間に応じてチャージ 0〜1(chargeSpeed で調整)
///  3. 溜め中に Shift を押すとキャンセル(何も起きずに元に戻る)
///  4. 離すと:
///       - チャージがほぼ0 → その場で衝撃波(移動しない)
///       - それ以外       → ロック方向へ距離に比例して移動 → 着地で衝撃波
///  5. ジャンプ中は入力を受け付けない。着地で再操作可能
///  6. 着地・被弾でカメラシェイク。衝撃波で倒した敵の数だけ魂を得る
///  7. 足場に乗れなかったらゲームオーバー
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    private enum State { Idle, Charging, Jumping, Falling }

    [SerializeField] private PlayerTuning tuning = new PlayerTuning();

    [Header("Refs")]
    [SerializeField] private JumpIndicator indicator;
    [SerializeField] private PlatformSpawner platformSpawner;

    private State _state = State.Idle;
    private float _charge01;
    private float _lastCharge;
    private float _lastGroundShockTime = -99f;
    private Vector3 _lockedDir = Vector3.up;
    private Vector3 _jumpFrom, _jumpTo;
    private float _jumpT;
    private float _missT;
    private bool _endHandled;
    private Rigidbody2D _rb;
    private SpriteRenderer[] _renderers;
    private float[] _baseAlpha;
    private float _startY;
    private bool _startYSet;
    private float _trailTimer;

    public PlayerTuning Tuning { get => tuning; set => tuning = value; }
    public float Charge01 => _charge01;
    public bool IsCharging => _state == State.Charging;
    public bool IsIdle => _state == State.Idle;

    /// <summary>飛んでいる間は無敵。</summary>
    public bool IsInvincible => tuning.invincibleWhileFlying && _state == State.Jumping;
    public bool IsGrounded => _state == State.Idle || _state == State.Charging;
    public bool CanControl => _state == State.Idle || _state == State.Charging;
    public bool IsJumping => _state == State.Jumping;
    public bool IsMissFalling => _state == State.Falling;
    public float JumpProgress01 => Mathf.Clamp01(_jumpT);
    public float MissProgress01 => Mathf.Clamp01(_missT);
    public float MaxJumpDistance => tuning.maxJumpDistance;
    public float MinJumpDistance => tuning.minJumpDistance;
    public Vector3 LockedDir => _lockedDir;

    /// <summary>Shift 衝撃波のクールダウン充填率(1 で使用可)。</summary>
    public float ShockReady01 =>
        Mathf.Clamp01((Time.time - _lastGroundShockTime) / Mathf.Max(0.0001f, tuning.groundShockCooldown));

    /// <summary>見た目(アニメーション対象)のルート。</summary>
    public Transform Visual { get; set; }

    /// <summary>ジャンプ開始回数 / 着地回数。イベント側が変化を見て処理する。</summary>
    public int JumpCount { get; private set; }
    public int LandCount { get; private set; }

    /// <summary>いま乗っている足場(色替えイベントで足元を守るのに使う)。</summary>
    public LanternPlatform CurrentPlatform { get; private set; }

    /// <summary>直近の PERFECT 着地(UI のポップ表示用)。</summary>
    public float LastPerfectTime { get; private set; } = -99f;
    public int LastPerfectAdd { get; private set; }
    public int LastPerfectCombo { get; private set; }

    public JumpIndicator Indicator { get => indicator; set => indicator = value; }
    public PlatformSpawner PlatformSpawner { get => platformSpawner; set => platformSpawner = value; }
    public SoulSystem Souls { get; set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.simulated = true;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        // 静止中も接触判定を切らさない(着地後の重なりを Stay で拾うため)
        _rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        _renderers = GetComponentsInChildren<SpriteRenderer>();
        _baseAlpha = new float[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++) _baseAlpha[i] = _renderers[i].color.a;
    }

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && !gm.IsPlaying) { HandleEnd(gm); return; }

        if (!_startYSet) { _startY = transform.position.y; _startYSet = true; }
        gm?.ReportDepth(transform.position.y - _startY);

        HandleInput();
        Tick(Time.deltaTime);
    }

    // ---------------- Input ----------------
    private void HandleInput()
    {
        bool shiftDown = InputHub.SpecialPressed;

        if (_state == State.Idle && InputHub.ConfirmPressed)
        {
            _lockedDir = indicator != null ? indicator.GetDirection() : Vector3.up;
            _lockedDir.z = 0f;
            if (_lockedDir.sqrMagnitude < 0.0001f) _lockedDir = Vector3.up;
            _lockedDir.Normalize();
            _charge01 = 0f;
            _state = State.Charging;
            if (indicator != null) indicator.SetFrozen(true);
        }
        else if (_state == State.Charging)
        {
            // Shift: 溜めをキャンセルしつつ、溜め段階の「半分のサイズ」の衝撃を放つ
            if (shiftDown) { ShiftBurst(); return; }

            _charge01 = Mathf.Clamp01(_charge01 + tuning.chargeSpeed * Time.deltaTime);
            if (InputHub.ConfirmReleased || !InputHub.ConfirmHeld)
                Release();
        }
        else if (_state == State.Idle && shiftDown)
        {
            // 待機中の Shift も同じ式(溜め0 の半分)で小さな衝撃になる
            ShiftBurst();
        }
    }

    /// <summary>Shift 発動:溜めをキャンセルし、その段階の衝撃波を shiftShockScale 倍で放つ。</summary>
    private void ShiftBurst()
    {
        float charge = _charge01;
        _charge01 = 0f;
        _state = State.Idle;
        if (indicator != null) indicator.SetFrozen(false);

        if (Time.time - _lastGroundShockTime < tuning.groundShockCooldown) return;
        _lastGroundShockTime = Time.time;

        float full = tuning.shockBaseRadius + charge * tuning.shockMaxRadiusBonus;
        EmitShockwaveRadius(transform.position, full * tuning.shiftShockScale);
        CameraFollow.Instance?.ShakeLand(charge * 0.5f);
    }

    private void Release()
    {
        _lastCharge = _charge01;

        // チャージ ≒ 0 → その場で衝撃波(移動しない)
        if (_lastCharge <= tuning.chargeZeroThreshold)
        {
            _state = State.Idle;
            if (indicator != null) indicator.SetFrozen(false);
            if (Time.time - _lastGroundShockTime >= tuning.groundShockCooldown)
            {
                _lastGroundShockTime = Time.time;
                EmitShockwave(transform.position, 0f);
                CameraFollow.Instance?.ShakeLand(0f);
            }
            return;
        }

        float distance = Mathf.Lerp(tuning.minJumpDistance, tuning.maxJumpDistance, _lastCharge);
        _jumpFrom = transform.position;
        _jumpTo = _jumpFrom + _lockedDir * distance;
        _jumpTo.z = 0f;
        _jumpT = 0f;
        _state = State.Jumping;
        JumpCount++;
        AudioManager.PlayJump(_lastCharge);
    }

    // ---------------- Movement ----------------
    private void Tick(float dt)
    {
        switch (_state)
        {
            case State.Idle:
            case State.Charging:
            {
                KeepZ();
                if (platformSpawner != null)
                {
                    // 足元の足場を常に把握しておく(色替えイベントが参照する)
                    if (platformSpawner.TryGetLanding(transform.position, tuning.landingProbe, out var here))
                    {
                        CurrentPlatform = here;
                    }
                    else if (tuning.fallWhenOffPlatform)
                    {
                        // 待機中に足場から外れていたら落下(磁気嵐で流されたとき等)
                        _missT = 0f;
                        _state = State.Falling;
                        AudioManager.PlayMiss();
                        if (indicator != null) indicator.SetFrozen(false);
                    }
                }
                break;
            }

            case State.Jumping:
            {
                _jumpT += dt / Mathf.Max(0.0001f, tuning.jumpDuration);
                float t = Mathf.Clamp01(_jumpT);
                float ease = t * t * (3f - 2f * t);
                Vector3 p = Vector3.Lerp(_jumpFrom, _jumpTo, ease);
                p.z = 0f;
                MoveTo(p);
                EmitTrail(dt);

                if (t >= 1f) ResolveLanding();
                break;
            }

            case State.Falling:
            {
                _missT += dt / Mathf.Max(0.0001f, tuning.missFallTime);
                float k = Mathf.Clamp01(_missT);
                SetAlpha(1f - k);
                if (k >= 1f) GameManager.Instance?.GameOver("missed platform");
                break;
            }
        }
    }

    /// <summary>飛行中に霊気の尾を残す(無敵中であることの手掛かりにもなる)。</summary>
    private void EmitTrail(float dt)
    {
        if (tuning.trailInterval <= 0f) return;
        _trailTimer -= dt;
        if (_trailTimer > 0f) return;
        _trailTimer = tuning.trailInterval;

        Vector2 jitter = Random.insideUnitCircle * 0.12f;
        FadeMote.Spawn(transform.position + (Vector3)jitter,
                       new Color(1f, 0.72f, 0.35f, 0.5f),
                       tuning.playerDiameter * 1.6f, 0.4f,
                       new Vector2(0f, 0.4f), sortingOrder: 4);
    }

    private void SetAlpha(float mul)
    {
        if (_renderers == null) return;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            Color c = _renderers[i].color;
            c.a = _baseAlpha[i] * Mathf.Clamp01(mul);
            _renderers[i].color = c;
        }
    }

    /// <summary>イベント(磁気嵐など)から外部的に位置をずらす。物理側も一緒に同期する。</summary>
    public void ExternalMove(Vector3 delta)
    {
        Vector3 p = transform.position + delta;
        p.z = 0f;
        MoveTo(p);
    }

    private void MoveTo(Vector3 p)
    {
        transform.position = p;
        _rb.position = p;
    }

    private void KeepZ()
    {
        if (Mathf.Abs(transform.position.z) > 0.0001f)
        {
            Vector3 p = transform.position; p.z = 0f;
            MoveTo(p);
        }
    }

    private void ResolveLanding()
    {
        Vector2 pos = transform.position;

        if (platformSpawner != null && platformSpawner.TryGetLanding(pos, tuning.landingProbe, out var landed))
        {
            _state = State.Idle;
            CurrentPlatform = landed;
            LandCount++;
            if (indicator != null) indicator.SetFrozen(false);

            // ど真ん中着地(PERFECT)判定
            float dist = Vector2.Distance(pos, (Vector2)landed.transform.position);
            bool perfect = dist <= landed.VisualRadius * tuning.perfectThreshold;
            var gm = GameManager.Instance;

            float radius = tuning.shockBaseRadius + _lastCharge * tuning.shockMaxRadiusBonus;
            if (perfect)
            {
                radius *= tuning.perfectShockScale;
                LastPerfectAdd = gm != null ? gm.AddPerfect(tuning.perfectScore, tuning.comboBonusPerStep) : 0;
                LastPerfectCombo = gm != null ? gm.Combo : 0;
                LastPerfectTime = Time.time;
                AudioManager.PlayPerfect(LastPerfectCombo);
                ShockwaveEffect.Spawn(pos, radius * 0.55f, tuning.shockwaveDuration * 1.4f,
                                      new Color(1f, 0.9f, 0.45f, 0.95f));
                CameraFollow.Instance?.ShakeLand(Mathf.Min(1f, _lastCharge + 0.35f));
            }
            else
            {
                gm?.BreakCombo();
                AudioManager.PlayLand();
                CameraFollow.Instance?.ShakeLand(_lastCharge);
            }

            EmitShockwaveRadius(pos, radius);
            platformSpawner.NotifyLanded(transform.position);

            // 大灯籠に乗ったときだけ区間クリア
            if (landed != null && landed.IsGreat) SectionDirector.Instance?.OnLandOnGreat(landed);
        }
        else
        {
            _missT = 0f;
            _state = State.Falling;
            AudioManager.PlayMiss();
        }
    }

    /// <summary>チャージ量に比例した半径の衝撃波。</summary>
    private void EmitShockwave(Vector2 pos, float charge)
        => EmitShockwaveRadius(pos, tuning.shockBaseRadius + charge * tuning.shockMaxRadiusBonus);

    /// <summary>指定半径の衝撃波。範囲内の敵を成仏させ、その数だけ魂を得る。</summary>
    private void EmitShockwaveRadius(Vector2 pos, float radius)
    {
        radius = Mathf.Max(0.2f, radius);
        ShockwaveEffect.Spawn(pos, radius, tuning.shockwaveDuration, new Color(1f, 0.78f, 0.32f, 0.9f));
        AudioManager.PlayShock(Mathf.InverseLerp(tuning.shockBaseRadius,
                               tuning.shockBaseRadius + tuning.shockMaxRadiusBonus, radius));

        int killed = 0;
        var hits = Physics2D.OverlapCircleAll(pos, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            var e = hits[i].GetComponent<EnemyController>();
            if (e != null && e.Vanish())
            {
                GhostPoof.Spawn(e.transform.position, new Color(0.85f, 0.9f, 1f, 0.95f), 0.6f, 0.7f);
                killed++;
            }
        }
        if (killed > 0)
        {
            Souls?.AddSouls(killed);
            int streak = GameManager.Instance != null ? GameManager.Instance.RegisterKills(killed) : 0;
            AudioManager.PlayKill(streak);
            // 連鎖が伸びるほど手応えを強くする
            CameraFollow.Instance?.ShakeLand(Mathf.Clamp01(0.25f + 0.08f * streak));
        }
    }

    // ---------------- End-of-game flourish ----------------
    private void HandleEnd(GameManager gm)
    {
        if (_endHandled) return;
        _endHandled = true;

        bool clear = gm.IsCleared;
        Color c = clear ? new Color(1f, 0.92f, 0.55f, 0.97f) : new Color(0.8f, 0.86f, 1f, 0.97f);
        int n = clear ? 7 : 2;
        for (int i = 0; i < n; i++)
        {
            Vector3 o = (Vector3)(Random.insideUnitCircle * 0.6f);
            GhostPoof.Spawn(transform.position + o, c, clear ? 1.1f : 0.9f, clear ? 1.4f : 1.0f);
        }
        if (clear) CameraFollow.Instance?.ShakeMerge();
        else CameraFollow.Instance?.ShakeHit();
    }

    private void OnTriggerEnter2D(Collider2D other) => TryGhostHit(other);

    // 無敵中にすり抜けた敵と着地後も重なったままのケースを拾うため Stay も見る
    private void OnTriggerStay2D(Collider2D other) => TryGhostHit(other);

    private void TryGhostHit(Collider2D other)
    {
        if (IsInvincible) return;                       // 飛んでいる間は無敵
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        var e = other.GetComponent<EnemyController>();
        if (e == null || e.IsVanishing) return;

        CameraFollow.Instance?.ShakeHit();
        GameManager.Instance?.GameOver("hit by ghost");
    }
}
