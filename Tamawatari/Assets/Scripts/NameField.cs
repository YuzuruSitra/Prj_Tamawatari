using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// タイトルのプレイヤー名入力欄。EventSystem も InputField も使わず、
/// クリック / タップの当たり判定と生のキー入力だけで動く。
///  - PC   : 枠をクリック、または TAB で編集開始。文字は Keyboard.onTextInput から拾う。
///           ENTER で決定、ESC でやめる。
///  - スマホ: 枠をタップすると OS のソフトキーボードが開く。使えない環境(WebGL)では
///           自前の OnScreenKeyboard を出す。
/// 編集中は InputHub.Suppressed を立てて、SPACE / タップでゲームが始まらないようにする。
/// 入れた名前は ScoreBoard.PlayerName に保存され、ランキングに残る。
/// </summary>
public class NameField : MonoBehaviour
{
    private const string Placeholder = "ななし";

    private static readonly Color Cream = new Color(0.94f, 0.90f, 0.83f);
    private static readonly Color Ember = new Color(1f, 0.68f, 0.28f);
    private static readonly Color Dim = new Color(0.62f, 0.58f, 0.58f);

    private RectTransform _box;
    private Image _plate, _underline;
    private Text _value, _hint;

    private string _name = "";
    private string _editing = "";
    private bool _isEditing;
    private int _openedFrame = -1;

    private Keyboard _subscribed;
    private TouchScreenKeyboard _osKeyboard;
    private OnScreenKeyboard _softKeyboard;
    private System.Func<Rect> _area;

    /// <summary>いま決まっている名前。</summary>
    public string Name => _name;
    public bool IsEditing => _isEditing;

    public static NameField Create(Transform parent, float centerY)
    {
        var root = MockUtil.CreateRect(parent, "NameField", Vector2.zero, Vector2.one,
                                       Vector2.zero, Vector2.zero);
        var f = root.gameObject.AddComponent<NameField>();
        f.Build(root, centerY);
        return f;
    }

    private void Awake()
    {
        _area = HitRect;
        _name = ScoreBoard.PlayerName;
    }

    private void OnEnable() => InputHub.AddTouchBlocker(_area);

    private void OnDisable()
    {
        EndEdit(true);
        InputHub.RemoveTouchBlocker(_area);
    }

    /// <summary>枠より少し広めに取った当たり判定。スマホで押しやすくするため。</summary>
    private Rect HitRect()
    {
        var r = MockUtil.ScreenRect(_box);
        if (r.width <= 0f) return r;
        float pad = r.height * 0.45f;
        return new Rect(r.x - pad, r.y - pad, r.width + pad * 2f, r.height + pad * 2f);
    }

    private void Build(Transform root, float centerY)
    {
        MockUtil.CreateText(root, "なまえ", 24, TextAnchor.MiddleRight,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-340f, centerY - 26f), new Vector2(-200f, centerY + 26f),
            new Color(0.82f, 0.76f, 0.72f));

        _box = MockUtil.CreateRect(root, "Box", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-180f, centerY - 29f), new Vector2(340f, centerY + 29f));
        _plate = MockUtil.CreateImage(_box, new Color(0.08f, 0.06f, 0.12f, 0.9f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "plate");
        _underline = MockUtil.CreateImage(_box, MockUtil.WithAlpha(Ember, 0.5f),
            new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 3f), "underline");

        _value = MockUtil.CreateText(_box, "", 30, TextAnchor.MiddleLeft,
            Vector2.zero, Vector2.one, new Vector2(18f, 0f), new Vector2(-18f, 0f), Cream);

        _hint = MockUtil.CreateText(root, "", 18, TextAnchor.UpperCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-440f, centerY - 64f), new Vector2(440f, centerY - 34f), Dim);

        UpdateVisual();
    }

    private void Update()
    {
        if (_isEditing) UpdateEditing();
        else UpdateIdle();
        UpdateVisual();
    }

    private void UpdateIdle()
    {
        if (InputHub.TryGetTapPosition(out var pos) && HitRect().Contains(pos))
        {
            BeginEdit();
            return;
        }
        var k = PhysicalKeyboard;
        if (k != null && k.tabKey.wasPressedThisFrame) BeginEdit();
    }

    /// <summary>スマホ環境をまねている間は物理キーボードを見ない(実機に条件を揃える)。</summary>
    private static Keyboard PhysicalKeyboard => InputHub.PhysicalInputEnabled ? Keyboard.current : null;

    private void UpdateEditing()
    {
        var k = PhysicalKeyboard;
        if (k != null)
        {
            if (k.escapeKey.wasPressedThisFrame) { EndEdit(false); return; }
            if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame) { EndEdit(true); return; }
            if (k.backspaceKey.wasPressedThisFrame && _editing.Length > 0)
                _editing = _editing.Substring(0, _editing.Length - 1);
        }

        // OS のソフトキーボード(Android / iOS)
        if (_osKeyboard != null && Time.frameCount != _openedFrame)
        {
            _editing = ScoreBoard.Sanitize(_osKeyboard.text);
            if (_osKeyboard.status != TouchScreenKeyboard.Status.Visible) { EndEdit(true); return; }
        }

        // 枠とキーボードの外側を触ったら決定して閉じる
        if (Time.frameCount != _openedFrame && InputHub.TryGetTapPosition(out var pos))
        {
            bool inKeyboard = _softKeyboard != null && _softKeyboard.IsOpen
                              && _softKeyboard.ScreenArea.Contains(pos);
            if (!inKeyboard && !HitRect().Contains(pos)) EndEdit(true);
        }
    }

    private void BeginEdit()
    {
        if (_isEditing) return;
        _isEditing = true;
        _editing = _name;
        _openedFrame = Time.frameCount;
        InputHub.Suppressed = true;
        AudioManager.PlayUi();

        var k = PhysicalKeyboard;
        if (k != null && _subscribed == null) { k.onTextInput += OnTextInput; _subscribed = k; }

        if (!InputHub.WantsTouchUi) return;

        // MobileSim 中は OS のソフトキーボードを使わず、WebGL と同じ自前のキーボードを出す
        if (TouchScreenKeyboard.isSupported && !MobileSim.Enabled)
        {
            _osKeyboard = TouchScreenKeyboard.Open(_editing, TouchScreenKeyboardType.Default,
                false, false, false, false, "なまえ", ScoreBoard.NameMaxLength);
        }
        else
        {
            if (_softKeyboard == null)
            {
                _softKeyboard = OnScreenKeyboard.Create();
                _softKeyboard.OnChanged = s => _editing = ScoreBoard.Sanitize(s);
                _softKeyboard.OnCommit = s => { _editing = ScoreBoard.Sanitize(s); EndEdit(true); };
            }
            _softKeyboard.Open(_editing, ScoreBoard.NameMaxLength);
        }
    }

    private void EndEdit(bool commit)
    {
        if (!_isEditing) return;
        _isEditing = false;
        InputHub.Suppressed = false;

        if (_subscribed != null) { _subscribed.onTextInput -= OnTextInput; _subscribed = null; }
        if (_osKeyboard != null) { _osKeyboard.active = false; _osKeyboard = null; }
        if (_softKeyboard != null && _softKeyboard.IsOpen) _softKeyboard.Close();

        if (commit)
        {
            _name = ScoreBoard.Sanitize(_editing);
            ScoreBoard.PlayerName = _name;
        }
        AudioManager.PlayUi();
    }

    /// <summary>物理キーボードから打たれた1文字。制御文字はここでは扱わない。</summary>
    private void OnTextInput(char c)
    {
        if (!_isEditing || c < ' ') return;
        if (c == '|' || c == ';') return;                  // ランキングの保存形式の区切り文字
        if (_editing.Length >= ScoreBoard.NameMaxLength) return;
        _editing += c;
    }

    private void UpdateVisual()
    {
        bool caret = _isEditing && ((Time.unscaledTime * 2f) % 1f) < 0.55f;
        string shown = _isEditing ? _editing : _name;
        bool empty = string.IsNullOrEmpty(shown);

        _value.text = _isEditing ? shown + (caret ? "|" : "") : (empty ? Placeholder : shown);
        _value.color = (!_isEditing && empty) ? Dim : Cream;
        _plate.color = _isEditing ? new Color(0.12f, 0.09f, 0.17f, 0.95f)
                                  : new Color(0.08f, 0.06f, 0.12f, 0.9f);
        _underline.color = MockUtil.WithAlpha(Ember, _isEditing ? 0.95f : 0.5f);

        bool touch = InputHub.WantsTouchUi;
        _hint.text = _isEditing
            ? (touch ? "きめたら 決定 か 外がわをタップ" : "ENTER : きめる      ESC : やめる")
            : (touch ? "ここをタップして なまえ を入れる" : "クリック か TAB で なまえ を入れる");
    }
}
