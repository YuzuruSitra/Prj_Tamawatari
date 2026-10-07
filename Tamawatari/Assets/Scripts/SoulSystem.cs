using UnityEngine;

/// <summary>
/// 魂システム(新規)。プレイヤーに付く。
///  - 衝撃波で敵を倒すと魂が1つ増え、プレイヤーの周りを周回する(最大 maxSouls)。
///  - 魂が最大まで溜まった状態で Shift を長押しすると「統合」を発動:
///     周囲のお化けを全て消し、目の前(進行方向=上)に安全な足場の道を作る。
/// </summary>
public class SoulSystem : MonoBehaviour
{
    private PlayerTuning _t;
    private PlatformSpawner _spawner;
    private JumpIndicator _indicator;
    private PlayerController _player;

    private int _count;
    private float _orbitAngle;
    private float _holdTimer;
    private Transform[] _soulTf;

    public int Count => _count;
    public int MaxSouls => _t != null ? _t.maxSouls : 3;
    public bool IsFull => _count >= MaxSouls;
    public float MergeCharge01 => _t != null && _t.soulMergeHoldTime > 0f
        ? Mathf.Clamp01(_holdTimer / _t.soulMergeHoldTime) : 0f;

    public void Init(PlayerTuning tuning, PlatformSpawner spawner, JumpIndicator indicator, PlayerController player)
    {
        _t = tuning;
        _spawner = spawner;
        _indicator = indicator;
        _player = player;
        // 味方の魂 = 小さめの人魂だが「暖色 + 上に光る印」で敵と区別できる見た目
        // (Assets/Prefabs/Soul.prefab に焼いてある)
        _soulTf = new Transform[Mathf.Max(1, MaxSouls)];
        for (int i = 0; i < _soulTf.Length; i++)
        {
            var go = GameAssets.Spawn(GameAssets.I != null ? GameAssets.I.soul : null);
            if (go == null) continue;
            go.name = $"Soul_{i}";
            _soulTf[i] = go.transform;
            go.SetActive(false);
        }
    }

    public void AddSouls(int n)
    {
        if (_t == null || n <= 0) return;
        _count = Mathf.Clamp(_count + n, 0, _t.maxSouls);
    }

    private void Update()
    {
        if (_t == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            SyncSoulVisuals(0);
            return;
        }

        _orbitAngle += _t.soulOrbitSpeed * Time.deltaTime;
        SyncSoulVisuals(_count);

        // 統合(Shift長押し)。溜め中ではなく操作可能な状態のときだけ。
        bool shiftHeld = InputHub.SpecialHeld;
        bool eligible = IsFull && _player != null && _player.CanControl && !_player.IsCharging;

        if (eligible && shiftHeld)
        {
            _holdTimer += Time.deltaTime;
            if (_holdTimer >= _t.soulMergeHoldTime) DoMerge();
        }
        else
        {
            _holdTimer = 0f;
        }
    }

    private void SyncSoulVisuals(int active)
    {
        if (_soulTf == null) return;
        int n = Mathf.Max(1, MaxSouls);
        for (int i = 0; i < _soulTf.Length; i++)
        {
            if (_soulTf[i] == null) continue;
            bool on = i < active;
            if (_soulTf[i].gameObject.activeSelf != on) _soulTf[i].gameObject.SetActive(on);
            if (!on) continue;

            float a = (_orbitAngle + i * (360f / n)) * Mathf.Deg2Rad;
            Vector3 p = transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * _t.soulOrbitRadius;
            p.z = 0f;
            _soulTf[i].position = p;
        }
    }

    private void DoMerge()
    {
        _holdTimer = 0f;
        _count = 0;

        // 周囲のお化けを全て統合(成仏)
        var enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        int merged = 0;
        foreach (var e in enemies)
        {
            if (e == null || e.IsVanishing) continue;
            GhostPoof.Spawn(e.transform.position, new Color(0.7f, 0.95f, 1f, 0.95f), 0.7f, 0.7f);
            if (e.Vanish()) merged++;
        }
        GameManager.Instance?.AddGhostsDefeated(merged);

        // 目の前(=上方向)に安全な道を作る
        Vector3 dir = Vector3.up;
        if (_spawner != null)
            _spawner.CreateSafePath(transform.position, dir, _t.safePathCount, _t.safePathGap, _t.safePathDiameter);

        ShockwaveEffect.Spawn(transform.position, _t.safePathGap * _t.safePathCount * 0.5f,
                              0.5f, new Color(0.6f, 0.95f, 1f, 0.5f));
        CameraFollow.Instance?.ShakeMerge();
        AudioManager.PlayMerge();
        SyncSoulVisuals(0);
    }

    private void OnDestroy()
    {
        if (_soulTf == null) return;
        foreach (var t in _soulTf) if (t != null) Destroy(t.gameObject);
    }
}
