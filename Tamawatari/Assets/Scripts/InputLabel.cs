using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Text にボタン表記を差し込む。書式内の {C} が決定ボタン、{S} が特殊ボタンに置き換わり、
/// キーボード / ゲームパッドの切り替えに追従して自動で書き直される。
/// </summary>
public class InputLabel : MonoBehaviour
{
    private Text _text;
    private string _format;
    private bool _lastUsingPad, _lastUsingTouch;
    private bool _init;

    public static InputLabel Bind(Text text, string format)
    {
        if (text == null) return null;
        var l = text.gameObject.AddComponent<InputLabel>();
        l._text = text;
        l._format = format;
        l.Apply();
        return l;
    }

    private void Apply()
    {
        if (_text == null) return;
        _text.text = _format.Replace("{C}", InputHub.ConfirmLabel).Replace("{S}", InputHub.SpecialLabel);
        _lastUsingPad = InputHub.UsingGamepad;
        _lastUsingTouch = InputHub.UsingTouch;
        _init = true;
    }

    private void Update()
    {
        if (!_init) return;
        if (_lastUsingPad == InputHub.UsingGamepad && _lastUsingTouch == InputHub.UsingTouch) return;
        Apply();
    }
}
