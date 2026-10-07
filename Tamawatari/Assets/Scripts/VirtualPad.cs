using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// スマホ(タッチ)環境用のバーチャルパッド(<c>Assets/Prefabs/UI/VirtualPad.prefab</c>)。
///  - SHIFT だけがボタン。右下の丸を触ると Special 入力になる。
///  - それ以外の場所を触ると Space と同じ Confirm 入力になる(どこでもタップでジャンプ)。
///
/// 当たり判定そのものは InputHub が生のタッチを見て行う。ここは見た目と、
/// ボタンの画面矩形を InputHub に渡すことだけを担当する(EventSystem は使わない)。
///
/// タッチ環境でない間は自分を隠しておき、実際に画面が触られたら出てくる
/// (WebGL でモバイル判定が外れる端末があるため)。
/// スコア画面の画像を撮っている間(<see cref="ScoreShot.IsCapturing"/>)も、
/// 写り込まないように引っ込む。
/// </summary>
public class VirtualPad : MonoBehaviour
{
    [Header("プレハブ上のパーツ")]
    [SerializeField] private GameObject visual;
    [SerializeField] private RectTransform button;
    [SerializeField] private Image glow;
    [SerializeField] private Image back;
    [SerializeField] private Image ring;
    [SerializeField] private Text label;
    [SerializeField] private Text sub;
    [SerializeField] private Text tapHint;
#if UNITY_EDITOR
    [SerializeField] private Text simBadge;   // 開発用。スマホ環境をまねている間だけ出す目印
#endif

    private TouchTuning _t = new TouchTuning();
    private System.Func<Rect> _area;
    private float _flash;

    /// <summary>シーンごとに1つ作る。タッチ環境でなければ隠れたまま常駐する。</summary>
    public static VirtualPad Create(TouchTuning tuning)
    {
        var t = tuning ?? new TouchTuning();
        InputHub.ForceTouchUi = t.forceVirtualPad;

        var go = GameAssets.Spawn(GameAssets.I != null ? GameAssets.I.virtualPad : null);
        if (go == null) return null;

        var pad = go.GetComponent<VirtualPad>();
        if (pad != null) pad.ApplyLayout(t);
        return pad;
    }

    private void Awake() => _area = () => GameArt.ScreenRect(button);

    private void OnEnable() => InputHub.SpecialTouchArea = _area;

    private void OnDisable()
    {
        if (InputHub.SpecialTouchArea == _area) InputHub.SpecialTouchArea = null;
        InputHub.TouchUiActive = false;
    }

    /// <summary>
    /// ボタンの大きさ・位置・文言を TouchTuning に合わせる。
    /// プレハブは既定値(直径 250)で焼いてあり、ここで置き直す。
    /// </summary>
    public void ApplyLayout(TouchTuning tuning)
    {
        if (tuning != null) _t = tuning;
        if (button == null) return;

        float d = Mathf.Max(90f, _t.buttonDiameter);
        Vector2 m = _t.buttonMargin;

        button.offsetMin = new Vector2(-m.x - d, m.y);
        button.offsetMax = new Vector2(-m.x, m.y + d);

        SetSize(glow, d * 1.9f, d * 1.9f);
        SetSize(back, d, d);
        SetSize(ring, d, d);

        if (label != null)
        {
            label.fontSize = Mathf.RoundToInt(d * 0.22f);
            SetOffsets(label.rectTransform, new Vector2(-d * 0.5f, -d * 0.04f), new Vector2(d * 0.5f, d * 0.24f));
        }
        if (sub != null)
        {
            sub.fontSize = Mathf.RoundToInt(d * 0.11f);
            SetOffsets(sub.rectTransform, new Vector2(-d * 0.5f, -d * 0.26f), new Vector2(d * 0.5f, -d * 0.04f));
        }
        if (tapHint != null)
        {
            tapHint.text = _t.tapHint;
            SetOffsets(tapHint.rectTransform,
                new Vector2(-m.x - d - 520f, m.y + d * 0.34f),
                new Vector2(-m.x - d - 24f, m.y + d * 0.66f));
            if (tapHint.gameObject.activeSelf != _t.showTapHint) tapHint.gameObject.SetActive(_t.showTapHint);
        }

        Apply();
    }

    private static void SetSize(Image img, float w, float h)
    {
        if (img != null) img.rectTransform.sizeDelta = new Vector2(w, h);
    }

    private static void SetOffsets(RectTransform rt, Vector2 min, Vector2 max)
    {
        if (rt == null) return;
        rt.offsetMin = min;
        rt.offsetMax = max;
    }

    private void Update()
    {
        // 名前入力中とスコア画面の撮影中は引っ込める
        bool want = InputHub.WantsTouchUi && !InputHub.Suppressed && !ScoreShot.IsCapturing;
        if (visual != null && visual.activeSelf != want) visual.SetActive(want);
#if UNITY_EDITOR
        if (simBadge != null && simBadge.gameObject.activeSelf != MobileSim.Enabled)
            simBadge.gameObject.SetActive(MobileSim.Enabled);
#endif
        InputHub.TouchUiActive = want;
        if (!want) return;

        float target = InputHub.SpecialHeld ? 1f : 0f;
        _flash = Mathf.MoveTowards(_flash, target, Time.unscaledDeltaTime * (target > _flash ? 14f : 6f));
        Apply();
    }

    /// <summary>押されている間だけ明るく、わずかに縮む。</summary>
    private void Apply()
    {
        if (button == null) return;
        Color c = _t.buttonColor;
        float o = Mathf.Clamp01(_t.opacity);

        if (glow != null) glow.color = GameArt.WithAlpha(c, 0.55f * _flash * o);
        if (back != null)
            back.color = new Color(0.06f + 0.16f * _flash, 0.05f + 0.09f * _flash,
                                   0.1f + 0.18f * _flash, (0.6f + 0.22f * _flash) * o);
        if (ring != null)
            ring.color = GameArt.WithAlpha(Color.Lerp(c, Color.white, 0.45f * _flash), (0.8f + 0.2f * _flash) * o);
        if (label != null) label.color = new Color(0.98f, 0.95f, 1f, (0.9f + 0.1f * _flash) * o);
        if (sub != null) sub.color = new Color(0.86f, 0.82f, 0.92f, 0.75f * o);
        if (tapHint != null) tapHint.color = new Color(0.82f, 0.78f, 0.76f, 0.62f * o);

        float s = 1f - 0.06f * _flash;
        button.localScale = new Vector3(s, s, 1f);
    }

#if UNITY_EDITOR
    /// <summary>焼き直しツールから、プレハブのパーツ参照を差し込むために使う。</summary>
    public void BindPartsForBake(GameObject padVisual, RectTransform shiftButton, Image glowImg, Image backImg,
                                 Image ringImg, Text labelText, Text subText, Text hintText, Text badge)
    {
        visual = padVisual; button = shiftButton; glow = glowImg; back = backImg; ring = ringImg;
        label = labelText; sub = subText; tapHint = hintText; simBadge = badge;
    }
#endif
}
