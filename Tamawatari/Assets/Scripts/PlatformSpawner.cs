using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 進行方向(基本 +Y = 画面上)へ足場(灯篭)を動的生成する(完全2D)。
///  - メインチェーン:直前の足場からランダムな角度・距離で1本道を伸ばす。
///    距離は必ずプレイヤーの最大ジャンプ距離以内(reachSafety 倍)に収め、届くことを保証。
///  - 加えて各ステップごとに scatterPerStep 個を撒いて密度を上げる。
///  - 各足場は palette の色IDを持ち、色替えイベントで「有効な色」だけが着地可能になる
///    (無効な足場は灰色になり、接地判定からも外れる)。
/// </summary>
public class PlatformSpawner : MonoBehaviour
{
    [SerializeField] private PlatformTuning tuning = new PlatformTuning();

    [Header("Refs")]
    [SerializeField] private PlayerController player;

    private readonly List<LanternPlatform> _chain = new List<LanternPlatform>();
    private readonly List<LanternPlatform> _scatter = new List<LanternPlatform>();
    private Vector3 _lastPos;
    private Vector3 _firstPos;
    private float _headingDeg;
    private int _spawnCount;
    private int _chainIndex;                       // チェーンの通し番号(大灯籠の位置決め)

    private int _blockedColor = -1;                // -1 = 全部有効
    private LanternPlatform _exempt;               // 足元だけは色に関係なく有効
    private bool _golden;                          // ボーナスタイム
    private float _diameterScale = 1f;             // 新規足場の大きさ倍率

    public int PlatformsPassed { get; private set; }
    public PlatformTuning Tuning { get => tuning; set => tuning = value; }
    public PlayerController Player { get => player; set => player = value; }
    public int PaletteCount => (tuning.palette != null && tuning.palette.Length > 0) ? tuning.palette.Length : 1;
    /// <summary>いま乗れない色(-1 なら制限なし)。</summary>
    public int BlockedColor => _blockedColor;
    /// <summary>色制限から除外している足場(足元)。</summary>
    public LanternPlatform ExemptPlatform => _exempt;

    private float MinGap => (player != null ? player.MinJumpDistance : 2.5f) + tuning.minGapExtra;
    private float MaxGap => Mathf.Max(MinGap + 0.5f,
                                     (player != null ? player.MaxJumpDistance : 6.5f) * tuning.reachSafety);
    private int ScatterCap => Mathf.Max(12, tuning.aheadCount * Mathf.Max(1, tuning.scatterPerStep) * 3);

    private Color Palette(int id)
    {
        if (tuning.palette == null || tuning.palette.Length == 0) return new Color(1f, 0.7f, 0.3f);
        return tuning.palette[Mathf.Abs(id) % tuning.palette.Length];
    }

    // ==================== 生成 ====================
    /// <summary>足場0をプレイヤー開始地点に作り、前方分も生成する。戻り値は足場0の中心(z=0)。</summary>
    public Vector3 Initialize(Vector2 startXY)
    {
        _lastPos = new Vector3(startXY.x, startXY.y, 0f);
        _headingDeg = 0f;

        SpawnPlatform(_lastPos, tuning.maxPlatformDiameter, isChain: true);
        _firstPos = _lastPos;

        for (int i = 0; i < tuning.aheadCount; i++) SpawnChainStep();
        return _firstPos;
    }

    private LanternPlatform SpawnPlatform(Vector3 pos, float diameter, bool isChain)
    {
        pos.z = 0f;
        int colorId = Random.Range(0, PaletteCount);
        Color pal = Palette(colorId);

        // 区間の区切りになる大灯籠かどうか
        bool great = false;
        if (isChain)
        {
            int len = Mathf.Max(1, tuning.sectionLength);
            great = _chainIndex > 0 && _chainIndex % len == 0;
            _chainIndex++;
        }

        Color stone = isChain ? tuning.litStone : tuning.dimStone;
        Color core = isChain ? pal
            : new Color(pal.r * tuning.dimCoreFade, pal.g * tuning.dimCoreFade, pal.b * tuning.dimCoreFade, 1f);
        Color glow = GameArt.WithAlpha(pal, isChain ? 0.5f : tuning.dimGlowAlpha);

        if (great)
        {
            stone = tuning.greatStone;
            core = tuning.greatCore;
            glow = GameArt.WithAlpha(tuning.greatCore, 0.8f);
            diameter = tuning.maxPlatformDiameter * Mathf.Max(1f, tuning.greatLanternScale);
        }

        var go = GameAssets.Spawn(GameAssets.I != null ? GameAssets.I.lantern : null, transform);
        if (go == null) return null;

        go.name = $"Lantern_{_spawnCount}";
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * Mathf.Max(0.01f, diameter * _diameterScale);

        var lp = go.GetComponent<LanternPlatform>();
        lp.Setup(colorId, lit: isChain, great: great, order: isChain ? 0 : -6, stone, core, glow);
        if (_golden) lp.SetGolden(true, tuning.goldColor);
        else if (!great && _blockedColor >= 0 && colorId == _blockedColor) lp.SetUsable(false, tuning.disabledColor);

        (isChain ? _chain : _scatter).Add(lp);
        _spawnCount++;
        return lp;
    }

    private void SpawnChainStep()
    {
        _headingDeg = Mathf.Clamp(_headingDeg + Random.Range(-tuning.maxTurnAngleDeg, tuning.maxTurnAngleDeg),
                                  -tuning.maxHeadingDeg, tuning.maxHeadingDeg);
        float gap = Random.Range(MinGap, MaxGap);
        float rad = _headingDeg * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0f);   // 0 = 真上(+Y)
        Vector3 next = _lastPos + dir * gap;

        SpawnPlatform(next, Random.Range(tuning.minPlatformDiameter, tuning.maxPlatformDiameter), isChain: true);
        _lastPos = next;

        for (int i = 0; i < tuning.scatterPerStep; i++)
        {
            Vector2 off = Random.insideUnitCircle * tuning.scatterSpread;
            SpawnPlatform(next + new Vector3(off.x, off.y, 0f),
                          Random.Range(tuning.scatterMinDiameter, tuning.scatterMaxDiameter), isChain: false);
        }

        while (_scatter.Count > ScatterCap)
        {
            if (_scatter[0] != null) Destroy(_scatter[0].gameObject);
            _scatter.RemoveAt(0);
        }
    }

    /// <summary>魂の統合演出:目の前(dir 方向)に安全な大きい足場の道をまっすぐ作る。</summary>
    public void CreateSafePath(Vector3 fromPos, Vector3 dir, int count, float gap, float diameter)
    {
        for (int i = 0; i < _chain.Count; i++)
            if (_chain[i] != null) Destroy(_chain[i].gameObject);
        _chain.Clear();

        Vector3 d = dir; d.z = 0f;
        if (d.sqrMagnitude < 0.0001f) d = Vector3.up;
        d.Normalize();

        _headingDeg = Mathf.Clamp(Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg,
                                  -tuning.maxHeadingDeg, tuning.maxHeadingDeg);
        fromPos.z = 0f;
        _lastPos = fromPos;

        // 安全な道は色替えイベント中でも必ず踏めるようにする
        var first = SpawnPlatform(fromPos, diameter, isChain: true);
        if (first != null) first.SetUsable(true, tuning.disabledColor);
        for (int i = 0; i < Mathf.Max(1, count); i++)
        {
            _lastPos += d * Mathf.Max(0.5f, gap);
            var lp = SpawnPlatform(_lastPos, diameter, isChain: true);
            if (lp != null) lp.SetUsable(true, tuning.disabledColor);
        }

        while (_chain.Count < tuning.aheadCount + 1) SpawnChainStep();
    }

    // ==================== 色フィルタ(イベント用) ====================
    /// <summary>
    /// 指定した色「だけ」乗れなくする(他の色は全部乗れる)。-1 で全解除。
    /// exempt(いま乗っている足場)は色に関係なく有効のまま残す。
    /// </summary>
    public void SetBlockedColor(int blockedColorId, LanternPlatform exempt = null)
    {
        _blockedColor = blockedColorId;
        _exempt = exempt;
        Apply(_chain);
        Apply(_scatter);

        void Apply(List<LanternPlatform> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                if (p == null) continue;
                bool ok = blockedColorId < 0 || p.IsGreat || p.ColorId != blockedColorId || p == exempt;
                p.SetUsable(ok, tuning.disabledColor);
            }
        }
    }

    public Color ColorOf(int id) => Palette(id);
    public bool IsGolden => _golden;

    /// <summary>ボーナスタイム:全部の灯篭を金色にして必ず乗れるようにし、新しい足場も大きくする。</summary>
    public void SetGolden(bool on, float diameterScale)
    {
        _golden = on;
        _diameterScale = on ? Mathf.Max(0.5f, diameterScale) : 1f;

        Apply(_chain);
        Apply(_scatter);

        void Apply(List<LanternPlatform> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                if (p == null) continue;
                p.SetGolden(on, tuning.goldColor);
                if (!on)
                {
                    bool ok = _blockedColor < 0 || p.IsGreat || p.ColorId != _blockedColor || p == _exempt;
                    p.SetUsable(ok, tuning.disabledColor);
                }
            }
        }
    }

    // ==================== 判定 ====================
    /// <summary>
    /// 指定位置が「有効な足場の見た目の円 + probe」以内にあれば true。
    /// 重なっているときはもっとも中心に近い足場を返す。
    /// </summary>
    public bool TryGetLanding(Vector2 pos, float probe, out LanternPlatform platform)
    {
        LanternPlatform best = null;
        float bestD = float.MaxValue;
        Scan(_chain, pos, probe, ref best, ref bestD);
        Scan(_scatter, pos, probe, ref best, ref bestD);
        platform = best;
        return best != null;
    }

    private static void Scan(List<LanternPlatform> list, Vector2 pos, float probe,
                             ref LanternPlatform best, ref float bestD)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var p = list[i];
            if (p == null || !p.IsUsable) continue;
            float d = Vector2.Distance(pos, (Vector2)p.transform.position);
            if (d <= p.VisualRadius + Mathf.Max(0f, probe) && d < bestD) { bestD = d; best = p; }
        }
    }

    /// <summary>指定 Y より先(上)にある、まだ発動していないいちばん手前の大灯籠。</summary>
    public LanternPlatform NextGreatAbove(float y)
    {
        LanternPlatform best = null;
        for (int i = 0; i < _chain.Count; i++)
        {
            var p = _chain[i];
            if (p == null || !p.IsGreat || p.Consumed) continue;
            float py = p.transform.position.y;
            if (py <= y) continue;
            if (best == null || py < best.transform.position.y) best = p;
        }
        return best;
    }

    /// <summary>プレイヤー着地時。通過したチェーン足場を削除し、前方を補充する。クリア判定も行う。</summary>
    public void NotifyLanded(Vector3 playerPos)
    {
        if (_chain.Count == 0) return;

        int cur = 0;
        float best = float.MaxValue;
        for (int i = 0; i < _chain.Count; i++)
        {
            if (_chain[i] == null) continue;
            float sq = ((Vector2)_chain[i].transform.position - (Vector2)playerPos).sqrMagnitude;
            if (sq < best) { best = sq; cur = i; }
        }

        for (int i = 0; i < cur; i++)
            if (_chain[i] != null) Destroy(_chain[i].gameObject);

        if (cur > 0)
        {
            _chain.RemoveRange(0, cur);
            PlatformsPassed += cur;
            if (GameManager.Instance != null) GameManager.Instance.PlatformsPassed = PlatformsPassed;
        }

        for (int i = _scatter.Count - 1; i >= 0; i--)
        {
            if (_scatter[i] == null) { _scatter.RemoveAt(i); continue; }
            if (_scatter[i].transform.position.y < playerPos.y - tuning.cullBehind)
            {
                Destroy(_scatter[i].gameObject);
                _scatter.RemoveAt(i);
            }
        }

        while (_chain.Count < tuning.aheadCount + 1) SpawnChainStep();


        if (tuning.clearPlatformCount > 0 && PlatformsPassed >= tuning.clearPlatformCount)
            GameManager.Instance?.Clear();
    }
}
