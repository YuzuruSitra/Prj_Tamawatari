using UnityEditor;
using UnityEngine;

/// <summary>
/// 開発用メニュー。Editor フォルダに置いてあるのでプレイヤービルドには含まれない。
///
///   Tamawatari > 開発用 > スマホ環境をまねる      : MobileSim の ON/OFF(チェック付き)
///   Tamawatari > 開発用 > Device Simulator を開く : 画面の形・解像度まで実機に寄せたいとき
///
/// スイッチは EditorPrefs に持つので、シーンにもプロジェクト設定にも残らない。
/// </summary>
public static class MobileSimMenu
{
    private const string SimPath = "Tamawatari/開発用/スマホ環境をまねる";
    private const string DevicePath = "Tamawatari/開発用/Device Simulator を開く";

    [InitializeOnLoadMethod]
    private static void SyncCheckMark()
    {
        EditorApplication.delayCall += () => Menu.SetChecked(SimPath, MobileSim.Forced);
    }

    [MenuItem(SimPath, priority = 100)]
    private static void ToggleSim()
    {
        MobileSim.Forced = !MobileSim.Forced;
        Menu.SetChecked(SimPath, MobileSim.Forced);
        Debug.Log(MobileSim.Forced
            ? "[MobileSim] スマホ環境のまね: ON  (タッチ + バーチャルパッドのみ。マウス左クリックが指の代わり)"
            : "[MobileSim] スマホ環境のまね: OFF (キーボード / パッドが戻る)");
    }

    [MenuItem(SimPath, true)]
    private static bool ToggleSimValidate()
    {
        Menu.SetChecked(SimPath, MobileSim.Forced);
        return true;
    }

    [MenuItem(DevicePath, priority = 101)]
    private static void OpenDeviceSimulator()
    {
        if (!EditorApplication.ExecuteMenuItem("Window/General/Device Simulator"))
            Debug.LogWarning("[MobileSim] Device Simulator を開けなかった。Window メニューから探すこと。");
    }
}
