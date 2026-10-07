using UnityEngine;

/// <summary>
/// 音を鳴らす口(<c>Assets/Prefabs/AudioRig.prefab</c>)。
/// SE 用に何本かと BGM 用に1本の AudioSource を持つだけの入れ物で、
/// シーンをまたいで生き残る。中身の出し入れは <see cref="AudioManager"/> が行う。
/// </summary>
public class AudioRig : MonoBehaviour
{
    [Tooltip("SE 用。鳴っている音の pitch を後から変えないよう順番に使い回す")]
    [SerializeField] private AudioSource[] sfx;
    [SerializeField] private AudioSource bgm;

    public AudioSource[] Sfx => sfx;
    public AudioSource Bgm => bgm;

#if UNITY_EDITOR
    /// <summary>焼き直しツールから、プレハブのパーツ参照を差し込むために使う。</summary>
    public void BindPartsForBake(AudioSource[] sfxSources, AudioSource bgmSource)
    {
        sfx = sfxSources;
        bgm = bgmSource;
    }
#endif
}
