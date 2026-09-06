using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 入力の一元窓口。キーボードとゲームパッドの両対応。
///   Confirm(決定/狙う・溜める) : Space  /  ゲームパッド ×(South, Xbox の A)
///   Special(衝撃・統合・ランキング) : Shift / ゲームパッド □(West, Xbox の X)
/// 最後に触ったデバイスを覚えていて、UI のボタン表記(ConfirmLabel / SpecialLabel)を
/// キーボード表記とパッド表記で自動的に切り替える。
/// </summary>
public static class InputHub
{
    /// <summary>直近に操作したのがゲームパッドなら true。</summary>
    public static bool UsingGamepad { get; private set; }

    public static string ConfirmLabel => UsingGamepad ? "×" : "SPACE";
    public static string SpecialLabel => UsingGamepad ? "□" : "SHIFT";

    // ---- 入力 ----
    public static bool ConfirmPressed
    {
        get
        {
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && k.spaceKey.wasPressedThisFrame)
                || (g != null && g.buttonSouth.wasPressedThisFrame);
        }
    }

    public static bool ConfirmHeld
    {
        get
        {
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && k.spaceKey.isPressed) || (g != null && g.buttonSouth.isPressed);
        }
    }

    public static bool ConfirmReleased
    {
        get
        {
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && k.spaceKey.wasReleasedThisFrame)
                || (g != null && g.buttonSouth.wasReleasedThisFrame);
        }
    }

    public static bool SpecialPressed
    {
        get
        {
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && (k.leftShiftKey.wasPressedThisFrame || k.rightShiftKey.wasPressedThisFrame))
                || (g != null && g.buttonWest.wasPressedThisFrame);
        }
    }

    public static bool SpecialHeld
    {
        get
        {
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed))
                || (g != null && g.buttonWest.isPressed);
        }
    }

    /// <summary>直近に触ったデバイスを判定する。毎フレーム呼ばれる。</summary>
    public static void Poll()
    {
        var g = Gamepad.current;
        if (g != null)
        {
            if (g.buttonSouth.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame ||
                g.buttonEast.wasPressedThisFrame || g.buttonNorth.wasPressedThisFrame ||
                g.startButton.wasPressedThisFrame ||
                g.leftStick.ReadValue().sqrMagnitude > 0.25f ||
                g.dpad.ReadValue().sqrMagnitude > 0.25f)
            {
                UsingGamepad = true;
            }
        }

        var k = Keyboard.current;
        if (k != null && k.anyKey.wasPressedThisFrame) UsingGamepad = false;
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
