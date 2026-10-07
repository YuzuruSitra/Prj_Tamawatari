using UnityEngine;

/// <summary>
/// このプロジェクトが持っている絵・音・プレハブの一覧(カタログ)。
/// <c>Assets/Resources/GameAssets.asset</c> に1つだけ置き、実行時は <see cref="I"/> から引く。
///
/// 実体(PNG / WAV / .prefab)は <c>Assets/Editor/Bake/</c> の焼き直しツールが作る。
/// ランタイムのコードは「作る」ことをせず、ここに載っているものを Instantiate するだけ。
/// </summary>
public class GameAssets : ScriptableObject
{
    /// <summary>Resources から読むときの名前。</summary>
    public const string ResourcePath = "GameAssets";

    [Header("スプライト (Assets/Art/Sprites)")]
    public Sprite circle;
    public Sprite ring;
    public Sprite glow;
    public Sprite vignette;
    public Sprite fog;
    public Sprite arrow;

    [Header("フォント (Assets/Resources/Fonts)")]
    [Tooltip("本文用。WebGL には OS フォントが無いので、同梱したこれが唯一描画できるフォント")]
    public Font bodyFont;
    [Tooltip("見出し用。現状は本文と同じ同梱フォントを指す")]
    public Font displayFont;

    [Header("ワールドのプレハブ (Assets/Prefabs)")]
    public GameObject lantern;
    public GameObject player;
    public GameObject enemy;
    public GameObject soul;
    public GameObject shockwave;
    public GameObject mote;
    public GameObject ghostPoof;

    [Header("UI のプレハブ (Assets/Prefabs/UI)")]
    public GameObject titleCanvas;
    public GameObject hudCanvas;
    public GameObject rankingPanel;
    public GameObject virtualPad;
    public GameObject softKeyboard;

    [Header("音 (Assets/Audio)")]
    [Tooltip("AudioSource をまとめた常駐プレハブ")]
    public GameObject audioRig;
    public AudioClip seJump;
    public AudioClip seLand;
    public AudioClip sePerfect;
    public AudioClip seKill;
    public AudioClip seShock;
    public AudioClip seMiss;
    public AudioClip seOver;
    public AudioClip seClear;
    public AudioClip seWarn;
    public AudioClip seMerge;
    public AudioClip seUi;
    public AudioClip seCheck;
    public AudioClip bgmLoop;

    private static GameAssets _i;
    private static bool _warned;

    /// <summary>カタログ本体。見つからないときは null を返し、一度だけ理由をログに出す。</summary>
    public static GameAssets I
    {
        get
        {
            if (_i != null) return _i;
            _i = Resources.Load<GameAssets>(ResourcePath);
            if (_i == null && !_warned)
            {
                _warned = true;
                Debug.LogError($"[GameAssets] Assets/Resources/{ResourcePath}.asset が読み込めませんでした。"
                               + " Tamawatari > アセット > 焼き直す で作り直してください。");
            }
            return _i;
        }
    }

    /// <summary>カタログの1件を Instantiate する。欠けていても落ちないよう null を返す。</summary>
    public static GameObject Spawn(GameObject prefab, Transform parent = null)
    {
        if (prefab == null) return null;
        return parent != null ? Object.Instantiate(prefab, parent, false) : Object.Instantiate(prefab);
    }
}
