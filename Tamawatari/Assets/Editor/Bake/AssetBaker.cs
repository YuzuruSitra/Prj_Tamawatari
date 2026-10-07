using UnityEditor;
using UnityEngine;

namespace Tamawatari.Bake
{
    /// <summary>
    /// このプロジェクトのアセット(スプライト・音・プレハブ)を作り直す入り口。
    ///
    /// 絵と音の「形を決めている式」と「画面の組み立て」はすべてこの Bake フォルダの中にあり、
    /// 実行時のコードは出来上がったアセットを Instantiate するだけになっている。
    /// 見た目や音を変えたいときは Bake 側を直して、ここから焼き直す。
    ///
    ///   メニュー   : Tamawatari > アセット > 焼き直す
    ///   コマンド   : Unity.exe -batchmode -quit -projectPath . -executeMethod Tamawatari.Bake.AssetBaker.BakeAll
    ///                (Editor でプロジェクトを開いたままだと Temp/UnityLockfile で失敗する)
    /// </summary>
    public static class AssetBaker
    {
        private const string CatalogFolder = "Assets/Resources";
        private const string CatalogPath = CatalogFolder + "/GameAssets.asset";

        /// <summary>WebGL に OS フォントは無いので、同梱したこれが唯一描画できるフォント。</summary>
        private const string FontPath = "Assets/Resources/Fonts/ShipporiMincho.ttf";

        [MenuItem("Tamawatari/アセット/焼き直す", priority = 0)]
        public static void BakeAll()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
            {
                Debug.LogError($"[AssetBaker] 同梱フォント {FontPath} が見つかりません。"
                               + " これが無いと WebGL で文字が一切描画されないため、焼き直しを中止します。");
                return;
            }

            UiBuild.Sprites = BakeSprites.Run();
            UiBuild.BodyFont = font;
            UiBuild.DisplayFont = font;

            var audio = BakeAudio.Run();
            var world = BakeWorldPrefabs.Run();
            var ui = BakeUiPrefabs.Run();

            WriteCatalog(UiBuild.Sprites, font, world, ui, audio);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AssetBaker] アセットを焼き直しました。");
        }

        /// <summary>焼いたものを1枚の ScriptableObject にまとめる。実行時はここだけを見る。</summary>
        private static void WriteCatalog(BakeSprites.Result sprites, Font font,
                                         BakeWorldPrefabs.Result world, BakeUiPrefabs.Result ui,
                                         BakeAudio.Result audio)
        {
            BakeUtil.EnsureFolder(CatalogFolder);

            var catalog = AssetDatabase.LoadAssetAtPath<GameAssets>(CatalogPath);
            bool isNew = catalog == null;
            if (isNew) catalog = ScriptableObject.CreateInstance<GameAssets>();

            catalog.circle = sprites.Circle;
            catalog.ring = sprites.Ring;
            catalog.glow = sprites.Glow;
            catalog.vignette = sprites.Vignette;
            catalog.fog = sprites.Fog;
            catalog.arrow = sprites.Arrow;

            catalog.bodyFont = font;
            catalog.displayFont = font;

            catalog.lantern = world.Lantern;
            catalog.player = world.Player;
            catalog.enemy = world.Enemy;
            catalog.soul = world.Soul;
            catalog.shockwave = world.Shockwave;
            catalog.mote = world.Mote;
            catalog.ghostPoof = world.GhostPoof;
            catalog.audioRig = world.AudioRig;

            catalog.titleCanvas = ui.TitleCanvas;
            catalog.hudCanvas = ui.HudCanvas;
            catalog.rankingPanel = ui.RankingPanel;
            catalog.virtualPad = ui.VirtualPad;
            catalog.softKeyboard = ui.SoftKeyboard;

            catalog.seJump = audio.Jump;
            catalog.seLand = audio.Land;
            catalog.sePerfect = audio.Perfect;
            catalog.seKill = audio.Kill;
            catalog.seShock = audio.Shock;
            catalog.seMiss = audio.Miss;
            catalog.seOver = audio.Over;
            catalog.seClear = audio.Clear;
            catalog.seWarn = audio.Warn;
            catalog.seMerge = audio.Merge;
            catalog.seUi = audio.Ui;
            catalog.seCheck = audio.Check;
            catalog.bgmLoop = audio.Bgm;

            if (isNew) AssetDatabase.CreateAsset(catalog, CatalogPath);
            else EditorUtility.SetDirty(catalog);
        }
    }
}
