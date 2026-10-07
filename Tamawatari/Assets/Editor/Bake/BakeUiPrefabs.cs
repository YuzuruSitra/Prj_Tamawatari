using UnityEngine;
using UnityEngine.UI;

namespace Tamawatari.Bake
{
    /// <summary>
    /// 画面(uGUI)を <c>Assets/Prefabs/UI</c> のプレハブに焼く。
    /// タイトル / HUD + リザルト / ランキング / バーチャルパッド / ソフトキーボード。
    ///
    /// もともとランタイムで組んでいた画面をそのまま持ってきたもの。
    /// レイアウトを変えたいときはここを直して焼き直す。
    /// </summary>
    public static class BakeUiPrefabs
    {
        public const string Folder = "Assets/Prefabs/UI";

        private static BakeSprites.Result S => UiBuild.Sprites;

        private static readonly Color Cream = new Color(0.94f, 0.90f, 0.83f);
        private static readonly Color Ember = new Color(1f, 0.68f, 0.28f);
        private static readonly Color Wisp = new Color(0.62f, 0.80f, 1f);
        private static readonly Color Plum = new Color(0.72f, 0.55f, 0.95f);
        private static readonly Color Dim = new Color(0.62f, 0.58f, 0.58f);

        public struct Result
        {
            public GameObject TitleCanvas, HudCanvas, RankingPanel, VirtualPad, SoftKeyboard;
        }

        public static Result Run()
        {
            BakeUtil.EnsureFolder(Folder);
            return new Result
            {
                TitleCanvas = TitleCanvas(),
                HudCanvas = HudCanvas(),
                RankingPanel = RankingPanel(),
                VirtualPad = VirtualPadPrefab(),
                SoftKeyboard = SoftKeyboard(),
            };
        }

        // ==================== タイトル ====================
        private static GameObject TitleCanvas()
        {
            var canvas = UiBuild.CreateCanvas("TitleCanvas");
            var root = canvas.transform;

            UiBuild.CreateImage(root, new Color(0.02f, 0.01f, 0.04f, 0.9f),
                Vector2.zero, Vector2.one, new Vector2(-140, -140), new Vector2(140, 140),
                "Vignette", S.Vignette);

            // 灯篭が並ぶ道
            for (int i = 0; i < 7; i++)
            {
                float x = -540 + i * 180f;
                float y = -190 + Mathf.Abs(i - 3) * 14f;
                UiBuild.CreateBox(root, new Color(1f, 0.6f, 0.22f, 0.26f), new Vector2(x, y), new Vector2(230, 230),
                    "lampGlow", sprite: S.Glow);
                UiBuild.CreateBox(root, new Color(0.2f, 0.16f, 0.23f), new Vector2(x, y), new Vector2(46, 46), "lamp", circle: true);
                UiBuild.CreateBox(root, new Color(1f, 0.72f, 0.3f), new Vector2(x, y), new Vector2(24, 24), "lampCore", circle: true);
            }

            UiBuild.CreateText(root, "T A M A W A T A R I", 68, TextAnchor.MiddleCenter,
                new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 70), new Vector2(0, 190),
                new Color(1f, 0.78f, 0.34f), display: true);
            UiBuild.CreateText(root, "灯篭の道をわたる", 24, TextAnchor.MiddleCenter,
                new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 24), new Vector2(0, 64),
                new Color(0.82f, 0.76f, 0.72f));
            InputLabel.Bind(UiBuild.CreateText(root, "", 27, TextAnchor.MiddleCenter,
                new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -166), new Vector2(0, -116),
                new Color(0.94f, 0.90f, 0.83f)), "{C} : はじめる      {S} : ランキング");

            // なまえ(ランキングに残る)
            BuildNameField(root, -46f);

            InputLabel.Bind(UiBuild.CreateText(root, "", 19, TextAnchor.LowerCenter,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 30), new Vector2(0, 92),
                new Color(0.65f, 0.62f, 0.62f)),
                "{C}: 狙う / 溜める・離してジャンプ    {S}: 溜めをキャンセルして半分の衝撃    魂3つ + {S}長押し: 統合\n" +
                "足場のど真ん中に降りると PERFECT。敵を連続で倒すと KILL 連鎖が加熱してスコアが跳ね上がる");

            return BakeUtil.SavePrefab(canvas.gameObject, $"{Folder}/TitleCanvas.prefab");
        }

        /// <summary>タイトルのなまえ入力欄。</summary>
        private static void BuildNameField(Transform parent, float centerY)
        {
            var root = UiBuild.CreateRect(parent, "NameField", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var field = root.gameObject.AddComponent<NameField>();

            UiBuild.CreateText(root, "なまえ", 24, TextAnchor.MiddleRight,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-340f, centerY - 26f), new Vector2(-200f, centerY + 26f),
                new Color(0.82f, 0.76f, 0.72f));

            var box = UiBuild.CreateRect(root, "Box", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-180f, centerY - 29f), new Vector2(340f, centerY + 29f));
            var plate = UiBuild.CreateImage(box, new Color(0.08f, 0.06f, 0.12f, 0.9f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "plate");
            var underline = UiBuild.CreateImage(box, GameArt.WithAlpha(Ember, 0.5f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 3f), "underline");

            var value = UiBuild.CreateText(box, "", 30, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(18f, 0f), new Vector2(-18f, 0f), Cream);

            var hint = UiBuild.CreateText(root, "", 18, TextAnchor.UpperCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-440f, centerY - 64f), new Vector2(440f, centerY - 34f), Dim);

            field.BindPartsForBake(box, plate, underline, value, hint);
        }

        // ==================== ランキング ====================
        private static GameObject RankingPanel()
        {
            int rows = ScoreBoard.Capacity;

            var go = new GameObject("RankingPanel", typeof(RectTransform));
            var root = (RectTransform)go.transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            UiBuild.CreateImage(root, new Color(0.02f, 0.015f, 0.04f, 0.92f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "Dim");

            var card = UiBuild.CreateImage(root, new Color(0.10f, 0.08f, 0.14f, 0.985f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-520, -300), new Vector2(520, 300), "Card");
            var c = card.transform;

            UiBuild.CreateBox(c, GameArt.WithAlpha(Ember, 0.9f), new Vector2(0, 290), new Vector2(1040, 5), "top");
            UiBuild.CreateBox(c, GameArt.WithAlpha(Ember, 0.35f), new Vector2(0, -290), new Vector2(1040, 3), "bottom");

            UiBuild.CreateText(c, "R A N K I N G", 46, TextAnchor.UpperCenter,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -86), new Vector2(0, -22), Ember, display: true);
            UiBuild.CreateBox(c, GameArt.WithAlpha(Ember, 0.4f), new Vector2(0, 198), new Vector2(280, 2), "rule");

            var left = new Text[rows];
            var right = new Text[rows];
            var rowBg = new Image[rows];
            for (int i = 0; i < rows; i++)
            {
                float y = 152f - i * 52f;
                rowBg[i] = UiBuild.CreateBox(c, new Color(1f, 1f, 1f, 0.035f), new Vector2(0, y), new Vector2(960, 44), $"row{i}");
                left[i] = UiBuild.CreateText(c, "", 20, TextAnchor.MiddleLeft,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-464, y - 22), new Vector2(250, y + 22), Cream);
                right[i] = UiBuild.CreateText(c, "", 28, TextAnchor.MiddleRight,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(250, y - 22), new Vector2(464, y + 22), Ember);
            }

            var empty = UiBuild.CreateText(c, "まだ記録がありません", 24, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300, -20), new Vector2(300, 30), Dim);

            // 案内文は開く側(タイトル / リザルト)が実行時に差し替える
            var hint = InputLabel.Bind(UiBuild.CreateText(c, "", 21, TextAnchor.LowerCenter,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 22), new Vector2(0, 58),
                new Color(0.74f, 0.7f, 0.68f)), "{S} : 閉じる      {C} : はじめる");

            // 「画像をコピー」。見出しの反対側、カード右上の空いているところに置く。
            // 1位の行(y 130..174)と上の罫線(y 290)の間に収まっていて、見出しの文字は
            // 中央寄せで x -140..140 ほどなので、どの UI にも重ならない。
            var copyBtn = UiBuild.CreateRect(c, "CopyButton",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(244, 202), new Vector2(492, 264));

            // 下から順に: ぼんやりした光 → Ember 色のカプセル(縁取り) → 一回り小さい暗いカプセル
            var copyGlow = UiBuild.CreateBox(copyBtn, GameArt.WithAlpha(Ember, 0.05f), Vector2.zero,
                new Vector2(300, 140), "glow", sprite: S.Glow);
            var copyEdge = Capsule(copyBtn, GameArt.WithAlpha(Ember, 0.55f), new Vector2(248, 62), "edge");
            var copyFace = Capsule(copyBtn, new Color(0.12f, 0.09f, 0.16f, 0.98f), new Vector2(244, 58), "face");

            var copyLabel = UiBuild.CreateText(copyBtn, "画像をコピー", 21, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-116, 2), new Vector2(116, 26), Cream, display: true);
            var copyKey = UiBuild.CreateText(copyBtn, "", 14, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-116, -24), new Vector2(116, -2), new Color(0.78f, 0.74f, 0.72f));
            InputLabel.Bind(copyKey, "{P}");

            // 撮るときだけ出す署名。隠れた案内文と同じ位置に入れ替わる
            var copyStamp = UiBuild.CreateText(c, "", 22, TextAnchor.LowerCenter,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 22), new Vector2(0, 58), Cream);
            copyStamp.gameObject.SetActive(false);

            // コピーの結果。写り込まないようカードの外(すぐ下)に出す
            var copyNote = UiBuild.CreateText(root, "", 23, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-400, -352), new Vector2(400, -308), Ember);
            copyNote.gameObject.SetActive(false);

            var view = go.AddComponent<RankingView>();
            view.BindPartsForBake(left, right, rowBg, empty, hint);
            view.SetCopyRefs(copyBtn, copyGlow, copyEdge, copyFace, copyLabel, copyKey, copyStamp, copyNote);
            return BakeUtil.SavePrefab(go, $"{Folder}/RankingPanel.prefab");
        }

        /// <summary>
        /// 箱 + 両端の円でカプセル(角丸の矩形)を作る。角丸の画像は焼いていないのでこれで代用する。
        /// 返り値の3枚は同じ色で塗り替える前提(でないと継ぎ目が見える)。
        /// </summary>
        private static Image[] Capsule(Transform parent, Color color, Vector2 size, string name)
        {
            float r = size.y;
            float span = Mathf.Max(0f, size.x - r);
            return new[]
            {
                UiBuild.CreateBox(parent, color, Vector2.zero, new Vector2(span, size.y), name),
                UiBuild.CreateBox(parent, color, new Vector2(-span * 0.5f, 0f), new Vector2(r, r), name + "L", circle: true),
                UiBuild.CreateBox(parent, color, new Vector2(span * 0.5f, 0f), new Vector2(r, r), name + "R", circle: true),
            };
        }

        // ==================== HUD + リザルト ====================
        private static GameObject HudCanvas()
        {
            var canvas = UiBuild.CreateCanvas("HUDCanvas");
            var root = canvas.transform;
            var ui = canvas.gameObject.AddComponent<UIManager>();

            UiBuild.CreateImage(root, new Color(0.02f, 0.01f, 0.04f, 0.92f),
                Vector2.zero, Vector2.one, new Vector2(-140, -140), new Vector2(140, 140),
                "Vignette", S.Vignette);

            // 濃霧の覆い。大きさは fogHoleRadius から実行時に決まる
            var fog = UiBuild.CreateBox(root, new Color(0.05f, 0.05f, 0.09f, 0f), Vector2.zero,
                new Vector2(6250, 6250), "Fog", sprite: S.Fog);
            fog.gameObject.SetActive(false);

            var warmGlow = UiBuild.CreateImage(root, new Color(1f, 0.72f, 0.28f, 0f),
                Vector2.zero, Vector2.one, new Vector2(-220, -220), new Vector2(220, 220),
                "WarmGlow", S.Vignette);
            warmGlow.gameObject.SetActive(false);

            BuildHud(root, ui, warmGlow, fog);
            BuildShiftGauge(root, ui);
            BuildBannerAndWarning(root, ui);
            BuildSectionBanner(root, ui);
            BuildResult(root, ui);

            return BakeUtil.SavePrefab(canvas.gameObject, $"{Folder}/HudCanvas.prefab");
        }

        private static void BuildHud(Transform root, UIManager ui, Image warmGlow, Image fog)
        {
            UiBuild.CreateImage(root, new Color(0.05f, 0.04f, 0.08f, 0.5f),
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -196), new Vector2(438, -12), "HudPlate");
            UiBuild.CreateImage(root, GameArt.WithAlpha(Ember, 0.85f),
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -196), new Vector2(17, -12), "HudAccent");

            var depthText = UiBuild.CreateText(root, "DEPTH  0.0 m", 40, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -74), new Vector2(430, -24), Ember, display: true);
            var killText = UiBuild.CreateText(root, "KILLS  0", 28, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -112), new Vector2(430, -72), Wisp, display: true);
            var sectionText = UiBuild.CreateText(root, "", 22, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -144), new Vector2(430, -110),
                new Color(0.86f, 0.78f, 0.62f));

            // 揺れるグラフ
            var graph = UiBuild.CreateRect(root, "HudGraph",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -186), new Vector2(426, -118));
            UiBuild.CreateImage(graph, new Color(1f, 1f, 1f, 0.10f),
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 2), "baseline");

            const int n = 18;
            var bars = new Image[n];
            for (int i = 0; i < n; i++)
            {
                var img = UiBuild.CreateBox(graph, Ember, Vector2.zero, new Vector2(13, 10), $"bar{i}");
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(11f + i * 21.5f, 2f);
                bars[i] = img;
            }

            // チャージ
            var chargeGlow = UiBuild.CreateImage(root, new Color(1f, 0.6f, 0.2f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-380, -30), new Vector2(380, 190),
                "ChargeGlow", S.Glow);
            var barBg = UiBuild.CreateImage(root, new Color(0.05f, 0.04f, 0.08f, 0.8f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-262, 46), new Vector2(262, 86), "ChargeTrack");
            var chargeFill = UiBuild.CreateImage(barBg.transform, Ember,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(5, 5), new Vector2(-5, -5), "ChargeFill").rectTransform;
            var chargeText = UiBuild.CreateText(root, "CHARGE  0.00", 21, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-300, 88), new Vector2(300, 120), Cream);

            // PERFECT ポップ(画面中央やや上)
            var perfectText = UiBuild.CreateText(root, "", 46, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420, 110), new Vector2(420, 190),
                new Color(1f, 0.88f, 0.42f, 0f), display: true);

            // 連続キルの盛り上がり(画面右手)
            var streakText = UiBuild.CreateText(root, "", 54, TextAnchor.MiddleRight,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-620, -30), new Vector2(-40, 80),
                new Color(1f, 0.7f, 0.2f, 0f), display: true);
            var streakTrack = UiBuild.CreateImage(root, new Color(1f, 1f, 1f, 0.06f),
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-300, -50), new Vector2(-40, -42), "StreakTrack");
            var streakBar = UiBuild.CreateImage(streakTrack.transform, new Color(1f, 0.6f, 0.15f, 0f),
                new Vector2(1, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, "StreakBar");

            ui.SetHudRefs(depthText, killText, chargeText, chargeFill, chargeGlow, bars);
            ui.SetPopRefs(perfectText, streakText, streakBar, warmGlow, fog);
            _sectionHudText = sectionText;
        }

        /// <summary>区間表示だけは HUD 側で作るので、幕を作るときまで持っておく。</summary>
        private static Text _sectionHudText;

        private static void BuildShiftGauge(Transform root, UIManager ui)
        {
            var g = UiBuild.CreateRect(root, "ShiftGauge",
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 34), new Vector2(228, 234));

            var gaugeGlow = UiBuild.CreateBox(g, new Color(1f, 0.75f, 0.3f, 0f), Vector2.zero, new Vector2(300, 300),
                "gaugeGlow", sprite: S.Glow);
            UiBuild.CreateBox(g, new Color(0.05f, 0.04f, 0.09f, 0.72f), Vector2.zero, new Vector2(190, 190),
                "gaugeBack", circle: true);
            var gaugeTrack = UiBuild.CreateBox(g, new Color(1f, 1f, 1f, 0.12f), Vector2.zero, new Vector2(176, 176),
                "gaugeTrack", sprite: S.Ring);

            var gaugeFill = UiBuild.CreateBox(g, Plum, Vector2.zero, new Vector2(176, 176),
                "gaugeFill", sprite: S.Ring);
            UiBuild.MakeRadial(gaugeFill);

            var gaugeCool = UiBuild.CreateBox(g, new Color(1f, 0.68f, 0.28f, 0.65f), Vector2.zero, new Vector2(126, 126),
                "gaugeCooldown", sprite: S.Ring);
            UiBuild.MakeRadial(gaugeCool);

            var pips = new Image[8];
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i] = UiBuild.CreateBox(g, new Color(1f, 1f, 1f, 0.18f), Vector2.zero,
                    new Vector2(26, 26), $"pip{i}", circle: true);
                pips[i].gameObject.SetActive(false);
            }

            var gaugeLabel = UiBuild.CreateText(g, "", 20, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-110, -128), new Vector2(110, -96), Cream);

            ui.SetGaugeRefs(gaugeGlow, gaugeFill, gaugeCool, gaugeTrack, pips, gaugeLabel);
        }

        private static void BuildBannerAndWarning(Transform root, UIManager ui)
        {
            // --- 発生中バナー ---
            var b = UiBuild.CreateRect(root, "EventBanner",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-390, -128), new Vector2(390, -20));

            var bannerPlate = UiBuild.CreateBox(b, new Color(0.08f, 0.07f, 0.14f, 0.9f), Vector2.zero, new Vector2(780, 108), "plate");
            UiBuild.CreateBox(b, new Color(1f, 1f, 1f, 0.9f), new Vector2(0, 52), new Vector2(780, 4), "line");

            var bannerName = UiBuild.CreateText(b, "", 30, TextAnchor.UpperCenter,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -46), new Vector2(0, -6), Wisp);
            var bannerDesc = UiBuild.CreateText(b, "", 20, TextAnchor.UpperCenter,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -76), new Vector2(0, -44), Cream);

            var barTrack = UiBuild.CreateBox(b, new Color(1f, 1f, 1f, 0.14f), new Vector2(0, -42), new Vector2(720, 8), "barTrack");
            var bannerBarFill = UiBuild.CreateImage(barTrack.transform, Wisp,
                new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, Vector2.zero, "barFill").rectTransform;
            b.gameObject.SetActive(false);

            // --- イベント発生2秒前の強調表示 ---
            var w = UiBuild.CreateRect(root, "EventWarning", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var warnFlash = UiBuild.CreateImage(w, new Color(1f, 1f, 1f, 0f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "flash");

            var warnBox = UiBuild.CreateRect(w, "warnBox",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520, 40), new Vector2(520, 260));

            UiBuild.CreateBox(warnBox, new Color(0.03f, 0.02f, 0.06f, 0.82f), Vector2.zero, new Vector2(1040, 220), "plate");
            var warnBandL = UiBuild.CreateBox(warnBox, Wisp, new Vector2(0, 96), new Vector2(1040, 6), "bandTop");
            var warnBandR = UiBuild.CreateBox(warnBox, Wisp, new Vector2(0, -96), new Vector2(1040, 6), "bandBottom");

            var warnLead = UiBuild.CreateText(warnBox, "!  E V E N T   I N C O M I N G  !", 28, TextAnchor.UpperCenter,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -52), new Vector2(0, -10), Cream, display: true);
            var warnName = UiBuild.CreateText(warnBox, "", 52, TextAnchor.MiddleCenter,
                new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -34), new Vector2(0, 34), Wisp);
            var warnDesc = UiBuild.CreateText(warnBox, "", 22, TextAnchor.LowerCenter,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 18), new Vector2(0, 56), Cream);
            w.gameObject.SetActive(false);

            ui.SetEventRefs(b.gameObject, bannerPlate, bannerBarFill, bannerName, bannerDesc,
                            w.gameObject, warnFlash, warnBandL, warnBandR, warnBox,
                            warnLead, warnName, warnDesc);
        }

        /// <summary>区間クリアの幕。一息つける間だけ出る。</summary>
        private static void BuildSectionBanner(Transform root, UIManager ui)
        {
            var b = UiBuild.CreateRect(root, "SectionBanner",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520, 30), new Vector2(520, 230));

            UiBuild.CreateBox(b, new Color(1f, 0.84f, 0.42f, 0.22f), Vector2.zero, new Vector2(1400, 620),
                "glow", sprite: S.Glow);
            var rule = UiBuild.CreateBox(b, GameArt.WithAlpha(Ember, 0.9f), new Vector2(0, -66), new Vector2(680, 3), "rule");

            var big = UiBuild.CreateText(b, "", 62, TextAnchor.UpperCenter,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -86), new Vector2(0, -6),
                new Color(1f, 0.9f, 0.55f), display: true);
            var sub = UiBuild.CreateText(b, "", 24, TextAnchor.UpperCenter,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -152), new Vector2(0, -96), Cream);
            var rest = UiBuild.CreateText(b, "", 21, TextAnchor.UpperCenter,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -190), new Vector2(0, -150),
                new Color(0.8f, 0.76f, 0.72f));

            b.gameObject.SetActive(false);
            ui.SetSectionRefs(_sectionHudText, rest, b.gameObject, b, big, sub, rule);
        }

        // ==================== リザルト ====================
        private static void BuildResult(Transform root, UIManager ui)
        {
            var panel = UiBuild.CreateRect(root, "ResultPanel",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UiBuild.CreateImage(panel, new Color(0.02f, 0.015f, 0.04f, 0.88f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "Dim");

            var clearCard = BuildCard(panel, true, out var clearScore, out _);
            var goCard = BuildCard(panel, false, out var goScore, out var taunt);
            panel.gameObject.SetActive(false);

            ui.SetResultRefs(panel.gameObject, clearCard, goCard, clearScore, goScore, taunt);
        }

        private static GameObject BuildCard(Transform parent, bool clear,
                                            out UIManager.ScorePanel score, out Text taunt)
        {
            taunt = null;

            var card = UiBuild.CreateImage(parent,
                clear ? new Color(0.13f, 0.09f, 0.17f, 0.985f) : new Color(0.08f, 0.07f, 0.10f, 0.985f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420, -318), new Vector2(420, 318), "Card");
            var c = card.transform;

            Color accent = clear ? Ember : new Color(0.85f, 0.72f, 0.62f);

            UiBuild.CreateBox(c, GameArt.WithAlpha(accent, 0.9f), new Vector2(0, 308), new Vector2(840, 5), "TopLine");
            UiBuild.CreateBox(c, GameArt.WithAlpha(accent, 0.35f), new Vector2(0, -308), new Vector2(840, 3), "BottomLine");

            // 月・コウモリ・クモの巣
            UiBuild.CreateBox(c, new Color(1f, 0.85f, 0.5f, clear ? 0.26f : 0.12f), new Vector2(268, 168),
                new Vector2(420, 420), "MoonGlow", sprite: S.Glow);
            UiBuild.CreateBox(c, clear ? new Color(1f, 0.94f, 0.72f) : new Color(0.6f, 0.6f, 0.55f),
                new Vector2(268, 168), new Vector2(140, 140), "Moon", circle: true);
            Bat(c, new Vector2(-262, 190), 44);
            Bat(c, new Vector2(-166, 136), 36);
            Bat(c, new Vector2(152, 220), 38);
            if (!clear) { Bat(c, new Vector2(50, 158), 42); Bat(c, new Vector2(-52, 224), 32); }
            Cobweb(c, new Vector2(-420, 318), 1f);
            if (!clear) Cobweb(c, new Vector2(420, 318), -1f);

            // 足元の灯篭の道
            for (int i = 0; i < 5; i++)
            {
                float x = -310 + i * 155f;
                float a = clear ? 1f : 0.18f;
                UiBuild.CreateBox(c, new Color(1f, 0.62f, 0.22f, 0.3f * a), new Vector2(x, -262), new Vector2(150, 150),
                    "lampGlow", sprite: S.Glow);
                UiBuild.CreateBox(c, new Color(0.22f, 0.17f, 0.24f), new Vector2(x, -262), new Vector2(32, 32), "lamp", circle: true);
                UiBuild.CreateBox(c, new Color(1f, 0.72f, 0.3f, a), new Vector2(x, -262), new Vector2(17, 17), "lampCore", circle: true);
            }

            // 見出し(オシャレに字間を空けた RESULT)
            UiBuild.CreateText(c, "R  E  S  U  L  T", 56, TextAnchor.UpperCenter,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -100), new Vector2(0, -28), accent,
                display: true);
            UiBuild.CreateBox(c, GameArt.WithAlpha(accent, 0.5f), new Vector2(0, 196), new Vector2(300, 2), "rule");
            if (clear)
                UiBuild.CreateText(c, "灯篭の道を渡りきった", 22, TextAnchor.UpperCenter,
                    new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -150), new Vector2(0, -116),
                    new Color(0.78f, 0.74f, 0.72f));

            if (clear)
            {
                Pumpkin(c, new Vector2(-296, -60), 150);
                MiniGhost(c, new Vector2(300, -60), 132, true);
            }
            else
            {
                var taunter = TauntGhost(c, new Vector2(300, -44), 176);
                UIWiggle.Attach(taunter, new Vector2(7f, 12f), 7f, 0.05f, 2.6f);

                var bubble = SpeechBubble(c, new Vector2(150, 118), new Vector2(392, 148), out taunt);
                UIWiggle.Attach(bubble, new Vector2(4f, 6f), 2.5f, 0.03f, 1.9f);

                MiniGhost(c, new Vector2(-330, -190), 84, true);
            }

            // スコア内訳(ラベル左寄せ / 数値右寄せで桁をそろえる)
            float sx0 = clear ? -300f : -374f;
            float sx1 = clear ? 300f : 56f;
            score = new UIManager.ScorePanel
            {
                Labels = UiBuild.CreateText(c, "", 23, TextAnchor.UpperLeft,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(sx0, -112), new Vector2(sx1, 32), Cream),
                Values = UiBuild.CreateText(c, "", 23, TextAnchor.UpperRight,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(sx0, -112), new Vector2(sx1, 32), Cream),
                Total = UiBuild.CreateText(c, "", 40, TextAnchor.UpperRight,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(sx0, -178), new Vector2(sx1, -124), accent, display: true),
            };
            UiBuild.CreateBox(c, GameArt.WithAlpha(accent, 0.35f),
                new Vector2((sx0 + sx1) * 0.5f, -120), new Vector2(sx1 - sx0, 2), "scoreRule");

            InputLabel.Bind(UiBuild.CreateText(c, "", 21, TextAnchor.LowerCenter,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 20), new Vector2(0, 56),
                new Color(0.72f, 0.68f, 0.66f)), "{C} : タイトルへ      {S} : ランキング");

            return card.gameObject;
        }

        // ==================== リザルトのパーツ ====================
        private static RectTransform TauntGhost(Transform parent, Vector2 c, float s)
        {
            var root = UiBuild.CreateRect(parent, "TauntGhost",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(c.x - s * 0.9f, c.y - s * 0.9f), new Vector2(c.x + s * 0.9f, c.y + s * 0.9f));
            var p = root.transform;
            Vector2 o = Vector2.zero;
            Color b = new Color(0.86f, 0.88f, 0.98f);

            UiBuild.CreateBox(p, new Color(0.6f, 0.75f, 1f, 0.26f), o, new Vector2(s * 2.1f, s * 2.1f),
                "glow", sprite: S.Glow);
            UiBuild.CreateBox(p, b, o + new Vector2(-s * 0.56f, s * 0.2f), new Vector2(s * 0.3f, s * 0.3f), "armL", circle: true);
            UiBuild.CreateBox(p, b, o + new Vector2(s * 0.56f, s * 0.2f), new Vector2(s * 0.3f, s * 0.3f), "armR", circle: true);
            UiBuild.CreateBox(p, b, o + new Vector2(0, -s * 0.28f), new Vector2(s * 0.92f, s * 0.62f), "lower");
            UiBuild.CreateBox(p, b, o + new Vector2(-s * 0.3f, -s * 0.5f), new Vector2(s * 0.38f, s * 0.38f), "h0", circle: true);
            UiBuild.CreateBox(p, b, o + new Vector2(0f, -s * 0.55f), new Vector2(s * 0.38f, s * 0.38f), "h1", circle: true);
            UiBuild.CreateBox(p, b, o + new Vector2(s * 0.3f, -s * 0.5f), new Vector2(s * 0.38f, s * 0.38f), "h2", circle: true);
            UiBuild.CreateBox(p, b, o + new Vector2(0, s * 0.08f), new Vector2(s * 0.98f, s * 0.98f), "body", circle: true);

            Color e = new Color(0.12f, 0.11f, 0.17f);
            var eL = UiBuild.CreateBox(p, e, o + new Vector2(-s * 0.19f, s * 0.16f), new Vector2(s * 0.2f, s * 0.2f), "eL", circle: true);
            eL.rectTransform.localScale = new Vector3(1f, 0.28f, 1f);
            var eR = UiBuild.CreateBox(p, e, o + new Vector2(s * 0.19f, s * 0.16f), new Vector2(s * 0.19f, s * 0.19f), "eR", circle: true);
            eR.rectTransform.localScale = new Vector3(1f, 0.55f, 1f);

            UiBuild.CreateBox(p, e, o + new Vector2(0, -s * 0.06f), new Vector2(s * 0.46f, s * 0.24f), "mouth", circle: true);
            UiBuild.CreateBox(p, new Color(1f, 0.5f, 0.58f), o + new Vector2(s * 0.1f, -s * 0.16f),
                new Vector2(s * 0.2f, s * 0.16f), "tongue", circle: true);
            UiBuild.CreateBox(p, new Color(1f, 0.55f, 0.62f, 0.65f), o + new Vector2(-s * 0.36f, s * 0.02f), new Vector2(s * 0.14f, s * 0.14f), "blL", circle: true);
            UiBuild.CreateBox(p, new Color(1f, 0.55f, 0.62f, 0.65f), o + new Vector2(s * 0.36f, s * 0.02f), new Vector2(s * 0.14f, s * 0.14f), "blR", circle: true);
            return root;
        }

        private static RectTransform SpeechBubble(Transform parent, Vector2 c, Vector2 size, out Text label)
        {
            var root = UiBuild.CreateRect(parent, "Bubble",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(c.x - size.x * 0.5f, c.y - size.y * 0.5f),
                new Vector2(c.x + size.x * 0.5f, c.y + size.y * 0.5f));
            var p = root.transform;
            Color w = new Color(0.97f, 0.96f, 0.93f);

            UiBuild.CreateBox(p, w, Vector2.zero, size, "bubble", circle: true);
            UiBuild.CreateBox(p, w, new Vector2(size.x * 0.30f, -size.y * 0.46f), new Vector2(44, 44), "tail1", circle: true);
            UiBuild.CreateBox(p, w, new Vector2(size.x * 0.40f, -size.y * 0.64f), new Vector2(24, 24), "tail2", circle: true);

            label = UiBuild.CreateText(p, "", 25, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-size.x * 0.42f, -size.y * 0.3f), new Vector2(size.x * 0.42f, size.y * 0.3f),
                new Color(0.16f, 0.13f, 0.2f));
            return root;
        }

        private static void Bat(Transform p, Vector2 c, float s)
        {
            Color k = new Color(0.03f, 0.02f, 0.05f, 0.96f);
            UiBuild.CreateBox(p, k, c, new Vector2(s * 0.44f, s * 0.44f), "batBody", circle: true);
            var wl = UiBuild.CreateBox(p, k, c + new Vector2(-s * 0.42f, s * 0.06f), new Vector2(s * 0.72f, s * 0.34f), "wingL", circle: true);
            var wr = UiBuild.CreateBox(p, k, c + new Vector2(s * 0.42f, s * 0.06f), new Vector2(s * 0.72f, s * 0.34f), "wingR", circle: true);
            wl.rectTransform.localRotation = Quaternion.Euler(0, 0, 18);
            wr.rectTransform.localRotation = Quaternion.Euler(0, 0, -18);
        }

        private static void Pumpkin(Transform p, Vector2 c, float s)
        {
            Color body = new Color(1f, 0.52f, 0.14f);
            Color glow = new Color(1f, 0.87f, 0.42f);
            Color stem = new Color(0.32f, 0.45f, 0.2f);

            UiBuild.CreateBox(p, new Color(1f, 0.55f, 0.15f, 0.3f), c, new Vector2(s * 2.2f, s * 2.2f),
                "pumpkinGlow", sprite: S.Glow);
            UiBuild.CreateBox(p, stem, c + new Vector2(0, s * 0.5f), new Vector2(s * 0.15f, s * 0.26f), "stem");
            UiBuild.CreateBox(p, body, c + new Vector2(-s * 0.24f, 0), new Vector2(s * 0.72f, s * 0.92f), "lobeL", circle: true);
            UiBuild.CreateBox(p, body, c + new Vector2(s * 0.24f, 0), new Vector2(s * 0.72f, s * 0.92f), "lobeR", circle: true);
            UiBuild.CreateBox(p, body, c, new Vector2(s * 0.98f, s * 0.94f), "lobeC", circle: true);

            var eL = UiBuild.CreateBox(p, glow, c + new Vector2(-s * 0.2f, s * 0.12f), new Vector2(s * 0.2f, s * 0.2f), "eL");
            var eR = UiBuild.CreateBox(p, glow, c + new Vector2(s * 0.2f, s * 0.12f), new Vector2(s * 0.2f, s * 0.2f), "eR");
            eL.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            eR.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);

            UiBuild.CreateBox(p, glow, c + new Vector2(0, -s * 0.17f), new Vector2(s * 0.62f, s * 0.16f), "mouth");
            UiBuild.CreateBox(p, body, c + new Vector2(-s * 0.13f, -s * 0.13f), new Vector2(s * 0.12f, s * 0.2f), "t1");
            UiBuild.CreateBox(p, body, c + new Vector2(s * 0.13f, -s * 0.13f), new Vector2(s * 0.12f, s * 0.2f), "t2");
        }

        private static void MiniGhost(Transform p, Vector2 c, float s, bool happy)
        {
            Color b = happy ? new Color(1f, 0.97f, 0.88f) : new Color(0.66f, 0.70f, 0.82f);
            UiBuild.CreateBox(p, GameArt.WithAlpha(b, 0.24f), c, new Vector2(s * 2.1f, s * 2.1f),
                "ghostGlow", sprite: S.Glow);
            UiBuild.CreateBox(p, b, c + new Vector2(0, -s * 0.28f), new Vector2(s * 0.92f, s * 0.62f), "lower");
            UiBuild.CreateBox(p, b, c + new Vector2(-s * 0.3f, -s * 0.5f), new Vector2(s * 0.38f, s * 0.38f), "h0", circle: true);
            UiBuild.CreateBox(p, b, c + new Vector2(0f, -s * 0.55f), new Vector2(s * 0.38f, s * 0.38f), "h1", circle: true);
            UiBuild.CreateBox(p, b, c + new Vector2(s * 0.3f, -s * 0.5f), new Vector2(s * 0.38f, s * 0.38f), "h2", circle: true);
            UiBuild.CreateBox(p, b, c + new Vector2(0, s * 0.08f), new Vector2(s * 0.98f, s * 0.98f), "body", circle: true);

            Color e = new Color(0.12f, 0.11f, 0.17f);
            var eL = UiBuild.CreateBox(p, e, c + new Vector2(-s * 0.18f, s * 0.14f), new Vector2(s * 0.16f, s * 0.16f), "eL", circle: true);
            var eR = UiBuild.CreateBox(p, e, c + new Vector2(s * 0.18f, s * 0.14f), new Vector2(s * 0.16f, s * 0.16f), "eR", circle: true);
            if (happy)
            {
                eL.rectTransform.localScale = new Vector3(1f, 0.5f, 1f);
                eR.rectTransform.localScale = new Vector3(1f, 0.5f, 1f);
            }
            UiBuild.CreateBox(p, new Color(1f, 0.55f, 0.62f, 0.7f), c + new Vector2(-s * 0.34f, 0f), new Vector2(s * 0.13f, s * 0.13f), "blL", circle: true);
            UiBuild.CreateBox(p, new Color(1f, 0.55f, 0.62f, 0.7f), c + new Vector2(s * 0.34f, 0f), new Vector2(s * 0.13f, s * 0.13f), "blR", circle: true);
        }

        private static void Cobweb(Transform p, Vector2 corner, float sign)
        {
            Color w = new Color(1f, 0.95f, 0.9f, 0.16f);
            for (int i = 0; i < 3; i++)
            {
                var line = UiBuild.CreateBox(p, w, corner + new Vector2(sign * (70f + i * 12f), -(70f + i * 12f)),
                    new Vector2(170f, 3f), "web");
                line.rectTransform.localRotation = Quaternion.Euler(0, 0, sign * (18f + i * 26f));
            }
            for (int i = 1; i <= 2; i++)
            {
                var arc = UiBuild.CreateBox(p, w, corner + new Vector2(sign * 44f * i, -44f * i),
                    new Vector2(78f, 3f), "webArc");
                arc.rectTransform.localRotation = Quaternion.Euler(0, 0, sign * 45f);
            }
        }

        // ==================== バーチャルパッド ====================
        private static GameObject VirtualPadPrefab()
        {
            var t = new TouchTuning();
            float d = Mathf.Max(90f, t.buttonDiameter);
            Vector2 m = t.buttonMargin;
            Color c = t.buttonColor;

            var canvas = UiBuild.CreateCanvas("VirtualPad", 400);
            var pad = canvas.gameObject.AddComponent<VirtualPad>();

            var visual = UiBuild.CreateRect(canvas.transform, "Pad", Vector2.zero, Vector2.one,
                                            Vector2.zero, Vector2.zero);
            var v = visual.transform;

            var button = UiBuild.CreateRect(v, "ShiftButton", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-m.x - d, m.y), new Vector2(-m.x, m.y + d));

            var glow = UiBuild.CreateBox(button, GameArt.WithAlpha(c, 0f), Vector2.zero,
                new Vector2(d * 1.9f, d * 1.9f), "glow", sprite: S.Glow);
            var back = UiBuild.CreateBox(button, new Color(0.06f, 0.05f, 0.1f, 0.62f), Vector2.zero,
                new Vector2(d, d), "back", circle: true);
            var ring = UiBuild.CreateBox(button, GameArt.WithAlpha(c, 0.85f), Vector2.zero,
                new Vector2(d, d), "ring", sprite: S.Ring);

            var label = UiBuild.CreateText(button, "SHIFT", Mathf.RoundToInt(d * 0.22f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-d * 0.5f, -d * 0.04f), new Vector2(d * 0.5f, d * 0.24f),
                new Color(0.98f, 0.95f, 1f), display: true);
            var sub = UiBuild.CreateText(button, "衝撃 / 統合", Mathf.RoundToInt(d * 0.11f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-d * 0.5f, -d * 0.26f), new Vector2(d * 0.5f, -d * 0.04f),
                new Color(0.86f, 0.82f, 0.92f));

            var tapHint = UiBuild.CreateText(v, t.tapHint, 22, TextAnchor.MiddleRight,
                new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-m.x - d - 520f, m.y + d * 0.34f),
                new Vector2(-m.x - d - 24f, m.y + d * 0.66f),
                new Color(0.82f, 0.78f, 0.76f, 0.7f));

            // 開発用。スマホ環境をまねている間だけ出す目印(ビルドでは出ない)
            var badge = UiBuild.CreateText(v, "スマホ環境をまねています (開発用)", 20, TextAnchor.UpperRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-620f, -50f), new Vector2(-28f, -16f),
                new Color(1f, 0.72f, 0.35f, 0.8f));
            badge.gameObject.SetActive(false);

            pad.BindPartsForBake(visual.gameObject, button, glow, back, ring, label, sub, tapHint, badge);
            return BakeUtil.SavePrefab(canvas.gameObject, $"{Folder}/VirtualPad.prefab");
        }

        // ==================== ソフトキーボード ====================
        private static GameObject SoftKeyboard()
        {
            const int cols = 10;
            const int rows = 5;
            Color keyFace = new Color(0.16f, 0.13f, 0.2f, 0.96f);

            var canvas = UiBuild.CreateCanvas("SoftKeyboard", 500);
            var kb = canvas.gameObject.AddComponent<OnScreenKeyboard>();

            UiBuild.CreateImage(canvas.transform, new Color(0.02f, 0.015f, 0.04f, 0.82f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "Dim");

            var panel = UiBuild.CreateRect(canvas.transform, "Panel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-500f, 20f), new Vector2(500f, 660f));
            UiBuild.CreateImage(panel, new Color(0.09f, 0.07f, 0.13f, 0.985f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "Plate");
            UiBuild.CreateBox(panel, GameArt.WithAlpha(Ember, 0.85f), new Vector2(0f, 318f),
                new Vector2(1000f, 4f), "topRule");

            UiBuild.CreateBox(panel, new Color(0.03f, 0.02f, 0.05f, 0.92f), new Vector2(0f, 270f),
                new Vector2(950f, 72f), "previewPlate");
            var preview = UiBuild.CreateText(panel, "", 40, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-466f, 234f), new Vector2(466f, 306f), Cream);

            var charKeys = new OnScreenKeyboard.Key[rows * cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    charKeys[r * cols + c] = MakeKey(panel, keyFace,
                        new Vector2(-441f + c * 98f, 184f - r * 84f), new Vector2(90f, 76f),
                        "", OnScreenKeyboard.KeyKind.Char, 34);

            var fs = new Vector2(155f, 84f);
            var funcKeys = new[]
            {
                MakeKey(panel, keyFace, new Vector2(-408f, -244f), fs, "゛", OnScreenKeyboard.KeyKind.Dakuten, 34),
                MakeKey(panel, keyFace, new Vector2(-245f, -244f), fs, "゜", OnScreenKeyboard.KeyKind.Handakuten, 34),
                MakeKey(panel, keyFace, new Vector2(-82f, -244f), fs, "小", OnScreenKeyboard.KeyKind.Small, 30),
                MakeKey(panel, keyFace, new Vector2(81f, -244f), fs, "ABC", OnScreenKeyboard.KeyKind.TogglePage, 28),
                MakeKey(panel, keyFace, new Vector2(244f, -244f), fs, "けす", OnScreenKeyboard.KeyKind.Backspace, 28),
                MakeKey(panel, new Color(0.44f, 0.27f, 0.1f, 0.96f), new Vector2(407f, -244f), fs,
                        "決定", OnScreenKeyboard.KeyKind.Commit, 30),
            };

            kb.BindPartsForBake(panel, preview, charKeys, funcKeys);
            return BakeUtil.SavePrefab(canvas.gameObject, $"{Folder}/SoftKeyboard.prefab");
        }

        private static OnScreenKeyboard.Key MakeKey(Transform panel, Color face, Vector2 center, Vector2 size,
                                                    string label, OnScreenKeyboard.KeyKind kind, int fontSize)
        {
            var bg = UiBuild.CreateBox(panel, face, center, size, "key");
            var rt = bg.rectTransform;
            var txt = UiBuild.CreateText(rt, label, fontSize, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Cream);
            return new OnScreenKeyboard.Key { Rt = rt, Bg = bg, Label = txt, Ch = label, Kind = kind };
        }
    }
}
