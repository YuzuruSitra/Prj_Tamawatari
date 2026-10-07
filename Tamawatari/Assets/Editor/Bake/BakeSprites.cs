using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tamawatari.Bake
{
    /// <summary>
    /// 円 / リング / 光 / ビネット / 霧 / 矢印を PNG に焼いて <c>Assets/Art/Sprites</c> に置く。
    /// もともと実行時に Texture2D を組んでいた式をそのまま画像にしたものなので、
    /// 形を変えたいときはここの式を直して焼き直す。
    /// </summary>
    public static class BakeSprites
    {
        public const string Folder = "Assets/Art/Sprites";

        /// <summary>霧の穴の大きさ。<see cref="GameArt.FogCoreT"/> と一致させること。</summary>
        private const float FogCoreT = GameArt.FogCoreT;

        public struct Result
        {
            public Sprite Circle, Ring, Glow, Vignette, Fog, Arrow;
        }

        public static Result Run()
        {
            BakeUtil.EnsureFolder(Folder);

            return new Result
            {
                // 直径 1 ワールドユニットの白い円(可視半径ちょうど 0.5)
                Circle = Radial("Circle", 128, (t, r) => (1f - t) * r),

                // 輪(衝撃波・ゲージ用)
                Ring = Radial("Ring", 128, (t, r) => Mathf.Min((1f - t) * r, (t - 0.70f) * r)),

                // 中心が明るく外へ滑らかに消える光(灯篭の明かり・霊気・粒)
                Glow = Radial("Glow", 128, (t, r) => Mathf.Pow(Mathf.Clamp01(1f - t), 2.2f)),

                // 画面四隅を落とすビネット(中心が透明、外周が不透明)
                Vignette = Radial("Vignette", 256,
                    (t, r) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.52f, 1.05f, t))),

                // 濃霧の覆い。中心に小さな穴が開いていて、外は不透明
                Fog = Radial("Fog", 256,
                    (t, r) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(FogCoreT, 0.24f, t))),

                Arrow = Arrow("Arrow", 128),
            };
        }

        /// <summary>中心からの距離だけで不透明度が決まる図形。t は 0(中心)〜1(半径)。</summary>
        private static Sprite Radial(string name, int size, System.Func<float, float, float> alphaFn)
        {
            var px = new Color32[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float r = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                    float a = Mathf.Clamp01(alphaFn(d / r, r));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            return Write(name, size, px);
        }

        /// <summary>上(+Y)を向いた矢印。方向カーソル用。</summary>
        private static Sprite Arrow(string name, int size)
        {
            var px = new Color32[size * size];
            const float headTop = 0.48f, headBase = 0.04f, headHalf = 0.36f;
            const float shaftHalf = 0.13f, shaftBottom = -0.46f;
            float aa = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size - 0.5f;
                    float v = (y + 0.5f) / size - 0.5f;
                    float a = 0f;

                    if (v >= headBase && v <= headTop)                      // 三角の頭
                    {
                        float halfW = headHalf * (headTop - v) / (headTop - headBase);
                        a = Mathf.Max(a, (halfW - Mathf.Abs(u)) * aa);
                    }
                    if (v >= shaftBottom && v <= headBase + 0.02f)          // 軸
                    {
                        a = Mathf.Max(a, Mathf.Min((shaftHalf - Mathf.Abs(u)) * aa,
                                                   (v - shaftBottom) * aa));
                    }
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            }
            return Write(name, size, px);
        }

        /// <summary>PNG に書き出して、スプライトとして読み直せるよう設定する。</summary>
        private static Sprite Write(string name, int size, Color32[] pixels)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();

            string path = $"{Folder}/{name}.png";
            File.WriteAllBytes(BakeUtil.ToDiskPath(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            // 円スプライトの直径が 1 ワールドユニットになるよう、画像の1辺 = 1 unit にする
            importer.spritePixelsPerUnit = size;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            // やわらかい階調が潰れないよう圧縮しない(どれも 256px 以下の小さな画像)
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogError($"[BakeSprites] {path} を読み込めませんでした。");
            return sprite;
        }
    }
}
