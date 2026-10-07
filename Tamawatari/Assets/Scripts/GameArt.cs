using UnityEngine;

/// <summary>
/// 絵まわりの共通の決めごとと、EventSystem を使わない当たり判定のためのユーティリティ。
/// 絵そのものは <see cref="GameAssets"/> が持っているアセットで、ここでは作らない。
/// </summary>
public static class GameArt
{
    /// <summary>円スプライトの可視半径(スケール1のとき)。接地判定と一致させる基準。</summary>
    public const float CircleVisualRadius = 0.5f;

    /// <summary>
    /// 霧スプライトの「穴」の大きさ。画像の半径に対する比で、中心のここまでが透明。
    /// 見せたい半径から必要な画像サイズを逆算するのに使う。
    /// </summary>
    public const float FogCoreT = 0.08f;

    public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

    /// <summary>
    /// ScreenSpaceOverlay の Canvas 上の RectTransform を画面座標の矩形に変換する。
    /// タッチやクリックの当たり判定を EventSystem 無しで行うために使う。
    /// </summary>
    public static Rect ScreenRect(RectTransform rt)
    {
        if (rt == null) return Rect.zero;
        var c = new Vector3[4];
        rt.GetWorldCorners(c);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(null, c[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(null, c[2]);
        return new Rect(min, max - min);
    }
}
