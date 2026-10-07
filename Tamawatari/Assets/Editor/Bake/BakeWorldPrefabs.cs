using System.Collections.Generic;
using UnityEngine;

namespace Tamawatari.Bake
{
    /// <summary>
    /// ワールドに出るものを <c>Assets/Prefabs</c> のプレハブに焼く。
    /// 灯篭・プレイヤー・お化け・魂・衝撃波・灯の粉・ふわっと消えるお化け・音のリグ。
    ///
    /// もともとランタイムで SpriteRenderer を組み合わせて作っていた形をそのまま持ってきたもの。
    /// 形を変えたいときはここを直して焼き直す。
    /// </summary>
    public static class BakeWorldPrefabs
    {
        public const string Folder = "Assets/Prefabs";

        public struct Result
        {
            public GameObject Lantern, Player, Enemy, Soul, Shockwave, Mote, GhostPoof, AudioRig;
        }

        private static BakeSprites.Result S => UiBuild.Sprites;

        public static Result Run()
        {
            BakeUtil.EnsureFolder(Folder);
            return new Result
            {
                Lantern = Lantern(),
                Player = Player(),
                Enemy = Enemy(),
                Soul = Soul(),
                Shockwave = Shockwave(),
                Mote = Mote(),
                GhostPoof = GhostPoofPrefab(),
                AudioRig = AudioRigPrefab(),
            };
        }

        // ==================== 部品 ====================
        private static GameObject Circle(string name, Color color, float diameter, int order)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S.Circle;
            sr.color = color;
            sr.sortingOrder = order;
            go.transform.localScale = Vector3.one * Mathf.Max(0.01f, diameter);
            return go;
        }

        private static Transform AddChild(Transform parent, Sprite sprite, Vector2 localPos, float localDia,
            Color c, int order, string name = "part", float zRotDeg = 0f, float aspectY = 1f)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = c;
            sr.sortingOrder = order;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
            go.transform.localScale = new Vector3(localDia, localDia * aspectY, 1f);
            if (Mathf.Abs(zRotDeg) > 0.01f) go.transform.localRotation = Quaternion.Euler(0f, 0f, zRotDeg);
            return go.transform;
        }

        private static Transform AddCircle(Transform p, Vector2 pos, float dia, Color c, int order,
            string name = "part", float zRot = 0f, float aspectY = 1f)
            => AddChild(p, S.Circle, pos, dia, c, order, name, zRot, aspectY);

        private static Transform AddGlow(Transform p, float dia, Color c, int order, string name = "glow")
            => AddChild(p, S.Glow, Vector2.zero, dia, c, order, name);

        /// <summary>基準色から作った色を、実行時に塗り替えられるよう控えておく入れ物。</summary>
        private class TintRecorder
        {
            private readonly List<TintedParts.Part> _parts = new List<TintedParts.Part>();
            private readonly float _tintAlpha;

            public TintRecorder(Color bakeTint) => _tintAlpha = Mathf.Max(0.0001f, bakeTint.a);

            /// <summary>tone は基準色を白へ寄せる量(0 なら基準色そのまま)。</summary>
            public void Add(Transform t, float alpha, float tone = 0f)
            {
                _parts.Add(new TintedParts.Part
                {
                    renderer = t.GetComponent<SpriteRenderer>(),
                    alphaScale = alpha / _tintAlpha,
                    tone = tone,
                });
            }

            public void Attach(GameObject root)
                => root.AddComponent<TintedParts>().Parts = _parts.ToArray();
        }

        // ==================== 灯篭 ====================
        private static GameObject Lantern()
        {
            // 本道の灯篭を基準に焼く。色と描画順は LanternPlatform.Setup が実行時に上書きする。
            Color stone = new Color(0.36f, 0.26f, 0.30f);
            Color core = new Color(1f, 0.70f, 0.30f);
            Color glowCol = GameArt.WithAlpha(core, 0.5f);

            var go = Circle("Lantern", stone, 1f, 0);
            var t = go.transform;

            var glow = AddGlow(t, 3.4f, glowCol, -2);
            var coreT = AddCircle(t, Vector2.zero, 0.78f, core, 1, "core");
            var wick = AddCircle(t, new Vector2(0f, 0.04f), 0.30f, GameArt.WithAlpha(Color.white, 0.85f), 2, "wick");

            // 縁のリング:接地判定の境界を見た目でそのまま示す
            Color rimCol = GameArt.WithAlpha(Color.Lerp(core, Color.white, 0.35f), 0.95f);
            var rim = AddChild(t, S.Ring, Vector2.zero, 1f, rimCol, 3, "rim");

            // 大灯籠だけがまとう外側の輪(通常は消しておく)
            var halo = AddChild(t, S.Ring, Vector2.zero, 1.28f,
                                GameArt.WithAlpha(Color.Lerp(core, Color.white, 0.6f), 0.9f), 4, "halo");
            halo.gameObject.SetActive(false);

            // 「乗れない」ときだけ出す ✕
            Color x = new Color(0.95f, 0.35f, 0.35f, 0.9f);
            var crossA = AddCircle(t, Vector2.zero, 0.62f, x, 4, "crossA", 45f, 0.16f);
            var crossB = AddCircle(t, Vector2.zero, 0.62f, x, 4, "crossB", -45f, 0.16f);
            crossA.gameObject.SetActive(false);
            crossB.gameObject.SetActive(false);

            var lp = go.AddComponent<LanternPlatform>();
            lp.BindPartsForBake(go.GetComponent<SpriteRenderer>(), glow.GetComponent<SpriteRenderer>(),
                                coreT.GetComponent<SpriteRenderer>(), wick.GetComponent<SpriteRenderer>(),
                                rim.GetComponent<SpriteRenderer>(), halo.GetComponent<SpriteRenderer>(),
                                crossA.gameObject, crossB.gameObject);

            return BakeUtil.SavePrefab(go, $"{Folder}/Lantern.prefab");
        }

        // ==================== プレイヤー ====================
        private static GameObject Player()
        {
            Color body = new PlayerTuning().bodyColor;

            var root = new GameObject("Player");
            root.tag = "Player";

            // --- 見た目(お化けの親玉):本体 + 波打つ裾 + 王冠 + つり目 ---
            var visual = Circle("Visual", body, 1f, 10);
            visual.transform.SetParent(root.transform, false);
            var v = visual.transform;

            var rec = new TintRecorder(body);
            rec.Add(v, body.a);
            rec.Add(AddGlow(v, 2.6f, GameArt.WithAlpha(body, 0.5f), 8), 0.5f);

            Color trim = GameArt.WithAlpha(body, 1f);
            rec.Add(AddCircle(v, new Vector2(-0.33f, -0.34f), 0.46f, trim, 10, "hem0"), 1f);
            rec.Add(AddCircle(v, new Vector2(-0.10f, -0.40f), 0.46f, trim, 10, "hem1"), 1f);
            rec.Add(AddCircle(v, new Vector2(0.14f, -0.40f), 0.46f, trim, 10, "hem2"), 1f);
            rec.Add(AddCircle(v, new Vector2(0.36f, -0.32f), 0.42f, trim, 10, "hem3"), 1f);

            Color gold = new Color(1f, 0.82f, 0.25f);
            AddCircle(v, new Vector2(-0.24f, 0.44f), 0.22f, gold, 11, "crownL", 45f);
            AddCircle(v, new Vector2(0.00f, 0.52f), 0.26f, gold, 11, "crownC", 45f);
            AddCircle(v, new Vector2(0.24f, 0.44f), 0.22f, gold, 11, "crownR", 45f);

            Color eye = new Color(0.08f, 0.06f, 0.12f);
            AddCircle(v, new Vector2(-0.19f, 0.08f), 0.30f, eye, 11, "eyeL", 20f, 0.55f);
            AddCircle(v, new Vector2(0.19f, 0.08f), 0.30f, eye, 11, "eyeR", -20f, 0.55f);
            AddCircle(v, new Vector2(-0.19f, 0.10f), 0.09f, new Color(1f, 0.62f, 0.2f), 12, "pupilL");
            AddCircle(v, new Vector2(0.19f, 0.10f), 0.09f, new Color(1f, 0.62f, 0.2f), 12, "pupilR");
            AddCircle(v, new Vector2(0f, -0.16f), 0.34f, eye, 11, "mouth", 0f, 0.4f);
            rec.Attach(visual);

            // --- 当たり判定と挙動 ---
            var col = root.AddComponent<CircleCollider2D>();
            col.radius = new PlayerTuning().playerDiameter * GameArt.CircleVisualRadius;
            col.isTrigger = false;

            var pc = root.AddComponent<PlayerController>();      // RequireComponent が Rigidbody2D を足す
            pc.Visual = v;

            root.AddComponent<PlayerAnimator>();
            root.AddComponent<SoulSystem>();

            // --- 方向カーソル(矢印・振り子) ---
            var indGo = new GameObject("JumpIndicator");
            indGo.transform.SetParent(root.transform, false);
            var indicator = indGo.AddComponent<JumpIndicator>();

            var ind = new IndicatorTuning();
            var arrow = AddChild(indGo.transform, S.Arrow, Vector2.zero, ind.markerDiameter,
                                 ind.markerColor, 20, "IndicatorArrow");
            var arrowGlow = AddGlow(arrow, 1.6f, GameArt.WithAlpha(ind.markerColor, 0.5f), 18, "arrowGlow");

            indicator.BindPartsForBake(arrow, arrow.GetComponent<SpriteRenderer>(),
                                       arrowGlow.GetComponent<SpriteRenderer>());
            pc.Indicator = indicator;

            return BakeUtil.SavePrefab(root, $"{Folder}/Player.prefab");
        }

        // ==================== 人魂 ====================
        /// <summary>人魂:本体 + 尾 + コア + 霊気の光。friendly=味方は暖色 + 上に光る印。</summary>
        private static GameObject Hitodama(string name, Color tint, int order, bool friendly,
                                           out TintRecorder rec)
        {
            var go = Circle(name, tint, 1f, order);
            var t = go.transform;

            rec = new TintRecorder(tint);
            rec.Add(t, tint.a);
            rec.Add(AddGlow(t, 3.0f, GameArt.WithAlpha(tint, 0.45f), order - 2), 0.45f);

            rec.Add(AddCircle(t, new Vector2(0f, -0.44f), 0.58f, GameArt.WithAlpha(tint, tint.a * 0.95f), order, "tail0"),
                    tint.a * 0.95f);
            rec.Add(AddCircle(t, new Vector2(0f, -0.82f), 0.36f, GameArt.WithAlpha(tint, tint.a * 0.75f), order, "tail1"),
                    tint.a * 0.75f);
            rec.Add(AddCircle(t, new Vector2(0.03f, -1.12f), 0.18f, GameArt.WithAlpha(tint, tint.a * 0.5f), order, "tail2"),
                    tint.a * 0.5f);

            Color core = friendly ? new Color(1f, 0.96f, 0.72f, 0.95f) : new Color(0.9f, 0.97f, 1f, 0.9f);
            AddCircle(t, new Vector2(-0.07f, 0.10f), 0.44f, core, order + 1, "core");

            if (friendly)
            {
                AddCircle(t, new Vector2(0f, 0.5f), 0.2f, new Color(1f, 1f, 0.85f, 0.95f), order + 2, "mark");
            }
            else
            {
                Color eye = new Color(0.09f, 0.11f, 0.2f, 0.95f);
                AddCircle(t, new Vector2(-0.15f, 0.12f), 0.16f, eye, order + 2, "eyeL");
                AddCircle(t, new Vector2(0.15f, 0.12f), 0.16f, eye, order + 2, "eyeR");
            }
            return go;
        }

        private static GameObject Enemy()
        {
            var tuning = new EnemyTuning();
            var go = Hitodama("Enemy", tuning.soulColor, 8, friendly: false, out var rec);
            rec.Attach(go);

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = GameArt.CircleVisualRadius;
            col.isTrigger = true;

            go.AddComponent<EnemyController>();
            return BakeUtil.SavePrefab(go, $"{Folder}/Enemy.prefab");
        }

        private static GameObject Soul()
        {
            // 味方の魂は大きさも色も固定なので、プレハブの時点で決めておく
            var go = Hitodama("Soul", new Color(1f, 0.72f, 0.28f, 0.92f), 16, friendly: true, out var rec);
            rec.Attach(go);
            go.transform.localScale = Vector3.one * 0.34f;
            return BakeUtil.SavePrefab(go, $"{Folder}/Soul.prefab");
        }

        // ==================== エフェクト ====================
        private static GameObject Shockwave()
        {
            var root = new GameObject("Shockwave");
            var ring = AddChild(root.transform, S.Ring, Vector2.zero, 1f, Color.white, 6, "ring");
            var flash = AddChild(root.transform, S.Glow, Vector2.zero, 1f, GameArt.WithAlpha(Color.white, 0.55f), 5, "flash");

            root.AddComponent<ShockwaveEffect>()
                .BindPartsForBake(ring.GetComponent<SpriteRenderer>(), flash.GetComponent<SpriteRenderer>());

            return BakeUtil.SavePrefab(root, $"{Folder}/Shockwave.prefab");
        }

        private static GameObject Mote()
        {
            // やわらかい光の粒1つ。大きさ・色・描画順は出すときに決める
            var go = new GameObject("Mote");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S.Glow;
            sr.color = Color.white;
            sr.sortingOrder = 4;
            return BakeUtil.SavePrefab(go, $"{Folder}/Mote.prefab");
        }

        private static GameObject GhostPoofPrefab()
        {
            Color body = new Color(0.7f, 0.95f, 1f, 0.95f);

            var root = new GameObject("GhostPoof");
            var t = root.transform;
            var rec = new TintRecorder(body);

            rec.Add(AddCircle(t, Vector2.zero, 1.0f, body, 30, "body"), body.a);
            rec.Add(AddCircle(t, new Vector2(-0.28f, -0.42f), 0.44f, body, 30, "hem0"), body.a);
            rec.Add(AddCircle(t, new Vector2(0.02f, -0.44f), 0.44f, body, 30, "hem1"), body.a);
            rec.Add(AddCircle(t, new Vector2(0.32f, -0.42f), 0.44f, body, 30, "hem2"), body.a);

            AddCircle(t, new Vector2(-0.2f, 0.12f), 0.2f, new Color(0.15f, 0.15f, 0.2f, 1f), 31, "eyeL");
            AddCircle(t, new Vector2(0.2f, 0.12f), 0.2f, new Color(0.15f, 0.15f, 0.2f, 1f), 31, "eyeR");
            AddCircle(t, new Vector2(-0.34f, -0.06f), 0.16f, new Color(1f, 0.55f, 0.6f, 0.7f), 31, "blushL");
            AddCircle(t, new Vector2(0.34f, -0.06f), 0.16f, new Color(1f, 0.55f, 0.6f, 0.7f), 31, "blushR");

            rec.Attach(root);
            root.AddComponent<GhostPoof>();
            return BakeUtil.SavePrefab(root, $"{Folder}/GhostPoof.prefab");
        }

        // ==================== 音のリグ ====================
        private static GameObject AudioRigPrefab()
        {
            const int voices = 5;
            var go = new GameObject("~Audio");

            var sfx = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
            {
                sfx[i] = go.AddComponent<AudioSource>();
                sfx[i].playOnAwake = false;
            }
            var bgm = go.AddComponent<AudioSource>();
            bgm.playOnAwake = false;
            bgm.loop = true;

            go.AddComponent<AudioRig>().BindPartsForBake(sfx, bgm);
            return BakeUtil.SavePrefab(go, $"{Folder}/AudioRig.prefab");
        }
    }
}
