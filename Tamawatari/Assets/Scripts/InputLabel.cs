using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Text にボタン表記を差し込む。書式内の {C} が決定ボタン、{S} が特殊ボタン、
/// {P} が画像ほぞんボタンに置き換わり、
/// キーボード / ゲームパッドの切り替えに追従して自動で書き直される。
///
/// 書式はプレハブに保存されている。中身を差し替えたいときだけ <see cref="Format"/> を書く。
/// </summary>
public class InputLabel : MonoBehaviour
{
    [SerializeField] private Text text;
    [SerializeField, TextArea] private string format = "";

    private bool _lastUsingPad, _lastUsingTouch;
    private bool _init;

    /// <summary>{C} / {S} / {P} を含む書式。</summary>
    public string Format
    {
        get => format;
        set { format = value ?? ""; Apply(); }
    }

    /// <summary>既存の Text に貼り付ける(プレハブを焼くときに使う)。</summary>
    public static InputLabel Bind(Text target, string fmt)
    {
        if (target == null) return null;
        var l = target.GetComponent<InputLabel>() ?? target.gameObject.AddComponent<InputLabel>();
        l.text = target;
        l.format = fmt;
        l.Apply();
        return l;
    }

    private void OnEnable() => Apply();

    private void Apply()
    {
        if (text == null) text = GetComponent<Text>();
        if (text == null) return;
        text.text = format.Replace("{C}", InputHub.ConfirmLabel)
                          .Replace("{S}", InputHub.SpecialLabel)
                          .Replace("{P}", InputHub.CaptureLabel);
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
