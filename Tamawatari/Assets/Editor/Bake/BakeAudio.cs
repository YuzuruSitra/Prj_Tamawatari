using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tamawatari.Bake
{
    /// <summary>
    /// 効果音と BGM を波形合成して WAV に焼き、<c>Assets/Audio</c> に置く。
    /// もともと実行時に AudioClip.Create していた式をそのまま音源ファイルにしたもの。
    /// 音を足すときも、ここに1行足して焼き直す。
    ///
    /// <b>手で置いた音源は焼き直さない。</b>ここが書くのは <c>&lt;名前&gt;.wav</c> だけなので、
    /// <c>Assets/Audio</c> に同じ名前の <c>.mp3</c> / <c>.ogg</c> などが置かれていたら
    /// 「差し替えられた」と見なしてそれをそのまま使い、wav は書かない。
    /// つまり本物の音を使いたいときは、合成音と同じ名前で wav 以外の拡張子で置けばよい。
    /// 逆に式を直して合成音に戻したいときは、置いた差し替えファイルを消してから焼き直す。
    /// </summary>
    public static class BakeAudio
    {
        public const string Folder = "Assets/Audio";

        private const int Rate = 44100;
        private const int BgmRate = 22050;

        public struct Result
        {
            public AudioClip Jump, Land, Perfect, Kill, Shock, Miss, Over, Clear, Warn, Merge, Ui, Check, Bgm;
        }

        public static Result Run()
        {
            BakeUtil.EnsureFolder(Folder);
            _placed = ScanPlaced();

            return new Result
            {
                Jump = Make("se_jump", 0.22f, t => Env(t, 0.22f, 0.004f, 2.6f) * Sine(t, Sweep(t, 0.22f, 300f, 760f)) * 0.55f),
                Land = Make("se_land", 0.16f, t => Env(t, 0.16f, 0.002f, 9f) * (Sine(t, 150f) * 0.8f + Noise(t) * 0.25f) * 0.7f),
                Perfect = Make("se_perfect", 0.42f, t => Bell(t, 0.42f, 880f) + Bell(t - 0.09f, 0.33f, 1108f) + Bell(t - 0.18f, 0.24f, 1480f)),
                Kill = Make("se_kill", 0.14f, t => Env(t, 0.14f, 0.002f, 12f) * (Sine(t, 560f) * 0.7f + Sine(t, 1120f) * 0.25f + Noise(t) * 0.15f) * 0.6f),
                Shock = Make("se_shock", 0.34f, t => Env(t, 0.34f, 0.003f, 7f) * (Noise(t) * 0.5f + Sine(t, Sweep(t, 0.34f, 190f, 60f)) * 0.7f) * 0.7f),
                Miss = Make("se_miss", 0.5f, t => Env(t, 0.5f, 0.01f, 3.2f) * Sine(t, Sweep(t, 0.5f, 520f, 110f)) * 0.55f),
                Over = Make("se_over", 1.1f, t => Env(t, 1.1f, 0.02f, 2.2f) *
                            (Sine(t, Sweep(t, 1.1f, 220f, 82f)) * 0.6f + Sine(t, Sweep(t, 1.1f, 165f, 62f)) * 0.4f) * 0.6f),
                Clear = Make("se_clear", 0.95f, t => Bell(t, 0.9f, 523f) + Bell(t - 0.12f, 0.8f, 659f)
                            + Bell(t - 0.24f, 0.7f, 784f) + Bell(t - 0.36f, 0.6f, 1046f)),
                Warn = Make("se_warn", 0.42f, t =>
                {
                    float a = Env(t, 0.16f, 0.004f, 6f) * Square(t, 760f);
                    float b = Env(t - 0.2f, 0.16f, 0.004f, 6f) * Square(t - 0.2f, 640f);
                    return (a + b) * 0.42f;
                }),
                Merge = Make("se_merge", 0.7f, t => Env(t, 0.7f, 0.02f, 2.4f) *
                            (Sine(t, Sweep(t, 0.7f, 180f, 1200f)) * 0.55f + Noise(t) * 0.2f) * 0.7f),
                Check = Make("se_check", 1.0f, t => Bell(t, 0.95f, 392f) + Bell(t - 0.08f, 0.9f, 523f)
                            + Bell(t - 0.16f, 0.85f, 659f) + Bell(t - 0.24f, 0.8f, 784f)
                            + Bell(t - 0.34f, 0.7f, 1046f)),
                Ui = Make("se_ui", 0.06f, t => Env(t, 0.06f, 0.001f, 22f) * Sine(t, 1250f) * 0.45f),
                Bgm = BuildBgm(),
            };
        }

        // ==================== 波形ヘルパ ====================
        private static float Sine(float t, float hz) => t < 0f ? 0f : Mathf.Sin(Mathf.PI * 2f * hz * t);
        private static float Square(float t, float hz) => t < 0f ? 0f : (Mathf.Sin(Mathf.PI * 2f * hz * t) >= 0f ? 0.6f : -0.6f);
        private static float Noise(float t) => t < 0f ? 0f : (Mathf.PerlinNoise(t * 5200f, 0.3f) - 0.5f) * 2f;

        /// <summary>アタック + 指数減衰のエンベロープ。</summary>
        private static float Env(float t, float dur, float attack, float decay)
        {
            if (t < 0f || t > dur) return 0f;
            float a = attack <= 0f ? 1f : Mathf.Clamp01(t / attack);
            return a * Mathf.Exp(-decay * t);
        }

        private static float Sweep(float t, float dur, float from, float to)
            => Mathf.Lerp(from, to, Mathf.Clamp01(t / Mathf.Max(0.0001f, dur)));

        /// <summary>鐘っぽい音(基音 + 倍音)。</summary>
        private static float Bell(float t, float dur, float hz)
        {
            if (t < 0f) return 0f;
            float e = Env(t, dur, 0.003f, 4.5f);
            return e * (Sine(t, hz) * 0.5f + Sine(t, hz * 2.01f) * 0.2f + Sine(t, hz * 3.02f) * 0.08f) * 0.7f;
        }

        /// <summary>ハロウィンらしい短調のループ BGM。低いドローン + 分散和音 + かすかな風。</summary>
        private static AudioClip BuildBgm()
        {
            const float bpm = 84f;
            float beat = 60f / bpm;
            float step = beat * 0.5f;               // 8分音符
            const int steps = 48;                   // 6小節
            float dur = step * steps;

            // A ハーモニックマイナー風の分散和音
            float[] seq = { 220f, 261.63f, 329.63f, 415.30f, 329.63f, 261.63f, 220f, 196.00f };

            return Make("bgm_loop", dur, t =>
            {
                // 低いドローン
                float v = Sine(t, 55f) * 0.10f + Sine(t, 82.41f) * 0.05f + Sine(t, 55.3f) * 0.05f;

                // 分散和音(1音ずつ減衰)
                int s = (int)(t / step);
                float lt = t - s * step;
                float hz = seq[s % seq.Length];
                float e = Env(lt, step, 0.006f, 5.5f);
                v += e * (Sine(lt, hz) * 0.16f + Sine(lt, hz * 2f) * 0.05f);

                // 2小節ごとの高い残響
                int bar = s / 8;
                if (bar % 2 == 1)
                {
                    float bt = t - bar * step * 8f;
                    v += Env(bt, step * 4f, 0.05f, 1.4f) * Sine(bt, 1318.5f) * 0.045f;
                }

                // かすかな風
                v += Noise(t * 0.02f) * 0.012f;

                // 端をなめらかにしてループの継ぎ目を隠す
                float fade = Mathf.Min(1f, Mathf.Min(t, dur - t) / 0.35f);
                return v * fade;
            }, BgmRate);
        }

        // ==================== 手で置いた音源 ====================

        /// <summary>名前 → 手で置かれた音源。<see cref="Run"/> の頭で作る。</summary>
        private static Dictionary<string, AudioClip> _placed = new Dictionary<string, AudioClip>();

        /// <summary>
        /// <c>Assets/Audio</c> にある wav 以外の AudioClip を名前で引けるようにする。
        /// wav はこの焼き直しツールの出力なので、差し替えとは見なさない。
        /// </summary>
        private static Dictionary<string, AudioClip> ScanPlaced()
        {
            var map = new Dictionary<string, AudioClip>();

            var paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Folder }))
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            paths.Sort(System.StringComparer.Ordinal);   // 同じ名前が複数あっても結果が揺れないように

            foreach (string path in paths)
            {
                if (string.Equals(Path.GetExtension(path), ".wav", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                string name = Path.GetFileNameWithoutExtension(path);
                if (map.ContainsKey(name)) continue;

                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) continue;

                map[name] = clip;
                Debug.Log($"[BakeAudio] {path} が置かれているので、{name} は焼き直さずこれを使います。");
            }
            return map;
        }

        // ==================== 書き出し ====================

        /// <summary>
        /// 波形を作って <c>&lt;name&gt;.wav</c> に書き出す。ただし同じ名前の音源が
        /// 手で置かれていれば、何も書かずにそれを返す。
        /// </summary>
        private static AudioClip Make(string name, float dur, System.Func<float, float> f, int rate = Rate)
        {
            if (_placed != null && _placed.TryGetValue(name, out var placed) && placed != null)
                return placed;

            int n = Mathf.Max(1, Mathf.CeilToInt(dur * rate));
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)rate), -1f, 1f);

            string path = $"{Folder}/{name}.wav";
            File.WriteAllBytes(BakeUtil.ToDiskPath(path), EncodeWav(data, rate));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer != null)
            {
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) Debug.LogError($"[BakeAudio] {path} を読み込めませんでした。");
            return clip;
        }

        /// <summary>16bit PCM モノラルの WAV(RIFF)にする。</summary>
        private static byte[] EncodeWav(float[] samples, int rate)
        {
            const int channels = 1;
            const int bits = 16;
            int dataBytes = samples.Length * channels * (bits / 8);

            using var stream = new MemoryStream(44 + dataBytes);
            using var w = new BinaryWriter(stream);

            w.Write(new[] { 'R', 'I', 'F', 'F' });
            w.Write(36 + dataBytes);
            w.Write(new[] { 'W', 'A', 'V', 'E' });

            w.Write(new[] { 'f', 'm', 't', ' ' });
            w.Write(16);                                  // fmt チャンクの長さ
            w.Write((short)1);                            // PCM
            w.Write((short)channels);
            w.Write(rate);
            w.Write(rate * channels * (bits / 8));        // byte rate
            w.Write((short)(channels * (bits / 8)));      // block align
            w.Write((short)bits);

            w.Write(new[] { 'd', 'a', 't', 'a' });
            w.Write(dataBytes);
            foreach (float s in samples)
                w.Write((short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue));

            w.Flush();
            return stream.ToArray();
        }
    }
}
