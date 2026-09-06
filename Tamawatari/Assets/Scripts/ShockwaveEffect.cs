using UnityEngine;

/// <summary>
/// 衝撃波の見た目。輪(リング)が広がりながら消え、内側にやわらかい閃光が残る。
/// 敵の巻き込み判定は PlayerController 側で OverlapCircle により行う。
/// </summary>
public class ShockwaveEffect : MonoBehaviour
{
    private SpriteRenderer _ring, _flash;
    private float _finalDiameter;
    private float _duration;
    private float _t;

    public static ShockwaveEffect Spawn(Vector3 pos, float radius, float duration, Color color)
    {
        var go = new GameObject("Shockwave");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        var ringGo = new GameObject("ring");
        var ring = ringGo.AddComponent<SpriteRenderer>();
        ring.sprite = MockUtil.RingSprite;
        ring.color = color;
        ring.sortingOrder = 6;
        ringGo.transform.SetParent(go.transform, false);

        var flashGo = new GameObject("flash");
        var flash = flashGo.AddComponent<SpriteRenderer>();
        flash.sprite = MockUtil.GlowSprite;
        flash.color = MockUtil.WithAlpha(color, color.a * 0.55f);
        flash.sortingOrder = 5;
        flashGo.transform.SetParent(go.transform, false);

        var fx = go.AddComponent<ShockwaveEffect>();
        fx._ring = ring;
        fx._flash = flash;
        fx._finalDiameter = Mathf.Max(0.1f, radius * 2f);
        fx._duration = Mathf.Max(0.05f, duration);
        return fx;
    }

    private void Update()
    {
        _t += Time.deltaTime;
        float k = Mathf.Clamp01(_t / _duration);

        float d = Mathf.Lerp(0.15f, _finalDiameter, Mathf.Sqrt(k));   // 序盤に速く広がる
        if (_ring != null)
        {
            _ring.transform.localScale = Vector3.one * d;
            Color c = _ring.color; c.a = Mathf.Lerp(0.85f, 0f, k); _ring.color = c;
        }
        if (_flash != null)
        {
            _flash.transform.localScale = Vector3.one * (d * 0.85f);
            Color c = _flash.color; c.a = Mathf.Lerp(0.5f, 0f, k * 1.6f); _flash.color = c;
        }

        if (k >= 1f) Destroy(gameObject);
    }
}
