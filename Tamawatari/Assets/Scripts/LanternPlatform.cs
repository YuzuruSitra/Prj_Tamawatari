using UnityEngine;

/// <summary>
/// 足場(灯篭)1つ分。色IDを持ち、3つの見た目状態を切り替える。
///   通常   : 灯がともっている(本道は明るく大きく、脇道は控えめだが確かに灯っている)
///   無効   : 灯が完全に落ちて黒い石になり、✕ が浮かぶ(乗れない)
///   黄金   : ボーナス中。全部が金色に輝き、必ず乗れる
/// 「無効」と「脇道(背景)」を混同しないよう、無効側は明かりを完全に消して ✕ を出す。
/// </summary>
public class LanternPlatform : MonoBehaviour
{
    private MockUtil.LanternParts _p;
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
    public float VisualRadius => transform.localScale.x * MockUtil.CircleVisualRadius;

    public void Setup(int colorId, bool isChain, MockUtil.LanternParts parts)
    {
        ColorId = colorId;
        IsChain = isChain;
        _p = parts;
        if (_p == null) return;
        _sStone = _p.Stone != null ? _p.Stone.color : Color.white;
        _sGlow = _p.Glow != null ? _p.Glow.color : Color.clear;
        _sCore = _p.Core != null ? _p.Core.color : Color.white;
        _sWick = _p.Wick != null ? _p.Wick.color : Color.white;
        _sRim = _p.Rim != null ? _p.Rim.color : Color.white;
        _sHalo = _p.Halo != null ? _p.Halo.color : Color.clear;
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
        if (_p == null) return;

        if (_golden)
        {
            Set(_p.Stone, new Color(_gold.r * 0.42f, _gold.g * 0.34f, _gold.b * 0.22f, _sStone.a));
            Set(_p.Glow, MockUtil.WithAlpha(_gold, 0.8f));
            Set(_p.Core, _gold);
            Set(_p.Wick, new Color(1f, 0.99f, 0.9f, 1f));
            Set(_p.Rim, MockUtil.WithAlpha(Color.Lerp(_gold, Color.white, 0.5f), 1f));
            Set(_p.Halo, MockUtil.WithAlpha(Color.Lerp(_gold, Color.white, 0.7f), 1f));
            ShowCross(false);
        }
        else if (!_usable)
        {
            // 灯を完全に落とす:明かりを消して黒い石にし、✕ を出す
            Set(_p.Stone, new Color(_disabled.r * 0.28f, _disabled.g * 0.28f, _disabled.b * 0.32f, _sStone.a));
            Set(_p.Glow, new Color(_disabled.r, _disabled.g, _disabled.b, 0.03f));
            Set(_p.Core, new Color(_disabled.r * 0.34f, _disabled.g * 0.34f, _disabled.b * 0.38f, _sCore.a));
            Set(_p.Wick, new Color(_disabled.r * 0.5f, _disabled.g * 0.5f, _disabled.b * 0.55f, _sWick.a * 0.5f));
            Set(_p.Rim, new Color(_disabled.r * 0.75f, _disabled.g * 0.75f, _disabled.b * 0.8f, 0.5f));
            Set(_p.Halo, MockUtil.WithAlpha(_disabled, 0.25f));
            ShowCross(true);
        }
        else
        {
            Set(_p.Stone, _sStone);
            Set(_p.Glow, _sGlow);
            Set(_p.Core, _sCore);
            Set(_p.Wick, _sWick);
            Set(_p.Rim, _sRim);
            Set(_p.Halo, _sHalo);
            ShowCross(false);
        }
    }

    private void ShowCross(bool on)
    {
        if (_p.CrossA != null && _p.CrossA.gameObject.activeSelf != on) _p.CrossA.gameObject.SetActive(on);
        if (_p.CrossB != null && _p.CrossB.gameObject.activeSelf != on) _p.CrossB.gameObject.SetActive(on);
    }

    private static void Set(SpriteRenderer sr, Color c)
    {
        if (sr != null) sr.color = c;
    }
}
