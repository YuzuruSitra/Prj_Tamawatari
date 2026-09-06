using UnityEngine;

/// <summary>
/// SE と BGM。音源ファイルを持たないモックなので、すべて手続き生成(AudioClip.Create)。
/// 常駐オブジェクトを自前で作り、シーンをまたいで BGM を鳴らし続ける。
///
/// WebGL ではブラウザの自動再生制限があるため、最初の入力より後に StartBgm() が
/// 呼ばれるようになっている(タイトルで一度キーを押せば鳴りはじめる)。
/// </summary>
public static class AudioManager
{
    private const int Rate = 44100;
    private const int BgmRate = 22050;

    private const int SfxVoices = 5;
    private static AudioSource[] _sfxPool;
    private static int _voice;
    private static AudioSource _bgm;
    private static AudioTuning _t = new AudioTuning();
    private static bool _built;

    private static AudioClip _cJump, _cLand, _cPerfect, _cKill, _cShock,
                             _cMiss, _cOver, _cClear, _cWarn, _cMerge, _cUi, _cCheck, _cBgm;

    public static AudioTuning Tuning
    {
        get => _t;
        set
        {
            if (value != null) _t = value;
            ApplyVolumes();
        }
    }

    // ==================== 生成 ====================
    private static void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        var go = new GameObject("~Audio");
        go.hideFlags = HideFlags.HideAndDontSave;
        Object.DontDestroyOnLoad(go);
        _sfxPool = new AudioSource[SfxVoices];
        for (int i = 0; i < SfxVoices; i++)
        {
            _sfxPool[i] = go.AddComponent<AudioSource>();
            _sfxPool[i].playOnAwake = false;
        }
        _bgm = go.AddComponent<AudioSource>();
        _bgm.playOnAwake = false;
        _bgm.loop = true;

        _cJump = Make("se_jump", 0.22f, t => Env(t, 0.22f, 0.004f, 2.6f) * Sine(t, Sweep(t, 0.22f, 300f, 760f)) * 0.55f);
        _cLand = Make("se_land", 0.16f, t => Env(t, 0.16f, 0.002f, 9f) * (Sine(t, 150f) * 0.8f + Noise(t) * 0.25f) * 0.7f);
        _cPerfect = Make("se_perfect", 0.42f, t => Bell(t, 0.42f, 880f) + Bell(t - 0.09f, 0.33f, 1108f) + Bell(t - 0.18f, 0.24f, 1480f));
        _cKill = Make("se_kill", 0.14f, t => Env(t, 0.14f, 0.002f, 12f) * (Sine(t, 560f) * 0.7f + Sine(t, 1120f) * 0.25f + Noise(t) * 0.15f) * 0.6f);
        _cShock = Make("se_shock", 0.34f, t => Env(t, 0.34f, 0.003f, 7f) * (Noise(t) * 0.5f + Sine(t, Sweep(t, 0.34f, 190f, 60f)) * 0.7f) * 0.7f);
        _cMiss = Make("se_miss", 0.5f, t => Env(t, 0.5f, 0.01f, 3.2f) * Sine(t, Sweep(t, 0.5f, 520f, 110f)) * 0.55f);
        _cOver = Make("se_over", 1.1f, t => Env(t, 1.1f, 0.02f, 2.2f) *
                     (Sine(t, Sweep(t, 1.1f, 220f, 82f)) * 0.6f + Sine(t, Sweep(t, 1.1f, 165f, 62f)) * 0.4f) * 0.6f);
        _cClear = Make("se_clear", 0.95f, t => Bell(t, 0.9f, 523f) + Bell(t - 0.12f, 0.8f, 659f)
                     + Bell(t - 0.24f, 0.7f, 784f) + Bell(t - 0.36f, 0.6f, 1046f));
        _cWarn = Make("se_warn", 0.42f, t =>
        {
            float a = Env(t, 0.16f, 0.004f, 6f) * Square(t, 760f);
            float b = Env(t - 0.2f, 0.16f, 0.004f, 6f) * Square(t - 0.2f, 640f);
            return (a + b) * 0.42f;
        });
        _cMerge = Make("se_merge", 0.7f, t => Env(t, 0.7f, 0.02f, 2.4f) *
                     (Sine(t, Sweep(t, 0.7f, 180f, 1200f)) * 0.55f + Noise(t) * 0.2f) * 0.7f);
        _cCheck = Make("se_check", 1.0f, t => Bell(t, 0.95f, 392f) + Bell(t - 0.08f, 0.9f, 523f)
                     + Bell(t - 0.16f, 0.85f, 659f) + Bell(t - 0.24f, 0.8f, 784f)
                     + Bell(t - 0.34f, 0.7f, 1046f));
        _cUi = Make("se_ui", 0.06f, t => Env(t, 0.06f, 0.001f, 22f) * Sine(t, 1250f) * 0.45f);

        _cBgm = BuildBgm();
        _bgm.clip = _cBgm;

        ApplyVolumes();
    }

    private static void ApplyVolumes()
    {
        if (!_built) return;
        float sv = Mathf.Clamp01(_t.masterVolume * _t.sfxVolume) * (_t.sfxEnabled ? 1f : 0f);
        for (int i = 0; i < _sfxPool.Length; i++)
            if (_sfxPool[i] != null) _sfxPool[i].volume = sv;
        _bgm.volume = Mathf.Clamp01(_t.masterVolume * _t.bgmVolume) * (_t.bgmEnabled ? 1f : 0f);
    }

    // ==================== 波形ヘルパ ====================
    private static AudioClip Make(string name, float dur, System.Func<float, float> f, int rate = Rate)
    {
        int n = Mathf.Max(1, Mathf.CeilToInt(dur * rate));
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)rate), -1f, 1f);
        var c = AudioClip.Create(name, n, 1, rate, false);
        c.SetData(data, 0);
        return c;
    }

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

    // ==================== 再生 ====================
    private static void Play(AudioClip c, float pitch = 1f, float volScale = 1f)
    {
        EnsureBuilt();
        if (!_t.sfxEnabled || c == null) return;

        // 鳴っている音の pitch を後から変えないよう、声を順番に使い回す
        var src = _sfxPool[_voice];
        _voice = (_voice + 1) % _sfxPool.Length;
        src.pitch = Mathf.Clamp(pitch, 0.3f, 3f);
        src.PlayOneShot(c, Mathf.Clamp01(volScale));
    }

    public static void StartBgm()
    {
        EnsureBuilt();
        if (!_t.bgmEnabled) return;
        if (!_bgm.isPlaying) _bgm.Play();
    }

    public static void StopBgm()
    {
        if (_built && _bgm.isPlaying) _bgm.Stop();
    }

    public static void PlayJump(float charge01) => Play(_cJump, 0.85f + 0.45f * Mathf.Clamp01(charge01));
    public static void PlayLand() => Play(_cLand);
    public static void PlayPerfect(int combo) => Play(_cPerfect, 1f + 0.05f * Mathf.Min(combo, 8));
    public static void PlayShock(float scale01) => Play(_cShock, 1.15f - 0.3f * Mathf.Clamp01(scale01));
    public static void PlayMiss() => Play(_cMiss);
    public static void PlayGameOver() => Play(_cOver, 1f, 0.9f);
    public static void PlayClear() => Play(_cClear, 1f, 0.9f);
    public static void PlayWarning() => Play(_cWarn);
    public static void PlayMerge() => Play(_cMerge);
    public static void PlayUi() => Play(_cUi, 1f, 0.7f);
    public static void PlayCheckpoint() => Play(_cCheck, 1f, 0.95f);

    /// <summary>連続キル演出:連鎖が伸びるほど音程が上がる。</summary>
    public static void PlayKill(int streak)
    {
        float pitch = Mathf.Min(2.4f, 1f + 0.075f * Mathf.Max(0, streak - 1));
        Play(_cKill, pitch, Mathf.Min(1f, 0.7f + 0.03f * streak));
    }
}
