using UnityEngine;

/// <summary>
/// 背景の空気感。灯篭の火の粉のような光の粒が、画面内をゆっくり昇っていく。
/// 粒は <c>Assets/Prefabs/Mote.prefab</c> を最初に必要数だけ出して使い回す。
/// </summary>
public class AtmosphereFx : MonoBehaviour
{
    [SerializeField] private AtmosphereTuning tuning = new AtmosphereTuning();

    public AtmosphereTuning Tuning { get => tuning; set => tuning = value; }

    private Vector2 _wind, _windTarget;

    /// <summary>イベント演出用。粒を一定方向へ流す(磁気嵐など)。</summary>
    public void SetWind(Vector2 wind) => _windTarget = wind;

    private float _boost = 1f;
    private Color? _tintOverride;

    /// <summary>粒の勢いと色を一時的に変える(黄金の道など)。tint に null で元に戻す。</summary>
    public void SetBoost(float mul, Color? tint)
    {
        _boost = Mathf.Max(0.1f, mul);
        _tintOverride = tint;
        if (_srs == null) return;
        for (int i = 0; i < _srs.Length; i++)
        {
            if (_srs[i] == null) continue;
            Color c = tint ?? tuning.moteColor;
            _srs[i].color = new Color(c.r, c.g, c.b, _srs[i].color.a);
        }
    }

    /// <summary>背景の粒なので、灯篭より後ろに描く。</summary>
    private const int MoteSortingOrder = -20;

    private Camera _cam;
    private Transform[] _motes;
    private SpriteRenderer[] _srs;
    private float[] _rise, _phase, _wobbleAmp, _alpha;

    public void Init(Camera cam, AtmosphereTuning t)
    {
        _cam = cam;
        if (t != null) tuning = t;
        if (!tuning.enabled || tuning.moteCount <= 0) return;

        int n = tuning.moteCount;
        _motes = new Transform[n];
        _srs = new SpriteRenderer[n];
        _rise = new float[n];
        _phase = new float[n];
        _wobbleAmp = new float[n];
        _alpha = new float[n];

        for (int i = 0; i < n; i++)
        {
            float d = Random.Range(tuning.sizeRange.x, tuning.sizeRange.y);
            var go = GameAssets.Spawn(GameAssets.I != null ? GameAssets.I.mote : null, transform);
            if (go == null) continue;
            go.name = $"Mote_{i}";
            go.transform.localScale = Vector3.one * Mathf.Max(0.01f, d);
            _motes[i] = go.transform;
            _srs[i] = go.GetComponent<SpriteRenderer>();
            _srs[i].color = tuning.moteColor;
            _srs[i].sortingOrder = MoteSortingOrder;
            _rise[i] = Random.Range(tuning.riseSpeedRange.x, tuning.riseSpeedRange.y);
            _phase[i] = Random.Range(0f, Mathf.PI * 2f);
            _wobbleAmp[i] = Random.Range(0.3f, 1f) * tuning.wobble;
            _alpha[i] = tuning.moteColor.a * Random.Range(0.45f, 1f);
            Place(i, Random.Range(0f, 1f));
        }
    }

    private void Place(int i, float vertical01)
    {
        if (_cam == null || _motes[i] == null) return;
        float halfH = _cam.orthographicSize;
        float halfW = halfH * Mathf.Max(0.1f, _cam.aspect);
        Vector3 c = _cam.transform.position;
        float pad = tuning.padding;

        float x = c.x + Random.Range(-halfW - pad, halfW + pad);
        float y = Mathf.Lerp(c.y - halfH - pad, c.y + halfH + pad, vertical01);
        _motes[i].position = new Vector3(x, y, 0f);

        var col = _srs[i].color;
        col.a = _alpha[i];
        _srs[i].color = col;
    }

    private void Update()
    {
        if (_cam == null || _motes == null) return;

        float halfH = _cam.orthographicSize;
        float halfW = halfH * Mathf.Max(0.1f, _cam.aspect);
        Vector3 c = _cam.transform.position;
        float pad = tuning.padding;
        float dt = Time.deltaTime;
        _wind = Vector2.Lerp(_wind, _windTarget, 1f - Mathf.Exp(-2.5f * dt));

        for (int i = 0; i < _motes.Length; i++)
        {
            if (_motes[i] == null) continue;
            _phase[i] += dt * 1.4f;

            Vector3 p = _motes[i].position;
            p.y += (_rise[i] * _boost + _wind.y) * dt;
            p.x += (Mathf.Sin(_phase[i]) * _wobbleAmp[i] + _wind.x) * dt;
            p.z = 0f;
            _motes[i].position = p;

            // 画面外へ出たら反対側から出し直す(風で下へ流れる場合も拾う)
            if (p.y > c.y + halfH + pad) Place(i, 0f);
            else if (p.y < c.y - halfH - pad) Place(i, 1f);
            else if (p.x < c.x - halfW - pad) { p.x = c.x + halfW + pad; _motes[i].position = p; }
            else if (p.x > c.x + halfW + pad) { p.x = c.x - halfW - pad; _motes[i].position = p; }

            // 上下端でフェード
            float edge = Mathf.InverseLerp(halfH + pad, halfH * 0.6f, Mathf.Abs(_motes[i].position.y - c.y));
            Color baseCol = _tintOverride ?? tuning.moteColor;
            _srs[i].color = new Color(baseCol.r, baseCol.g, baseCol.b,
                                      _alpha[i] * Mathf.Clamp01(edge) * Mathf.Min(2f, _boost));
        }
    }
}
