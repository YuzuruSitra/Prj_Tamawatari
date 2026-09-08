using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// スマホ(タッチ)環境用のバーチャルパッド。
///  - SHIFT だけがボタン。右下の丸を触ると Special 入力になる。
///  - それ以外の場所を触ると Space と同じ Confirm 入力になる(どこでもタップでジャンプ)。
///
/// 当たり判定そのものは InputHub が生のタッチを見て行う。ここは見た目と、
/// ボタンの画面矩形を InputHub に渡すことだけを担当する(EventSystem は使わない)。
///
/// タッチ環境でない間は自分を隠しておき、実際に画面が触られたら出てくる
/// (WebGL でモバイル判定が外れる端末があるため)。
/// </summary>
public class VirtualPad : MonoBehaviour
{
    private TouchTuning _t = new TouchTuning();
    private GameObject _visual;
    private RectTransform _button;
    private Image _glow, _back, _ring;
    private Text _label, _sub, _tapHint;
    private System.Func<Rect> _area;
    private float _flash;
#if UNITY_EDITOR
    private Text _simBadge;      // 開発用。スマホ環境をまねている間だけ出す目印
#endif

    /// <summary>シーンごとに1つ作る。タッチ環境でなければ隠れたまま常駐する。</summary>
    public static VirtualPad Create(TouchTuning tuning)
    {
        var t = tuning ?? new TouchTuning();
        InputHub.ForceTouchUi = t.forceVirtualPad;

        var canvas = MockUtil.CreateCanvas("VirtualPad", 400);
        var pad = canvas.gameObject.AddComponent<VirtualPad>();
        pad._t = t;
        pad.Build(canvas.transform);
        return pad;
    }

    private void Awake() => _area = () => MockUtil.ScreenRect(_button);

    private void OnEnable() => InputHub.SpecialTouchArea = _area;

    private void OnDisable()
    {
        if (InputHub.SpecialTouchArea == _area) InputHub.SpecialTouchArea = null;
        InputHub.TouchUiActive = false;
    }

    private void Build(Transform root)
    {
        _visual = MockUtil.CreateRect(root, "Pad", Vector2.zero, Vector2.one,
                                      Vector2.zero, Vector2.zero).gameObject;
        var v = _visual.transform;

        float d = Mathf.Max(90f, _t.buttonDiameter);
        Vector2 m = _t.buttonMargin;
        Color c = _t.buttonColor;

        _button = MockUtil.CreateRect(v, "ShiftButton", new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-m.x - d, m.y), new Vector2(-m.x, m.y + d));

        _glow = MockUtil.CreateBox(_button, MockUtil.WithAlpha(c, 0f), Vector2.zero,
            new Vector2(d * 1.9f, d * 1.9f), "glow", sprite: MockUtil.GlowSprite);
        _back = MockUtil.CreateBox(_button, new Color(0.06f, 0.05f, 0.1f, 0.62f), Vector2.zero,
            new Vector2(d, d), "back", circle: true);
        _ring = MockUtil.CreateBox(_button, MockUtil.WithAlpha(c, 0.85f), Vector2.zero,
            new Vector2(d, d), "ring", sprite: MockUtil.RingSprite);

        _label = MockUtil.CreateText(_button, "SHIFT", Mathf.RoundToInt(d * 0.22f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-d * 0.5f, -d * 0.04f), new Vector2(d * 0.5f, d * 0.24f),
            new Color(0.98f, 0.95f, 1f), display: true);
        _sub = MockUtil.CreateText(_button, "衝撃 / 統合", Mathf.RoundToInt(d * 0.11f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-d * 0.5f, -d * 0.26f), new Vector2(d * 0.5f, -d * 0.04f),
            new Color(0.86f, 0.82f, 0.92f));

        if (_t.showTapHint)
        {
            _tapHint = MockUtil.CreateText(v, _t.tapHint, 22, TextAnchor.MiddleRight,
                new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-m.x - d - 520f, m.y + d * 0.34f),
                new Vector2(-m.x - d - 24f, m.y + d * 0.66f),
                new Color(0.82f, 0.78f, 0.76f, 0.7f));
        }

#if UNITY_EDITOR
        _simBadge = MockUtil.CreateText(v, "スマホ環境をまねています (開発用)", 20, TextAnchor.UpperRight,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-620f, -50f), new Vector2(-28f, -16f),
            new Color(1f, 0.72f, 0.35f, 0.8f));
        _simBadge.gameObject.SetActive(false);
#endif

        Apply();
    }

    private void Update()
    {
        bool want = InputHub.WantsTouchUi && !InputHub.Suppressed;   // 名前入力中は引っ込める
        if (_visual.activeSelf != want) _visual.SetActive(want);
#if UNITY_EDITOR
        if (_simBadge != null && _simBadge.gameObject.activeSelf != MobileSim.Enabled)
            _simBadge.gameObject.SetActive(MobileSim.Enabled);
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
        Color c = _t.buttonColor;
        float o = Mathf.Clamp01(_t.opacity);

        _glow.color = MockUtil.WithAlpha(c, 0.55f * _flash * o);
        _back.color = new Color(0.06f + 0.16f * _flash, 0.05f + 0.09f * _flash,
                                0.1f + 0.18f * _flash, (0.6f + 0.22f * _flash) * o);
        _ring.color = MockUtil.WithAlpha(Color.Lerp(c, Color.white, 0.45f * _flash), (0.8f + 0.2f * _flash) * o);
        _label.color = new Color(0.98f, 0.95f, 1f, (0.9f + 0.1f * _flash) * o);
        _sub.color = new Color(0.86f, 0.82f, 0.92f, 0.75f * o);
        if (_tapHint != null) _tapHint.color = new Color(0.82f, 0.78f, 0.76f, 0.62f * o);

        float s = 1f - 0.06f * _flash;
        _button.localScale = new Vector3(s, s, 1f);
    }
}
