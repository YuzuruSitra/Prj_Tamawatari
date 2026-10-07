using UnityEngine;

/// <summary>
/// プレイヤー周囲(前後左右ランダム)から一定間隔でおばけを出現させる(完全2D)。
/// 実体は <c>Assets/Prefabs/Enemy.prefab</c>。大きさと色だけ EnemyTuning から流し込む。
/// 時間経過で出現間隔が短く・移動速度が速くなる(難易度上昇)。挙動は EnemyTuning で調整。
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyTuning tuning = new EnemyTuning();

    [Header("Refs")]
    [SerializeField] private Transform player;

    private float _timer;

    public EnemyTuning Tuning { get => tuning; set => tuning = value; }
    public Transform Player { get => player; set => player = value; }

    /// <summary>false の間は湧かない(磁気嵐などのイベント用)。</summary>
    public bool SpawnEnabled { get; set; } = true;

    /// <summary>出現間隔の倍率。小さいほど大量に湧く(イベント用)。</summary>
    public float SpawnIntervalScale { get; set; } = 1f;

    /// <summary>区間が進むごとにかかる難易度倍率(イベントとは別に掛け算)。</summary>
    public float SectionIntervalScale { get; set; } = 1f;
    public float SectionSpeedScale { get; set; } = 1f;

    /// <summary>一気に count 体湧かせる。</summary>
    public void SpawnBurst(int count)
    {
        if (player == null) return;
        float sp = Mathf.Lerp(tuning.baseEnemySpeed, tuning.maxEnemySpeed, Difficulty01()) * SectionSpeedScale;
        for (int i = 0; i < count; i++) Spawn(sp);
    }

    private void Start() => _timer = tuning.firstSpawnDelay;

    private void Update()
    {
        if (player == null || !SpawnEnabled) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        float f = Difficulty01();
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            Spawn(Mathf.Lerp(tuning.baseEnemySpeed, tuning.maxEnemySpeed, f) * SectionSpeedScale);
            _timer = Mathf.Lerp(tuning.baseSpawnInterval, tuning.minSpawnInterval, f)
                     * Mathf.Max(0.05f, SpawnIntervalScale)
                     * Mathf.Max(0.05f, SectionIntervalScale);
        }
    }

    private float Difficulty01()
    {
        float t = GameManager.Instance != null ? GameManager.Instance.SurvivalTime : 0f;
        return Mathf.Clamp01(t / Mathf.Max(0.0001f, tuning.difficultyRampTime));
    }

    private void Spawn(float speed)
    {
        float ang = Random.Range(0f, Mathf.PI * 2f);
        Vector3 dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
        Vector3 pos = player.position + dir * tuning.spawnRadius;
        pos.z = 0f;

        var go = GameAssets.Spawn(GameAssets.I != null ? GameAssets.I.enemy : null, transform);
        if (go == null) return;

        go.transform.position = pos;
        go.transform.localScale = Vector3.one * Mathf.Max(0.01f, tuning.enemyDiameter);
        go.GetComponent<TintedParts>()?.SetTint(tuning.soulColor);

        go.GetComponent<EnemyController>().Init(player, speed, tuning.weaveAmplitude, tuning.weaveFrequency,
                                                tuning.despawnDistance, tuning.vanishTime);
    }
}
