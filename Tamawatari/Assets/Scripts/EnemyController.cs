using System.Collections;
using UnityEngine;

/// <summary>
/// お化け(人魂)1体。完全2D。プレイヤーを直線最短ではなく左右にゆらゆら揺れて追う。
/// プレイヤーと接触した瞬間に即ゲームオーバー。
/// 衝撃波・統合の範囲内に入ると Vanish()(成仏)で縮小フェードして消える。
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class EnemyController : MonoBehaviour
{
    /// <summary>現在シーンに存在するお化けの数(HUD 表示用)。</summary>
    public static int ActiveCount { get; private set; }
    public static void ResetCount() => ActiveCount = 0;

    private float _speed = 2.5f;
    private float _weaveAmplitude = 1.2f;
    private float _weaveFrequency = 3.0f;
    private float _despawnDistance = 45f;
    private float _vanishTime = 0.25f;

    private Transform _target;
    private Vector3 _anchor;
    private float _phase;
    private bool _vanishing;

    public float Speed => _speed;
    public bool IsVanishing => _vanishing;

    public void Init(Transform target, float moveSpeed, float amplitude, float frequency,
                     float despawnDistance, float vanishTime)
    {
        _target = target;
        _speed = moveSpeed;
        _weaveAmplitude = amplitude;
        _weaveFrequency = frequency;
        _despawnDistance = despawnDistance;
        _vanishTime = vanishTime;
        _anchor = transform.position;
        _phase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Awake() => ActiveCount++;

    private void OnDestroy() => ActiveCount = Mathf.Max(0, ActiveCount - 1);

    private void Update()
    {
        if (_vanishing || _target == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        _anchor = Vector3.MoveTowards(_anchor, _target.position, _speed * Time.deltaTime);
        _anchor.z = 0f;
        _phase += _weaveFrequency * Time.deltaTime;

        Vector2 toTarget = (Vector2)_target.position - (Vector2)_anchor;
        Vector2 perp = toTarget.sqrMagnitude > 0.0001f
            ? Vector2.Perpendicular(toTarget.normalized)
            : Vector2.right;

        Vector3 p = _anchor + (Vector3)(perp * (Mathf.Sin(_phase) * _weaveAmplitude));
        p.z = 0f;
        transform.position = p;

        if (((Vector2)transform.position - (Vector2)_target.position).sqrMagnitude > _despawnDistance * _despawnDistance)
            Destroy(gameObject);
    }

    /// <summary>イベント用:成仏させずに、静かにフェードアウトして消える。</summary>
    public bool FadeOut(float time)
    {
        if (_vanishing) return false;
        _vanishing = true;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        StartCoroutine(FadeRoutine(Mathf.Max(0.05f, time)));
        return true;
    }

    /// <summary>画面上のお化けを全てフェードアウトさせる。消した数を返す。</summary>
    public static int FadeOutAll(float time)
    {
        var all = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        int n = 0;
        foreach (var e in all)
            if (e != null && e.FadeOut(time)) n++;
        return n;
    }

    private IEnumerator FadeRoutine(float time)
    {
        var srs = GetComponentsInChildren<SpriteRenderer>();
        var a0 = new float[srs.Length];
        for (int i = 0; i < srs.Length; i++) a0[i] = srs[i].color.a;

        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / time);
            // ふわっと上に散りながら薄くなる
            transform.position += Vector3.up * (0.35f * Time.deltaTime);
            for (int i = 0; i < srs.Length; i++)
            {
                if (srs[i] == null) continue;
                Color c = srs[i].color; c.a = a0[i] * (1f - k); srs[i].color = c;
            }
            yield return null;
        }
        Destroy(gameObject);
    }

    /// <summary>成仏。実際に消滅処理を始めたら true。</summary>
    public bool Vanish()
    {
        if (_vanishing) return false;
        _vanishing = true;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        StartCoroutine(VanishRoutine());
        return true;
    }

    private IEnumerator VanishRoutine()
    {
        var srs = GetComponentsInChildren<SpriteRenderer>();
        var a0 = new float[srs.Length];
        for (int i = 0; i < srs.Length; i++) a0[i] = srs[i].color.a;

        Vector3 s0 = transform.localScale;
        float t = 0f;
        while (t < _vanishTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, _vanishTime));
            transform.localScale = Vector3.Lerp(s0, s0 * 0.1f, k);
            for (int i = 0; i < srs.Length; i++)
            {
                if (srs[i] == null) continue;
                Color c = srs[i].color; c.a = a0[i] * (1f - k); srs[i].color = c;
            }
            yield return null;
        }
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_vanishing) return;
        var pc = other.GetComponent<PlayerController>();
        if (pc == null || pc.IsInvincible) return;      // 飛んでいる間は無敵
        CameraFollow.Instance?.ShakeHit();
        GameManager.Instance?.GameOver("hit by ghost");
    }
}
