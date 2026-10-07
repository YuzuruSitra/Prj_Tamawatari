using UnityEngine;

/// <summary>
/// SE と BGM。音は <c>Assets/Audio</c> の AudioClip アセットで、
/// <see cref="GameAssets"/> から引いて <c>Assets/Prefabs/AudioRig.prefab</c> で鳴らす。
/// リグはシーンをまたいで残るので BGM が途切れない。
///
/// WebGL ではブラウザの自動再生制限があるため、最初の入力より後に StartBgm() が
/// 呼ばれるようになっている(タイトルで一度キーを押せば鳴りはじめる)。
/// </summary>
public static class AudioManager
{
    private static AudioRig _rig;
    private static GameAssets _a;
    private static int _voice;
    private static AudioTuning _t = new AudioTuning();
    private static bool _built;

    public static AudioTuning Tuning
    {
        get => _t;
        set
        {
            if (value != null) _t = value;
            ApplyVolumes();
        }
    }

    // ==================== リグ ====================
    private static bool EnsureBuilt()
    {
        if (_built) return _rig != null;
        _built = true;

        _a = GameAssets.I;
        var go = GameAssets.Spawn(_a != null ? _a.audioRig : null);
        if (go == null) return false;

        go.hideFlags = HideFlags.HideAndDontSave;
        Object.DontDestroyOnLoad(go);
        _rig = go.GetComponent<AudioRig>();
        if (_rig == null) return false;

        if (_rig.Bgm != null) _rig.Bgm.clip = _a.bgmLoop;
        ApplyVolumes();
        return true;
    }

    private static void ApplyVolumes()
    {
        if (_rig == null) return;
        float sv = Mathf.Clamp01(_t.masterVolume * _t.sfxVolume) * (_t.sfxEnabled ? 1f : 0f);
        var pool = _rig.Sfx;
        for (int i = 0; pool != null && i < pool.Length; i++)
            if (pool[i] != null) pool[i].volume = sv;
        if (_rig.Bgm != null)
            _rig.Bgm.volume = Mathf.Clamp01(_t.masterVolume * _t.bgmVolume) * (_t.bgmEnabled ? 1f : 0f);
    }

    // ==================== 再生 ====================
    private static void Play(AudioClip c, float pitch = 1f, float volScale = 1f)
    {
        if (!EnsureBuilt() || !_t.sfxEnabled || c == null) return;

        var pool = _rig.Sfx;
        if (pool == null || pool.Length == 0) return;

        // 鳴っている音の pitch を後から変えないよう、声を順番に使い回す
        var src = pool[_voice];
        _voice = (_voice + 1) % pool.Length;
        if (src == null) return;
        src.pitch = Mathf.Clamp(pitch, 0.3f, 3f);
        src.PlayOneShot(c, Mathf.Clamp01(volScale));
    }

    public static void StartBgm()
    {
        if (!EnsureBuilt() || !_t.bgmEnabled) return;
        if (_rig.Bgm != null && !_rig.Bgm.isPlaying) _rig.Bgm.Play();
    }

    public static void StopBgm()
    {
        if (_rig != null && _rig.Bgm != null && _rig.Bgm.isPlaying) _rig.Bgm.Stop();
    }

    public static void PlayJump(float charge01)
    {
        if (EnsureBuilt()) Play(_a.seJump, 0.85f + 0.45f * Mathf.Clamp01(charge01));
    }

    public static void PlayLand()
    {
        if (EnsureBuilt()) Play(_a.seLand);
    }

    public static void PlayPerfect(int combo)
    {
        if (EnsureBuilt()) Play(_a.sePerfect, 1f + 0.05f * Mathf.Min(combo, 8));
    }

    public static void PlayShock(float scale01)
    {
        if (EnsureBuilt()) Play(_a.seShock, 1.15f - 0.3f * Mathf.Clamp01(scale01));
    }

    public static void PlayMiss()
    {
        if (EnsureBuilt()) Play(_a.seMiss);
    }

    public static void PlayGameOver()
    {
        if (EnsureBuilt()) Play(_a.seOver, 1f, 0.9f);
    }

    public static void PlayClear()
    {
        if (EnsureBuilt()) Play(_a.seClear, 1f, 0.9f);
    }

    public static void PlayWarning()
    {
        if (EnsureBuilt()) Play(_a.seWarn);
    }

    public static void PlayMerge()
    {
        if (EnsureBuilt()) Play(_a.seMerge);
    }

    public static void PlayUi()
    {
        if (EnsureBuilt()) Play(_a.seUi, 1f, 0.7f);
    }

    public static void PlayCheckpoint()
    {
        if (EnsureBuilt()) Play(_a.seCheck, 1f, 0.95f);
    }

    /// <summary>連続キル演出:連鎖が伸びるほど音程が上がる。</summary>
    public static void PlayKill(int streak)
    {
        if (!EnsureBuilt()) return;
        float pitch = Mathf.Min(2.4f, 1f + 0.075f * Mathf.Max(0, streak - 1));
        Play(_a.seKill, pitch, Mathf.Min(1f, 0.7f + 0.03f * streak));
    }
}
