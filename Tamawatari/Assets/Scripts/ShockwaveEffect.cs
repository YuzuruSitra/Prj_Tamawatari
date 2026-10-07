using UnityEngine;

/// <summary>
/// 衝撃波の見た目(<c>Assets/Prefabs/Shockwave.prefab</c>)。
/// 輪(リング)が広がりながら消え、内側にやわらかい閃光が残る。
/// 敵の巻き込み判定は PlayerController 側で OverlapCircle により行う。
/// </summary>
public class ShockwaveEffect : MonoBehaviour
{
    [SerializeField] private SpriteRenderer ring;
    [SerializeField] private SpriteRenderer flash;

    private float _finalDiameter = 1f;
    private float _duration = 0.35f;
    private float _t;

    public static ShockwaveEffect Spawn(Vector3 pos, float radius, float duration, Color color)
    {
        var go = GameAssets.Spawn(GameAssets.I != null ? GameAssets.I.shockwave : null);
        if (go == null) return null;

        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var fx = go.GetComponent<ShockwaveEffect>();
        if (fx != null) fx.Init(radius, duration, color);
        return fx;
    }

    private void Init(float radius, float duration, Color color)
    {
        _finalDiameter = Mathf.Max(0.1f, radius * 2f);
        _duration = Mathf.Max(0.05f, duration);
        _t = 0f;
        if (ring != null) ring.color = color;
        if (flash != null) flash.color = GameArt.WithAlpha(color, color.a * 0.55f);
    }

    private void Update()
    {
        _t += Time.deltaTime;
        float k = Mathf.Clamp01(_t / _duration);

        float d = Mathf.Lerp(0.15f, _finalDiameter, Mathf.Sqrt(k));   // 序盤に速く広がる
        if (ring != null)
        {
            ring.transform.localScale = Vector3.one * d;
            Color c = ring.color; c.a = Mathf.Lerp(0.85f, 0f, k); ring.color = c;
        }
        if (flash != null)
        {
            flash.transform.localScale = Vector3.one * (d * 0.85f);
            Color c = flash.color; c.a = Mathf.Lerp(0.5f, 0f, k * 1.6f); flash.color = c;
        }

        if (k >= 1f) Destroy(gameObject);
    }

#if UNITY_EDITOR
    /// <summary>焼き直しツールから、プレハブのパーツ参照を差し込むために使う。</summary>
    public void BindPartsForBake(SpriteRenderer ringSr, SpriteRenderer flashSr)
    {
        ring = ringSr;
        flash = flashSr;
    }
#endif
}
