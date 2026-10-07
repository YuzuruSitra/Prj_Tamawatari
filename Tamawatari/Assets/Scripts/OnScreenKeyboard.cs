using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 自前のソフトキーボード。WebGL には OS のソフトキーボード(TouchScreenKeyboard)が
/// 無いため、スマホのブラウザでも名前を入力できる手段としてこれを用意する。
///  - かな(五十音)ページと英数ページを切り替えられる
///  - 濁点 / 半濁点 / 小文字 は直前の1文字に対して掛ける(もう一度押すと戻る)
/// EventSystem は使わず、押された画面座標とキーの矩形を突き合わせて判定する。
///
/// キーの並びは <c>Assets/Prefabs/UI/SoftKeyboard.prefab</c> に焼いてある。
/// ここでは押されたキーの見た目と文字の出し入れだけを行う。
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

    public enum KeyKind { Char, Backspace, Dakuten, Handakuten, Small, TogglePage, Commit }

    [System.Serializable]
    public class Key
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

    [Header("プレハブ上のパーツ")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private Text preview;
    [Tooltip("五十音 / 英数を貼り替える文字キー。Rows x Cols の並び順")]
    [SerializeField] private Key[] charKeys;
    [Tooltip("濁点・小文字・ページ切替・けす・決定")]
    [SerializeField] private Key[] funcKeys;

    private Key _toggleKey;
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
    public Rect ScreenArea => GameArt.ScreenRect(panel);

    public static OnScreenKeyboard Create()
    {
        var go = GameAssets.Spawn(GameAssets.I != null ? GameAssets.I.softKeyboard : null);
        if (go == null) return null;
        go.SetActive(false);
        return go.GetComponent<OnScreenKeyboard>();
    }

    private void Awake()
    {
        if (funcKeys == null) return;
        foreach (var k in funcKeys)
            if (k != null && k.Kind == KeyKind.TogglePage) { _toggleKey = k; break; }
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

    /// <summary>かな / 英数 のページを貼り替える。字が無いところはキー自体を消す。</summary>
    private void ApplyPage()
    {
        if (charKeys == null || charKeys.Length < Rows * Cols) return;
        if (_toggleKey == null) Awake();

        var rows = _kana ? KanaRows : AlnumRows;
        for (int r = 0; r < Rows; r++)
        {
            string row = r < rows.Length ? rows[r] : "";
            for (int c = 0; c < Cols; c++)
            {
                var key = charKeys[r * Cols + c];
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
        for (int i = 0; charKeys != null && i < charKeys.Length; i++)
        {
            var k = charKeys[i];
            if (k?.Rt != null && k.Rt.gameObject.activeSelf && GameArt.ScreenRect(k.Rt).Contains(pos)) return k;
        }
        for (int i = 0; funcKeys != null && i < funcKeys.Length; i++)
        {
            var k = funcKeys[i];
            if (k?.Rt != null && GameArt.ScreenRect(k.Rt).Contains(pos)) return k;
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
        if (preview == null) return;
        bool caret = ((Time.unscaledTime * 2f) % 1f) < 0.55f;
        preview.text = _text + (caret ? "|" : "");
    }

    private static void Tint(Key k, bool lit)
    {
        if (k == null || k.Kind == KeyKind.Commit) return;
        k.Bg.color = lit ? KeyFaceLit : KeyFace;
    }

#if UNITY_EDITOR
    /// <summary>焼き直しツールから、プレハブのパーツ参照を差し込むために使う。</summary>
    public void BindPartsForBake(RectTransform panelRt, Text previewText, Key[] chars, Key[] funcs)
    {
        panel = panelRt; preview = previewText; charKeys = chars; funcKeys = funcs;
    }
#endif
}
