using UnityEngine;

/// <summary>
/// やわらかい光の粒。出したら流れながら縮んで消える(飛行中の霊気の尾など)。
/// </summary>
public class FadeMote : MonoBehaviour
{
    private SpriteRenderer _sr;
    private Vector3 _drift;
    private float _life, _t, _baseAlpha, _baseScale;

    public static FadeMote Spawn(Vector3 pos, Color color, float diameter, float life,
                                 Vector2 drift, int sortingOrder = 4)
    {
        var go = MockUtil.MakeGlow("Mote", color, diameter, sortingOrder);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var m = go.AddComponent<FadeMote>();
        m._life = Mathf.Max(0.05f, life);
        m._drift = new Vector3(drift.x, drift.y, 0f);
        m._baseAlpha = color.a;
        m._baseScale = diameter;
        return m;
    }

    private void Awake() => _sr = GetComponent<SpriteRenderer>();

    private void Update()
    {
        _t += Time.deltaTime;
        float k = Mathf.Clamp01(_t / _life);

        transform.position += _drift * Time.deltaTime;
        transform.localScale = Vector3.one * (_baseScale * Mathf.Lerp(1f, 0.25f, k));

        if (_sr != null)
        {
            Color c = _sr.color;
            c.a = _baseAlpha * (1f - k);
            _sr.color = c;
        }

        if (k >= 1f) Destroy(gameObject);
    }
}
