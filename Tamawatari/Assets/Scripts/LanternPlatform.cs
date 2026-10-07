using UnityEngine;

/// <summary>
/// 足場(灯篭)1つ分。<c>Assets/Prefabs/Lantern.prefab</c> のルートに付いていて、
/// 各パーツの SpriteRenderer をプレハブ上で参照している。
///
/// 色IDを持ち、3つの見た目状態を切り替える。
///   通常   : 灯がともっている(本道は明るく大きく、脇道は控えめだが確かに灯っている)
///   無効   : 灯が完全に落ちて黒い石になり、✕ が浮かぶ(乗れない)
///   黄金   : ボーナス中。全部が金色に輝き、必ず乗れる
/// 「無効」と「脇道(背景)」を混同しないよう、無効側は明かりを完全に消して ✕ を出す。
/// </summary>
public class LanternPlatform : MonoBehaviour
{
    // 描画順は「灯篭全体の順」からの相対で決まる。プレハブもこの並びで焼いてある。
    private const int OrderGlow = -2;
    private const int OrderStone = 0;
    private const int OrderCore = 1;
    private const int OrderWick = 2;
    private const int OrderRim = 3;
    private const int OrderHalo = 4;
    private const int OrderCross = 4;

    // 灯っているときと消えているときで、背後の光の広がりを変える
    private const float GlowScaleLit = 3.4f;
    private const float GlowScaleDim = 2.4f;

    [Header("プレハブ上のパーツ")]
    [SerializeField] private SpriteRenderer stone;      // 石の円盤 = 接地範囲そのもの
    [SerializeField] private SpriteRenderer glow;       // 背後のやわらかい光
    [SerializeField] private SpriteRenderer core;       // 灯そのもの
    [SerializeField] private SpriteRenderer wick;       // 芯の白い点
    [SerializeField] private SpriteRenderer rim;        // 接地判定の境界を示す縁のリング
    [SerializeField] private SpriteRenderer halo;       // 大灯籠だけの外側の輪
    [SerializeField] private GameObject crossA;         // 「乗れない」ときの ✕
    [SerializeField] private GameObject crossB;

    private Color _sStone, _sGlow, _sCore, _sWick, _sRim, _sHalo;   // 通常時の色

    private bool _usable = true;
    private bool _golden;
    private Color _disabled = new Color(0.36f, 0.36f, 0.40f);
    private Color _gold = new Color(1f, 0.80f, 0.30f);

    public int ColorId { get; private set; }
    public bool IsChain { get; private set; }
    /// <summary>区間の区切りになる大灯籠。常に乗れる。</summary>
    public bool IsGreat { get; set; }
    /// <summary>大灯籠としてすでに発動済みか。</summary>
    public bool Consumed { get; set; }
    public bool IsUsable => _usable;
    public bool IsGolden => _golden;

    /// <summary>見た目の半径(接地判定と一致する)。</summary>
    public float VisualRadius => transform.localScale.x * GameArt.CircleVisualRadius;

    /// <summary>
    /// 生成直後に1度だけ呼ぶ。色IDと、灯の色から派生する通常時の見た目を決める。
    /// </summary>
    /// <param name="lit">本道なら true。灯を明るく大きく見せる</param>
    /// <param name="great">大灯籠なら true。外側の輪をまとう</param>
    public void Setup(int colorId, bool lit, bool great, int order, Color stoneColor, Color coreColor, Color glowColor)
    {
        ColorId = colorId;
        IsChain = lit;
        IsGreat = great;

        _sStone = stoneColor;
        _sGlow = glowColor;
        _sCore = coreColor;
        _sWick = GameArt.WithAlpha(Color.white, lit ? 0.85f : 0.5f);
        _sRim = GameArt.WithAlpha(Color.Lerp(coreColor, Color.white, lit ? 0.35f : 0.25f), lit ? 0.95f : 0.7f);
        _sHalo = GameArt.WithAlpha(Color.Lerp(coreColor, Color.white, 0.6f), 0.9f);

        if (glow != null)
        {
            float g = lit ? GlowScaleLit : GlowScaleDim;
            glow.transform.localScale = new Vector3(g, g, 1f);
        }
        if (halo != null) halo.gameObject.SetActive(great);

        ApplyOrder(order);
        Restyle();
    }

    /// <summary>本道と脇道で描画順を分ける。パーツは全体の順からの相対で並ぶ。</summary>
    private void ApplyOrder(int order)
    {
        SetOrder(glow, order + OrderGlow);
        SetOrder(stone, order + OrderStone);
        SetOrder(core, order + OrderCore);
        SetOrder(wick, order + OrderWick);
        SetOrder(rim, order + OrderRim);
        SetOrder(halo, order + OrderHalo);
        SetOrder(crossA != null ? crossA.GetComponent<SpriteRenderer>() : null, order + OrderCross);
        SetOrder(crossB != null ? crossB.GetComponent<SpriteRenderer>() : null, order + OrderCross);
    }

    public void SetUsable(bool usable, Color disabledColor)
    {
        _disabled = disabledColor;
        if (_usable == usable) return;
        _usable = usable;
        Restyle();
    }

    public void SetGolden(bool golden, Color goldColor)
    {
        _gold = goldColor;
        if (_golden == golden) return;
        _golden = golden;
        if (golden) _usable = true;
        Restyle();
    }

    private void Restyle()
    {
        if (_golden)
        {
            Set(stone, new Color(_gold.r * 0.42f, _gold.g * 0.34f, _gold.b * 0.22f, _sStone.a));
            Set(glow, GameArt.WithAlpha(_gold, 0.8f));
            Set(core, _gold);
            Set(wick, new Color(1f, 0.99f, 0.9f, 1f));
            Set(rim, GameArt.WithAlpha(Color.Lerp(_gold, Color.white, 0.5f), 1f));
            Set(halo, GameArt.WithAlpha(Color.Lerp(_gold, Color.white, 0.7f), 1f));
            ShowCross(false);
        }
        else if (!_usable)
        {
            // 灯を完全に落とす:明かりを消して黒い石にし、✕ を出す
            Set(stone, new Color(_disabled.r * 0.28f, _disabled.g * 0.28f, _disabled.b * 0.32f, _sStone.a));
            Set(glow, new Color(_disabled.r, _disabled.g, _disabled.b, 0.03f));
            Set(core, new Color(_disabled.r * 0.34f, _disabled.g * 0.34f, _disabled.b * 0.38f, _sCore.a));
            Set(wick, new Color(_disabled.r * 0.5f, _disabled.g * 0.5f, _disabled.b * 0.55f, _sWick.a * 0.5f));
            Set(rim, new Color(_disabled.r * 0.75f, _disabled.g * 0.75f, _disabled.b * 0.8f, 0.5f));
            Set(halo, GameArt.WithAlpha(_disabled, 0.25f));
            ShowCross(true);
        }
        else
        {
            Set(stone, _sStone);
            Set(glow, _sGlow);
            Set(core, _sCore);
            Set(wick, _sWick);
            Set(rim, _sRim);
            Set(halo, _sHalo);
            ShowCross(false);
        }
    }

    private void ShowCross(bool on)
    {
        if (crossA != null && crossA.activeSelf != on) crossA.SetActive(on);
        if (crossB != null && crossB.activeSelf != on) crossB.SetActive(on);
    }

    private static void Set(SpriteRenderer sr, Color c)
    {
        if (sr != null) sr.color = c;
    }

    private static void SetOrder(SpriteRenderer sr, int order)
    {
        if (sr != null) sr.sortingOrder = order;
    }

#if UNITY_EDITOR
    /// <summary>焼き直しツールから、プレハブのパーツ参照を差し込むために使う。</summary>
    public void BindPartsForBake(SpriteRenderer stoneSr, SpriteRenderer glowSr, SpriteRenderer coreSr,
                                 SpriteRenderer wickSr, SpriteRenderer rimSr, SpriteRenderer haloSr,
                                 GameObject xA, GameObject xB)
    {
        stone = stoneSr; glow = glowSr; core = coreSr; wick = wickSr; rim = rimSr; halo = haloSr;
        crossA = xA; crossB = xB;
    }
#endif
}
