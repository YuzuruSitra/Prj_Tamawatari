/// <summary>
/// 開発用。エディタでスマホビルドと同じ条件に揃えるためのスイッチ。
/// 中身は丸ごと UNITY_EDITOR で囲ってあるので、プレイヤービルドには一切載らない
/// (ビルドでは Enabled が常に false になり、判定はすべて実機の値だけで決まる)。
///
/// ON の間はこうなる:
///   - バーチャルパッドが出て、マウス左クリックが指の代わりになる
///   - 物理キーボード / ゲームパッドを「無かったこと」にする(SPACE では遊べない)
///   - 名前入力が OS のソフトキーボードではなく自前の OnScreenKeyboard になる(WebGL と同じ)
///   - フォントが同梱フォント固定になり、OS フォントへ落ちない(WebGL と同じ)
///
/// 切り替えは Tamawatari > 開発用 メニューから(Assets/Editor/MobileSimMenu.cs)。
/// 画面の形まで揃えたいときは Device Simulator を開く。そちらでスマホを選んでいる間は
/// メニューを触らなくても自動で ON になる。
/// </summary>
public static class MobileSim
{
#if UNITY_EDITOR
    private const string PrefKey = "Tamawatari.MobileSim";

    private static bool _forced = UnityEditor.EditorPrefs.GetBool(PrefKey, false);

    /// <summary>メニューの手動スイッチ。EditorPrefs に持つのでシーンにもビルドにも残らない。</summary>
    public static bool Forced
    {
        get => _forced;
        set
        {
            _forced = value;
            UnityEditor.EditorPrefs.SetBool(PrefKey, value);
        }
    }

    /// <summary>手動 ON か、Device Simulator でスマホを選んでいる間。</summary>
    public static bool Enabled => _forced || UnityEngine.Device.Application.isMobilePlatform;
#else
    /// <summary>ビルドでは常に false。この機能はエディタ専用。</summary>
    public static bool Enabled => false;
#endif
}
