using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// モック用ユーティリティ(完全2D / ハロウィン「灯篭の道」トーン)。
///  - 手続き生成のスプライト(円 / リング / やわらかい光 / ビネット)
///  - それらを使ったワールドオブジェクト(灯篭・お化けの親玉・人魂)の組み立て
///  - ランタイム uGUI(Canvas / Text / Image)の生成
/// </summary>
public static class MockUtil
{
    /// <summary>生成した円スプライトの可視半径(スケール1のとき)。接地判定と一致させる基準。</summary>
    public const float CircleVisualRadius = 0.5f;

    // ==================== Procedural sprites ====================
    private static Sprite _circle, _ring, _glow, _vignette, _arrow, _fog;

    /// <summary>霧用。ごく中心だけ透明で、すぐ不透明になる(穴あきの覆い)。</summary>
    public const float FogCoreT = 0.08f;

    private static Sprite BuildRadial(string spriteName, System.Func<float, float, float> alphaFn, int S = 128)
    {
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var px = new Color32[S * S];
        Vector2 c = new Vector2(S * 0.5f, S * 0.5f);
        float R = S * 0.5f;
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                float a = Mathf.Clamp01(alphaFn(d / R, R));   // t = 0(中心)〜1(半径)
                px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
        sp.name = spriteName;
        return sp;
    }

    /// <summary>直径 1 ワールドユニットの白い円(可視半径ちょうど 0.5)。</summary>
    public static Sprite CircleSprite =>
        _circle != null ? _circle : (_circle = BuildRadial("Circle", (t, R) => (1f - t) * R));

    /// <summary>輪(衝撃波用)。</summary>
    public static Sprite RingSprite =>
        _ring != null ? _ring : (_ring = BuildRadial("Ring",
            (t, R) => Mathf.Min((1f - t) * R, (t - 0.70f) * R)));

    /// <summary>中心が明るく外へ滑らかに消える光(灯篭の明かり・霊気・粒)。</summary>
    public static Sprite GlowSprite =>
        _glow != null ? _glow : (_glow = BuildRadial("Glow",
            (t, R) => Mathf.Pow(Mathf.Clamp01(1f - t), 2.2f)));

    /// <summary>画面四隅を落とすビネット(中心が透明、外周が不透明)。</summary>
    public static Sprite VignetteSprite =>
        _vignette != null ? _vignette : (_vignette = BuildRadial("Vignette",
            (t, R) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.52f, 1.05f, t)), 256));

    /// <summary>濃霧の覆い。中心に小さな穴が開いていて、外は不透明。</summary>
    public static Sprite FogSprite =>
        _fog != null ? _fog : (_fog = BuildRadial("Fog",
            (t, R) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(FogCoreT, 0.24f, t)), 256));

    /// <summary>上(+Y)を向いた矢印。方向カーソル用。</summary>
    public static Sprite ArrowSprite => _arrow != null ? _arrow : (_arrow = BuildArrow());

    private static Sprite BuildArrow(int S = 128)
    {
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var px = new Color32[S * S];
        const float headTop = 0.48f, headBase = 0.04f, headHalf = 0.36f;
        const float shaftHalf = 0.13f, shaftBottom = -0.46f;
        float aa = S * 0.5f;

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S - 0.5f;
                float v = (y + 0.5f) / S - 0.5f;
                float a = 0f;

                if (v >= headBase && v <= headTop)                      // 三角の頭
                {
                    float halfW = headHalf * (headTop - v) / (headTop - headBase);
                    a = Mathf.Max(a, (halfW - Mathf.Abs(u)) * aa);
                }
                if (v >= shaftBottom && v <= headBase + 0.02f)          // 軸
                {
                    a = Mathf.Max(a, Mathf.Min((shaftHalf - Mathf.Abs(u)) * aa,
                                               (v - shaftBottom) * aa));
                }
                px[y * S + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
        sp.name = "Arrow";
        return sp;
    }

    public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

    /// <summary>矢印の GameObject(回転0で +Y を向く)。</summary>
    public static GameObject MakeArrow(string name, Color color, float size, int sortingOrder = 0)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ArrowSprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        go.transform.localScale = Vector3.one * Mathf.Max(0.01f, size);
        return go;
    }

    // ==================== World objects ====================
    public static GameObject MakeCircle(string name, Color color, float diameter, int sortingOrder = 0)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CircleSprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        go.transform.localScale = Vector3.one * Mathf.Max(0.01f, diameter);
        return go;
    }

    /// <summary>やわらかい光の GameObject(単体)。</summary>
    public static GameObject MakeGlow(string name, Color color, float diameter, int sortingOrder = 0)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GlowSprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        go.transform.localScale = Vector3.one * Mathf.Max(0.01f, diameter);
        return go;
    }

    private static Transform AddChild(Transform parent, Sprite sprite, Vector2 localPos, float localDia,
        Color c, int order, float zRotDeg = 0f, float aspectY = 1f)
    {
        var go = new GameObject("part");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = c;
        sr.sortingOrder = order;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
        go.transform.localScale = new Vector3(localDia, localDia * aspectY, 1f);
        if (Mathf.Abs(zRotDeg) > 0.01f) go.transform.localRotation = Quaternion.Euler(0f, 0f, zRotDeg);
        return go.transform;
    }

    private static Transform AddChildCircle(Transform p, Vector2 pos, float dia, Color c, int order,
        float zRot = 0f, float aspectY = 1f) => AddChild(p, CircleSprite, pos, dia, c, order, zRot, aspectY);

    private static Transform AddChildGlow(Transform p, float dia, Color c, int order)
        => AddChild(p, GlowSprite, Vector2.zero, dia, c, order);

    /// <summary>灯篭の各パーツ。状態(通常/無効/黄金)ごとに個別に塗り替えるために保持する。</summary>
    public class LanternParts
    {
        public GameObject Root;
        public SpriteRenderer Stone, Glow, Core, Wick, Rim;
        public Transform CrossA, CrossB;      // 無効表示の ✕(通常は非表示)
        public SpriteRenderer Halo;           // 大灯籠だけの外側の輪
    }

    public static LanternParts MakeLantern(string name, float diameter, int order, bool lit,
                                           Color stone, Color core, Color glow, bool great = false)
    {
        var go = MakeCircle(name, stone, diameter, order);          // 石の円盤 = 接地範囲そのもの
        var t = go.transform;

        var p = new LanternParts { Root = go, Stone = go.GetComponent<SpriteRenderer>() };
        p.Glow = AddChildGlow(t, lit ? 3.4f : 2.4f, glow, order - 2).GetComponent<SpriteRenderer>();
        p.Core = AddChildCircle(t, Vector2.zero, 0.78f, core, order + 1).GetComponent<SpriteRenderer>();
        p.Wick = AddChildCircle(t, new Vector2(0f, 0.04f), 0.30f,
                                WithAlpha(Color.white, lit ? 0.85f : 0.5f), order + 2).GetComponent<SpriteRenderer>();

        // 縁のリング:接地判定の境界を見た目でそのまま示す
        Color rim = WithAlpha(Color.Lerp(core, Color.white, lit ? 0.35f : 0.25f), lit ? 0.95f : 0.7f);
        p.Rim = AddChild(t, RingSprite, Vector2.zero, 1f, rim, order + 3).GetComponent<SpriteRenderer>();

        // 大灯籠は外側にもう一重の輪をまとう
        if (great)
            p.Halo = AddChild(t, RingSprite, Vector2.zero, 1.28f,
                              WithAlpha(Color.Lerp(core, Color.white, 0.6f), 0.9f), order + 4).GetComponent<SpriteRenderer>();

        // 「乗れない」ときだけ出す ✕(最初は隠しておく)
        Color x = new Color(0.95f, 0.35f, 0.35f, 0.9f);
        p.CrossA = AddChildCircle(t, Vector2.zero, 0.62f, x, order + 4, 45f, 0.16f);
        p.CrossB = AddChildCircle(t, Vector2.zero, 0.62f, x, order + 4, -45f, 0.16f);
        p.CrossA.gameObject.SetActive(false);
        p.CrossB.gameObject.SetActive(false);
        return p;
    }

    /// <summary>お化けの親玉:本体 + 波打つ裾 + 王冠 + つり目。背後に霊気の光。</summary>
    public static GameObject MakeBossGhost(string name, Color body, float diameter, int order)
    {
        var go = MakeCircle(name, body, diameter, order);
        var t = go.transform;
        AddChildGlow(t, 2.6f, WithAlpha(body, 0.5f), order - 2);

        Color trim = WithAlpha(body, 1f);
        AddChildCircle(t, new Vector2(-0.33f, -0.34f), 0.46f, trim, order);
        AddChildCircle(t, new Vector2(-0.10f, -0.40f), 0.46f, trim, order);
        AddChildCircle(t, new Vector2(0.14f, -0.40f), 0.46f, trim, order);
        AddChildCircle(t, new Vector2(0.36f, -0.32f), 0.42f, trim, order);

        Color gold = new Color(1f, 0.82f, 0.25f);
        AddChildCircle(t, new Vector2(-0.24f, 0.44f), 0.22f, gold, order + 1, 45f);
        AddChildCircle(t, new Vector2(0.00f, 0.52f), 0.26f, gold, order + 1, 45f);
        AddChildCircle(t, new Vector2(0.24f, 0.44f), 0.22f, gold, order + 1, 45f);

        Color eye = new Color(0.08f, 0.06f, 0.12f);
        AddChildCircle(t, new Vector2(-0.19f, 0.08f), 0.30f, eye, order + 1, 20f, 0.55f);
        AddChildCircle(t, new Vector2(0.19f, 0.08f), 0.30f, eye, order + 1, -20f, 0.55f);
        AddChildCircle(t, new Vector2(-0.19f, 0.10f), 0.09f, new Color(1f, 0.62f, 0.2f), order + 2);
        AddChildCircle(t, new Vector2(0.19f, 0.10f), 0.09f, new Color(1f, 0.62f, 0.2f), order + 2);
        AddChildCircle(t, new Vector2(0f, -0.16f), 0.34f, eye, order + 1, 0f, 0.4f);
        return go;
    }

    /// <summary>人魂:本体 + 尾 + コア + 霊気の光。friendly=味方は暖色 + 上に光る印。</summary>
    public static GameObject MakeHitodama(string name, Color tint, float diameter, int order, bool friendly)
    {
        var go = MakeCircle(name, tint, diameter, order);
        var t = go.transform;
        AddChildGlow(t, 3.0f, WithAlpha(tint, 0.45f), order - 2);

        AddChildCircle(t, new Vector2(0f, -0.44f), 0.58f, WithAlpha(tint, tint.a * 0.95f), order);
        AddChildCircle(t, new Vector2(0f, -0.82f), 0.36f, WithAlpha(tint, tint.a * 0.75f), order);
        AddChildCircle(t, new Vector2(0.03f, -1.12f), 0.18f, WithAlpha(tint, tint.a * 0.5f), order);

        Color core = friendly ? new Color(1f, 0.96f, 0.72f, 0.95f) : new Color(0.9f, 0.97f, 1f, 0.9f);
        AddChildCircle(t, new Vector2(-0.07f, 0.10f), 0.44f, core, order + 1);

        if (friendly)
        {
            AddChildCircle(t, new Vector2(0f, 0.5f), 0.2f, new Color(1f, 1f, 0.85f, 0.95f), order + 2);
        }
        else
        {
            Color eye = new Color(0.09f, 0.11f, 0.2f, 0.95f);
            AddChildCircle(t, new Vector2(-0.15f, 0.12f), 0.16f, eye, order + 2);
            AddChildCircle(t, new Vector2(0.15f, 0.12f), 0.16f, eye, order + 2);
        }
        return go;
    }

    // ==================== Fonts ====================
    private static Font _font, _display;

    /// <summary>
    /// Assets/Resources に同梱したフォント。WebGL には OS フォントが存在しないため、
    /// 実際に描画できるフォントはビルドに埋め込んだこれだけになる。
    /// </summary>
    public const string EmbeddedFontResource = "Fonts/ShipporiMincho";

    // 本文用:和文が出せる明朝系を優先(シックさ重視)
    // ※ 同梱フォントが見つからなかったときの保険。WebGL では使われない。
    private static string[] _bodyNames =
    {
        "Yu Mincho", "YuMincho", "游明朝", "MS Mincho", "ＭＳ 明朝", "MS PMincho",
        "Hiragino Mincho ProN", "Meiryo", "Yu Gothic", "MS Gothic", "Georgia"
    };

    // 見出し用:お化けらしさのある表示書体(欧文のみに使う)
    private static string[] _displayNames =
    {
        "Chiller", "Gabriola", "Papyrus", "Palatino Linotype", "Book Antiqua",
        "Constantia", "Georgia", "Times New Roman"
    };

    private static string _bodyResource, _displayResource;
    private static bool _warnedNoFont;

    public static void ConfigureFonts(string[] display, string[] body,
                                      string displayResource = null, string bodyResource = null)
    {
        if (display != null && display.Length > 0) { _displayNames = display; _display = null; }
        if (body != null && body.Length > 0) { _bodyNames = body; _font = null; }
        if (displayResource != _displayResource) { _displayResource = displayResource; _display = null; }
        if (bodyResource != _bodyResource) { _bodyResource = bodyResource; _font = null; }
    }

    /// <summary>
    /// 同梱フォント → OS フォント → 組み込みフォントの順に落ちる。
    /// WebGL では OS フォントも組み込みフォント(LegacyRuntime.ttf)も
    /// 実体が OS 依存で何も描画されないため、同梱フォントに載ることが必須。
    /// </summary>
    private static Font BuildFont(string[] names, string resourceName)
    {
        // 1. Assets/Resources のフォント。未指定なら同梱フォントを使う。
        //    シーンに空文字が保存されていてもここで拾えるようにしておく。
        var path = string.IsNullOrEmpty(resourceName) ? EmbeddedFontResource : resourceName;
        var r = Resources.Load<Font>(path);
        if (r == null && path != EmbeddedFontResource) r = Resources.Load<Font>(EmbeddedFontResource);
        if (r != null) return r;

        // 2. OS フォント(WebGL では利用不可)
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            try
            {
                var f = Font.CreateDynamicFontFromOSFont(names, 48);
                if (f != null) return f;
            }
            catch { /* 未対応環境では組み込みへ */ }
        }

        // 3. 組み込み(欧文のみ / WebGL では描画されない)
        if (!_warnedNoFont)
        {
            _warnedNoFont = true;
            Debug.LogError($"[MockUtil] 同梱フォント '{EmbeddedFontResource}' を読み込めませんでした。"
                           + " Assets/Resources/Fonts/ShipporiMincho.ttf が存在するか確認してください。"
                           + " WebGL ではこの状態だと文字が一切描画されません。");
        }
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
               ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    /// <summary>本文用フォント(和文可)。</summary>
    public static Font UIFont => _font != null ? _font : (_font = BuildFont(_bodyNames, _bodyResource));

    /// <summary>見出し用フォント。</summary>
    public static Font DisplayFont => _display != null ? _display : (_display = BuildFont(_displayNames, _displayResource));

    // ==================== uGUI ====================

    public static Canvas CreateCanvas(string name, int sortingOrder = 100)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static RectTransform CreateRect(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return rt;
    }

    /// <summary>display=true で見出し用フォント。</summary>
    public static Text CreateText(Transform parent, string content, int fontSize, TextAnchor anchor,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color? color = null,
        bool display = false)
    {
        var rt = CreateRect(parent, "Text", anchorMin, anchorMax, offsetMin, offsetMax);
        var txt = rt.gameObject.AddComponent<Text>();
        txt.font = display ? DisplayFont : UIFont;
        txt.text = content;
        txt.fontSize = fontSize;
        txt.alignment = anchor;
        txt.color = color ?? Color.white;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;
        return txt;
    }

    public static Image CreateImage(Transform parent, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
        string name = "Image", Sprite sprite = null)
    {
        var rt = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        if (sprite != null) img.sprite = sprite;
        return img;
    }

    /// <summary>親の中心を基準に、指定位置・サイズの矩形/円 Image を作る(UI パーツ組み立て用)。</summary>
    public static Image CreateBox(Transform parent, Color color, Vector2 center, Vector2 size,
        string name = "Box", bool circle = false, Sprite sprite = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = center;
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        if (sprite != null) img.sprite = sprite;
        else if (circle) img.sprite = CircleSprite;
        return img;
    }
}
