using UnityEngine;

/// <summary>
/// 小さな可愛いお化けがふわっと浮かんで消える演出(<c>Assets/Prefabs/GhostPoof.prefab</c>)。
/// ゲームオーバー・クリア・敵の統合などスタイリッシュに見せたい場面で Spawn する。
/// 体の色はプレハブ上の <see cref="TintedParts"/> に流し込む。
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
        var go = GameAssets.Spawn(GameAssets.I != null ? GameAssets.I.ghostPoof : null);
        if (go == null) return null;

        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = Vector3.one * scale;

        var poof = go.GetComponent<GhostPoof>();
        if (poof != null) poof.Init(bodyColor, life);
        return poof;
    }

    private void Init(Color bodyColor, float lifeTime)
    {
        life = lifeTime;
        GetComponent<TintedParts>()?.SetTint(bodyColor);

        // 色を決めたあとで、消えるときの基準になる不透明度を控える
        _parts = GetComponentsInChildren<SpriteRenderer>();
        _baseAlpha = new float[_parts.Length];
        for (int i = 0; i < _parts.Length; i++) _baseAlpha[i] = _parts[i].color.a;
        _seed = Random.Range(0f, 10f);
    }

    private void Update()
    {
        if (_parts == null) return;
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
