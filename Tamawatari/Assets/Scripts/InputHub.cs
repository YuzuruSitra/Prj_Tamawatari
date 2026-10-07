using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 入力の一元窓口。キーボード / ゲームパッド / タッチ(スマホ)の三対応。
///   Confirm(決定/狙う・溜める) : Space  /  ゲームパッド ×(South, Xbox の A)  /  画面タップ
///   Special(衝撃・統合・ランキング) : Shift / ゲームパッド □(West, Xbox の X) / バーチャルボタン
///   Capture(スコア画面の画像ほぞん) : P / ゲームパッド △(North, Xbox の Y) / 画面のボタン
///
/// スマホでは「SHIFT だけがボタン、それ以外のどこを触っても Space と同じ」。
/// ボタンの矩形は SpecialTouchArea を VirtualPad が差し込み、触っても Confirm に
/// したくない領域(名前入力欄など)は AddTouchBlocker で登録する。
/// スコア画面の「画像ほぞん」も同じ仕組みで、CaptureTouchArea を UIManager が差し込む。
///
/// 最後に触ったデバイスを覚えていて、UI のボタン表記(ConfirmLabel / SpecialLabel /
/// CaptureLabel)をキーボード表記 / パッド表記 / タッチ表記で自動的に切り替える。
/// </summary>
public static class InputHub
{
    /// <summary>直近に操作したのがゲームパッドなら true。</summary>
    public static bool UsingGamepad { get; private set; }

    /// <summary>直近に操作したのが画面タッチなら true。</summary>
    public static bool UsingTouch { get; private set; }

    public static string ConfirmLabel => UsingTouch ? "タップ" : UsingGamepad ? "×" : "SPACE";
    public static string SpecialLabel => UsingGamepad ? "□" : "SHIFT";
    /// <summary>"タップ" だけだと「どこでもタップ」の Confirm と見分けが付かないので場所まで言う。</summary>
    public static string CaptureLabel => UsingTouch ? "ここをタップ" : UsingGamepad ? "△" : "P";

    /// <summary>名前入力中など、ゲーム側の入力を丸ごと止めたいときに立てる。</summary>
    public static bool Suppressed { get; set; }

    // ==================== タッチ(スマホ) ====================

    /// <summary>エディタでもバーチャルパッドを確認するための強制フラグ(TouchTuning から差し込む)。</summary>
    public static bool ForceTouchUi { get; set; }

    /// <summary>一度でも実際に画面を触ったか。WebGL でモバイル判定が外れた端末の保険。</summary>
    public static bool TouchSeen { get; private set; }

    /// <summary>バーチャルパッドを出すべき環境か。</summary>
    public static bool WantsTouchUi =>
        MobileSim.Enabled || ForceTouchUi || Application.isMobilePlatform || TouchSeen;

    /// <summary>
    /// 物理キーボード / ゲームパッドを入力として見るか。スマホ環境をまねている間は
    /// false になり、実機と同じくタッチ(とマウス=指)だけで遊ぶことになる。
    /// </summary>
    public static bool PhysicalInputEnabled => !MobileSim.Enabled;

    private static Keyboard Key => PhysicalInputEnabled ? Keyboard.current : null;
    private static Gamepad Pad => PhysicalInputEnabled ? Gamepad.current : null;

    private static bool _touchUiActive;

    /// <summary>VirtualPad が表示されている間だけ true。タッチを入力として拾うのはこの間だけ。</summary>
    public static bool TouchUiActive
    {
        get => _touchUiActive;
        set
        {
            if (_touchUiActive == value) return;
            _touchUiActive = value;
            if (value) UsingTouch = true;          // 出した時点で表記をタッチ用にしておく
        }
    }

    /// <summary>SHIFT のバーチャルボタンの画面矩形。VirtualPad が生きている間だけ入る。</summary>
    public static System.Func<Rect> SpecialTouchArea { get; set; }

    /// <summary>スコア画面の「画像ほぞん」ボタンの画面矩形。UIManager が差し込む。</summary>
    public static System.Func<Rect> CaptureTouchArea { get; set; }

    // 触っても Confirm にしない領域(名前入力欄など)
    private static readonly List<System.Func<Rect>> _blockers = new List<System.Func<Rect>>();

    public static void AddTouchBlocker(System.Func<Rect> area)
    {
        if (area != null && !_blockers.Contains(area)) _blockers.Add(area);
    }

    public static void RemoveTouchBlocker(System.Func<Rect> area)
    {
        if (area != null) _blockers.Remove(area);
    }

    private enum TouchKind : byte { None = 0, Confirm, Special, Capture, Ignored }

    private const int MaxTouches = 10;
    private static readonly TouchKind[] _slotKind = new TouchKind[MaxTouches];
    private static TouchKind _mouseKind;
    private static bool _tConfirmDown, _tConfirmHeld, _tConfirmUp, _tSpecialDown, _tSpecialHeld;
    private static bool _tCaptureDown, _tCaptureHeld;
    private static int _touchFrame = -1;

    /// <summary>触った位置がどの入力になるかを決める。</summary>
    private static TouchKind Classify(Vector2 screenPos)
    {
        var special = SpecialTouchArea;
        if (special != null && special().Contains(screenPos)) return TouchKind.Special;
        var capture = CaptureTouchArea;
        if (capture != null && capture().Contains(screenPos)) return TouchKind.Capture;
        for (int i = 0; i < _blockers.Count; i++)
        {
            var b = _blockers[i];
            if (b != null && b().Contains(screenPos)) return TouchKind.Ignored;
        }
        return TouchKind.Confirm;
    }

    /// <summary>
    /// タッチの状態を1フレームに1回だけ作り直す。Update の実行順に依存しないよう、
    /// 最初に参照されたタイミングで遅延計算する。
    /// </summary>
    private static void EnsureTouch()
    {
        if (_touchFrame == Time.frameCount) return;
        _touchFrame = Time.frameCount;
        _tConfirmDown = _tConfirmHeld = _tConfirmUp = false;
        _tSpecialDown = _tSpecialHeld = false;
        _tCaptureDown = _tCaptureHeld = false;
        if (!TouchUiActive) return;

        var ts = Touchscreen.current;
        if (ts != null)
        {
            var touches = ts.touches;
            int n = Mathf.Min(touches.Count, MaxTouches);
            for (int i = 0; i < n; i++)
            {
                var t = touches[i];
                bool down = t.press.wasPressedThisFrame;
                bool up = t.press.wasReleasedThisFrame;
                if (down) _slotKind[i] = Classify(t.position.ReadValue());

                var kind = _slotKind[i];
                if (kind == TouchKind.Confirm)
                {
                    if (down) _tConfirmDown = true;
                    if (up) _tConfirmUp = true;
                    if (t.press.isPressed) _tConfirmHeld = true;
                }
                else if (kind == TouchKind.Special)
                {
                    if (down) _tSpecialDown = true;
                    if (t.press.isPressed) _tSpecialHeld = true;
                }
                else if (kind == TouchKind.Capture)
                {
                    if (down) _tCaptureDown = true;
                    if (t.press.isPressed) _tCaptureHeld = true;
                }
                if (up) _slotKind[i] = TouchKind.None;
            }
        }

        // エディタ確認用。マウス左ボタンを指の代わりに扱う(実機では使わない)。
        if (ForceTouchUi || MobileSim.Enabled)
        {
            var m = Mouse.current;
            if (m != null)
            {
                bool down = m.leftButton.wasPressedThisFrame;
                bool up = m.leftButton.wasReleasedThisFrame;
                if (down) _mouseKind = Classify(m.position.ReadValue());
                if (_mouseKind == TouchKind.Confirm)
                {
                    if (down) _tConfirmDown = true;
                    if (up) _tConfirmUp = true;
                    if (m.leftButton.isPressed) _tConfirmHeld = true;
                }
                else if (_mouseKind == TouchKind.Special)
                {
                    if (down) _tSpecialDown = true;
                    if (m.leftButton.isPressed) _tSpecialHeld = true;
                }
                else if (_mouseKind == TouchKind.Capture)
                {
                    if (down) _tCaptureDown = true;
                    if (m.leftButton.isPressed) _tCaptureHeld = true;
                }
                if (up) _mouseKind = TouchKind.None;
            }
        }
    }

    // ==================== 入力 ====================
    public static bool ConfirmPressed
    {
        get
        {
            if (Suppressed) return false;
            EnsureTouch();
            var k = Key; var g = Pad;
            return (k != null && k.spaceKey.wasPressedThisFrame)
                || (g != null && g.buttonSouth.wasPressedThisFrame)
                || _tConfirmDown;
        }
    }

    public static bool ConfirmHeld
    {
        get
        {
            if (Suppressed) return false;
            EnsureTouch();
            var k = Key; var g = Pad;
            return (k != null && k.spaceKey.isPressed)
                || (g != null && g.buttonSouth.isPressed)
                || _tConfirmHeld;
        }
    }

    public static bool ConfirmReleased
    {
        get
        {
            if (Suppressed) return false;
            EnsureTouch();
            var k = Key; var g = Pad;
            return (k != null && k.spaceKey.wasReleasedThisFrame)
                || (g != null && g.buttonSouth.wasReleasedThisFrame)
                || _tConfirmUp;
        }
    }

    public static bool SpecialPressed
    {
        get
        {
            if (Suppressed) return false;
            EnsureTouch();
            var k = Key; var g = Pad;
            return (k != null && (k.leftShiftKey.wasPressedThisFrame || k.rightShiftKey.wasPressedThisFrame))
                || (g != null && g.buttonWest.wasPressedThisFrame)
                || _tSpecialDown;
        }
    }

    public static bool SpecialHeld
    {
        get
        {
            if (Suppressed) return false;
            EnsureTouch();
            var k = Key; var g = Pad;
            return (k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed))
                || (g != null && g.buttonWest.isPressed)
                || _tSpecialHeld;
        }
    }

    public static bool CapturePressed
    {
        get
        {
            if (Suppressed) return false;
            EnsureTouch();
            var k = Key; var g = Pad;
            return (k != null && k.pKey.wasPressedThisFrame)
                || (g != null && g.buttonNorth.wasPressedThisFrame)
                || _tCaptureDown;
        }
    }

    public static bool CaptureHeld
    {
        get
        {
            if (Suppressed) return false;
            EnsureTouch();
            var k = Key; var g = Pad;
            return (k != null && k.pKey.isPressed)
                || (g != null && g.buttonNorth.isPressed)
                || _tCaptureHeld;
        }
    }

    /// <summary>
    /// この瞬間に押された画面座標(タッチ or マウス左ボタン)を返す。
    /// EventSystem を使わない UI (名前入力欄・ソフトキーボード)の当たり判定用。
    /// </summary>
    public static bool TryGetTapPosition(out Vector2 pos)
    {
        pos = Vector2.zero;
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
        {
            pos = ts.primaryTouch.position.ReadValue();
            return true;
        }
        var m = Mouse.current;
        if (m != null && m.leftButton.wasPressedThisFrame)
        {
            pos = m.position.ReadValue();
            return true;
        }
        return false;
    }

    /// <summary>直近に触ったデバイスを判定する。毎フレーム呼ばれる。</summary>
    public static void Poll()
    {
        // 実際のタッチを見張る。TouchUiActive でなくてもここだけは見て、
        // モバイル判定が外れた端末でもバーチャルパッドを出せるようにする。
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
        {
            TouchSeen = true;
            UsingTouch = true;
            UsingGamepad = false;
        }

        var g = Pad;
        if (g != null)
        {
            if (g.buttonSouth.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame ||
                g.buttonEast.wasPressedThisFrame || g.buttonNorth.wasPressedThisFrame ||
                g.startButton.wasPressedThisFrame ||
                g.leftStick.ReadValue().sqrMagnitude > 0.25f ||
                g.dpad.ReadValue().sqrMagnitude > 0.25f)
            {
                UsingGamepad = true;
                UsingTouch = false;
            }
        }

        var k = Key;
        if (k != null && k.anyKey.wasPressedThisFrame) { UsingGamepad = false; UsingTouch = false; }
    }

    // ---- 自前で毎フレーム Poll するための常駐オブジェクト ----
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var go = new GameObject("~InputHub");
        go.hideFlags = HideFlags.HideAndDontSave;
        Object.DontDestroyOnLoad(go);
        go.AddComponent<InputHubUpdater>();
    }
}

/// <summary>InputHub.Poll を毎フレーム回すだけの常駐コンポーネント。</summary>
public class InputHubUpdater : MonoBehaviour
{
    private void Update() => InputHub.Poll();
}
