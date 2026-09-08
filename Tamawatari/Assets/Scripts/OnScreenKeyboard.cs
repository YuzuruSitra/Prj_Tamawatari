using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 自前のソフトキーボード。WebGL には OS のソフトキーボード(TouchScreenKeyboard)が
/// 無いため、スマホのブラウザでも名前を入力できる手段としてこれを用意する。
///  - かな(五十音)ページと英数ページを切り替えられる
///  - 濁点 / 半濁点 / 小文字 は直前の1文字に対して掛ける(もう一度押すと戻る)
/// EventSystem は使わず、押された画面座標とキーの矩形を突き合わせて判定する。
/// </summary>
public class OnScreenKeyboard : MonoBehaviour
{
    private const int Cols = 10;
    private const int Rows = 5;

    // 五十音を段(横)×行(縦)に並べたもの。\u3000 はキーを置かない印。
    private static readonly string[] KanaRows =
    {
        "あかさたなはまやらわ",
        "いきしちにひみ\u3000りを",
        "うくすつぬふむゆるん",
        "えけせてねへめ\u3000れー",
        "おこそとのほもよろ\u3000",
    };

    private static readonly string[] AlnumRows =
    {
        "ABCDEFGHIJ",
        "KLMNOPQRST",
        "UVWXYZ0123",
        "456789-_.\u3000",
        "\u3000\u3000\u3000\u3000\u3000\u3000\u3000\u3000\u3000\u3000",
    };

    // 左が素の字、右が変換後。偶数番目から奇数番目へ、もう一度押すと逆に戻る。
    private const string Dakuten =
        "かがきぎくぐけげこごさざしじすずせぜそぞただちぢつづてでとどはばひびふぶへべほぼ";
    private const string Handakuten = "はぱひぴふぷへぺほぽ";
    private const string Small = "あぁいぃうぅえぇおぉつっやゃゆゅよょ";

    private enum KeyKind { Char, Backspace, Dakuten, Handakuten, Small, TogglePage, Commit }

    private class Key
    {
        public RectTransform Rt;
        public Image Bg;
        public Text Label;
        public string Ch = "";
        public KeyKind Kind;
    }

    private static readonly Color Cream = new Color(0.94f, 0.90f, 0.83f);
    private static readonly Color Ember = new Color(1f, 0.68f, 0.28f);
    private static readonly Color KeyFace = new Color(0.16f, 0.13f, 0.2f, 0.96f);
    private static readonly Color KeyFaceLit = new Color(0.42f, 0.3f, 0.5f, 0.98f);

    private readonly List<Key> _charKeys = new List<Key>();
    private readonly List<Key> _funcKeys = new List<Key>();
    private RectTransform _panel;
    private Image _dim;
    private Text _preview;
    private Key _pressed;
    private float _pressT;
    private bool _kana = true;
    private string _text = "";
    private int _maxLength = 10;
    private int _openedFrame = -1;

    /// <summary>入力中の文字列が変わったとき。</summary>
    public System.Action<string> OnChanged;
    /// <summary>決定キーが押されたとき。</summary>
    public System.Action<string> OnCommit;

    public bool IsOpen => gameObject.activeSelf;
    public string Text => _text;
    /// <summary>キーボードが占める画面矩形(外側を触ったかの判定に使う)。</summary>
    public Rect ScreenArea => MockUtil.ScreenRect(_panel);

    public static OnScreenKeyboard Create()
    {
        var canvas = MockUtil.CreateCanvas("SoftKeyboard", 500);
        var kb = canvas.gameObject.AddComponent<OnScreenKeyboard>();
        kb.Build(canvas.transform);
        canvas.gameObject.SetActive(false);
        return kb;
    }

    public void Open(string initial, int maxLength)
    {
        _text = initial ?? "";
        _maxLength = Mathf.Max(1, maxLength);
        _kana = true;
        ApplyPage();
        UpdatePreview();
        _openedFrame = Time.frameCount;
        gameObject.SetActive(true);
    }

    public void Close() => gameObject.SetActive(false);

    // ==================== 組み立て ====================
    private Key _toggleKey;

    private void Build(Transform root)
    {
        _dim = MockUtil.CreateImage(root, new Color(0.02f, 0.015f, 0.04f, 0.82f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "Dim");

        _panel = MockUtil.CreateRect(root, "Panel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-500f, 20f), new Vector2(500f, 660f));
        MockUtil.CreateImage(_panel, new Color(0.09f, 0.07f, 0.13f, 0.985f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "Plate");
        MockUtil.CreateBox(_panel, MockUtil.WithAlpha(Ember, 0.85f), new Vector2(0f, 318f),
            new Vector2(1000f, 4f), "topRule");

        MockUtil.CreateBox(_panel, new Color(0.03f, 0.02f, 0.05f, 0.92f), new Vector2(0f, 270f),
            new Vector2(950f, 72f), "previewPlate");
        _preview = MockUtil.CreateText(_panel, "", 40, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-466f, 234f), new Vector2(466f, 306f), Cream);

        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                _charKeys.Add(MakeKey(new Vector2(-441f + c * 98f, 184f - r * 84f),
                                      new Vector2(90f, 76f), "", KeyKind.Char, 34));

        var fs = new Vector2(155f, 84f);
        _funcKeys.Add(MakeKey(new Vector2(-408f, -244f), fs, "゛", KeyKind.Dakuten, 34));
        _funcKeys.Add(MakeKey(new Vector2(-245f, -244f), fs, "゜", KeyKind.Handakuten, 34));
        _funcKeys.Add(MakeKey(new Vector2(-82f, -244f), fs, "小", KeyKind.Small, 30));
        _toggleKey = MakeKey(new Vector2(81f, -244f), fs, "ABC", KeyKind.TogglePage, 28);
        _funcKeys.Add(_toggleKey);
        _funcKeys.Add(MakeKey(new Vector2(244f, -244f), fs, "けす", KeyKind.Backspace, 28));
        var commit = MakeKey(new Vector2(407f, -244f), fs, "決定", KeyKind.Commit, 30);
        commit.Bg.color = new Color(0.44f, 0.27f, 0.1f, 0.96f);
        _funcKeys.Add(commit);

        ApplyPage();
    }

    private Key MakeKey(Vector2 center, Vector2 size, string label, KeyKind kind, int fontSize)
    {
        var bg = MockUtil.CreateBox(_panel, KeyFace, center, size, "key");
        var rt = bg.rectTransform;
        var txt = MockUtil.CreateText(rt, label, fontSize, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Cream);
        return new Key { Rt = rt, Bg = bg, Label = txt, Ch = label, Kind = kind };
    }

    /// <summary>かな / 英数 のページを貼り替える。字が無いところはキー自体を消す。</summary>
    private void ApplyPage()
    {
        var rows = _kana ? KanaRows : AlnumRows;
        for (int r = 0; r < Rows; r++)
        {
            string row = r < rows.Length ? rows[r] : "";
            for (int c = 0; c < Cols; c++)
            {
                var key = _charKeys[r * Cols + c];
                char ch = c < row.Length ? row[c] : '\u3000';
                bool has = ch != '\u3000';
                key.Ch = has ? ch.ToString() : "";
                key.Label.text = key.Ch;
                if (key.Rt.gameObject.activeSelf != has) key.Rt.gameObject.SetActive(has);
            }
        }
        if (_toggleKey != null) _toggleKey.Label.text = _kana ? "ABC" : "かな";
    }

    // ==================== 入力 ====================
    private void Update()
    {
        UpdatePreview();

        if (_pressT > 0f)
        {
            _pressT -= Time.unscaledDeltaTime;
            if (_pressT <= 0f && _pressed != null) { Tint(_pressed, false); _pressed = null; }
        }

        if (Time.frameCount == _openedFrame) return;   // 開いた瞬間のタップでキーを押さない
        if (!InputHub.TryGetTapPosition(out var pos)) return;

        var hit = Find(pos);
        if (hit == null) return;

        AudioManager.PlayUi();
        if (_pressed != null) Tint(_pressed, false);
        _pressed = hit;
        _pressT = 0.12f;
        Tint(hit, true);
        Press(hit);
    }

    private Key Find(Vector2 pos)
    {
        for (int i = 0; i < _charKeys.Count; i++)
        {
            var k = _charKeys[i];
            if (k.Rt.gameObject.activeSelf && MockUtil.ScreenRect(k.Rt).Contains(pos)) return k;
        }
        for (int i = 0; i < _funcKeys.Count; i++)
        {
            var k = _funcKeys[i];
            if (MockUtil.ScreenRect(k.Rt).Contains(pos)) return k;
        }
        return null;
    }

    private void Press(Key k)
    {
        switch (k.Kind)
        {
            case KeyKind.Char:
                if (_text.Length < _maxLength) _text += k.Ch;
                break;
            case KeyKind.Backspace:
                if (_text.Length > 0) _text = _text.Substring(0, _text.Length - 1);
                break;
            case KeyKind.Dakuten: Convert(Dakuten); break;
            case KeyKind.Handakuten: Convert(Handakuten); break;
            case KeyKind.Small: Convert(Small); break;
            case KeyKind.TogglePage:
                _kana = !_kana;
                ApplyPage();
                return;
            case KeyKind.Commit:
                OnCommit?.Invoke(_text);
                return;
        }
        OnChanged?.Invoke(_text);
        UpdatePreview();
    }

    /// <summary>直前の1文字を変換表で置き換える。もう一度押すと元に戻る。</summary>
    private void Convert(string table)
    {
        if (_text.Length == 0) return;
        int i = table.IndexOf(_text[_text.Length - 1]);
        if (i < 0) return;
        char to = (i % 2 == 0) ? table[i + 1] : table[i - 1];
        _text = _text.Substring(0, _text.Length - 1) + to;
    }

    private void UpdatePreview()
    {
        bool caret = ((Time.unscaledTime * 2f) % 1f) < 0.55f;
        _preview.text = _text + (caret ? "|" : "");
    }

    private static void Tint(Key k, bool lit)
    {
        if (k == null || k.Kind == KeyKind.Commit) return;
        k.Bg.color = lit ? KeyFaceLit : KeyFace;
    }
}
