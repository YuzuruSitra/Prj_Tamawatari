using UnityEngine;

/// <summary>
/// 小さな可愛いお化けがふわっと浮かんで消える演出(新規)。
/// ゲームオーバー・クリア・敵の統合などスタイリッシュに見せたい場面で Spawn する。
/// SpriteRenderer を複数組み合わせて簡易的なお化け形を作る。
/// </summary>
public class GhostPoof : MonoBehaviour
{
    [SerializeField] private float life = 1.1f;
    [SerializeField] private float riseSpeed = 2.2f;
    [SerializeField] private float wobble = 0.6f;

    private SpriteRenderer[] _parts;
    private float[] _baseAlpha;
    private float _t;
    private float _seed;

    public static GhostPoof Spawn(Vector3 pos, Color bodyColor, float scale = 1f, float life = 1.1f)
    {
        var root = new GameObject("GhostPoof");
        root.transform.position = new Vector3(pos.x, pos.y, 0f);
        root.transform.localScale = Vector3.one * scale;

        AddPart(root.transform, new Vector2(0f, 0f), 1.0f, bodyColor, 30);
        AddPart(root.transform, new Vector2(-0.28f, -0.42f), 0.44f, bodyColor, 30);
        AddPart(root.transform, new Vector2(0.02f, -0.44f), 0.44f, bodyColor, 30);
        AddPart(root.transform, new Vector2(0.32f, -0.42f), 0.44f, bodyColor, 30);
        AddPart(root.transform, new Vector2(-0.2f, 0.12f), 0.2f, new Color(0.15f, 0.15f, 0.2f, 1f), 31);
        AddPart(root.transform, new Vector2(0.2f, 0.12f), 0.2f, new Color(0.15f, 0.15f, 0.2f, 1f), 31);
        AddPart(root.transform, new Vector2(-0.34f, -0.06f), 0.16f, new Color(1f, 0.55f, 0.6f, 0.7f), 31);
        AddPart(root.transform, new Vector2(0.34f, -0.06f), 0.16f, new Color(1f, 0.55f, 0.6f, 0.7f), 31);

        var poof = root.AddComponent<GhostPoof>();
        poof.life = life;
        return poof;
    }

    private static void AddPart(Transform parent, Vector2 local, float dia, Color c, int order)
    {
        var go = MockUtil.MakeCircle("part", c, dia, order);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(local.x, local.y, 0f);
    }

    private void Awake()
    {
        _parts = GetComponentsInChildren<SpriteRenderer>();
        _baseAlpha = new float[_parts.Length];
        for (int i = 0; i < _parts.Length; i++) _baseAlpha[i] = _parts[i].color.a;
        _seed = Random.Range(0f, 10f);
    }

    private void Update()
    {
        _t += Time.deltaTime;
        float k = Mathf.Clamp01(_t / Mathf.Max(0.05f, life));

        transform.position += new Vector3(Mathf.Sin((_t + _seed) * 6f) * wobble * Time.deltaTime,
                                          riseSpeed * Time.deltaTime, 0f);
        transform.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(k * Mathf.PI));

        float fade = 1f - k;
        for (int i = 0; i < _parts.Length; i++)
        {
            if (_parts[i] == null) continue;
            Color c = _parts[i].color;
            c.a = _baseAlpha[i] * fade;
            _parts[i].color = c;
        }

        if (k >= 1f) Destroy(gameObject);
    }
}
