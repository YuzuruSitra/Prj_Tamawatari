using UnityEngine;

/// <summary>
/// 方向カーソル(矢印)。完全2D / XY平面。
/// 真上(+Y)を中心に、左右 swingHalfRangeDeg ずつ(合計180度)を振り子のように往復する。
/// 端に到達したら反転。速度は IndicatorTuning.swingSpeed(deg/sec)。
///
/// スペース押下で SetFrozen(true)(方向ロック)、着地で SetFrozen(false)(再開)。
/// 実際の飛行方向は PlayerController が GetDirection() を読んでロックする。
///
/// 矢印は Player プレハブの子として最初から置いてある。
/// </summary>
public class JumpIndicator : MonoBehaviour
{
    [SerializeField] private Transform center;                    // 通常はプレイヤー
    [SerializeField] private IndicatorTuning tuning = new IndicatorTuning();

    [Header("プレハブ上のパーツ")]
    [SerializeField] private Transform marker;             // 矢印そのもの
    [SerializeField] private SpriteRenderer markerSr;
    [SerializeField] private SpriteRenderer glowSr;        // 矢印の背後の光

    private bool _frozen;
    private float _angleDeg;     // 真上(+Y)を0、+X方向(右)を正
    private float _dir = 1f;

    public Transform Center { get => center; set => center = value; }
    public IndicatorTuning Tuning { get => tuning; set => tuning = value; }
    public bool IsFrozen => _frozen;

    public void SetFrozen(bool frozen) => _frozen = frozen;

    /// <summary>大きさと色は Tuning 側の値。プレハブの矢印に流し込む。</summary>
    private void Start()
    {
        if (marker != null) marker.localScale = Vector3.one * Mathf.Max(0.01f, tuning.markerDiameter);
        PlaceMarker();
    }

    private void Update()
    {
        if (center == null || marker == null) return;

        if (!_frozen)
        {
            float half = Mathf.Max(1f, tuning.swingHalfRangeDeg);
            _angleDeg += _dir * tuning.swingSpeed * Time.deltaTime;
            if (_angleDeg >= half) { _angleDeg = half; _dir = -1f; }
            else if (_angleDeg <= -half) { _angleDeg = -half; _dir = 1f; }
        }

        PlaceMarker();

        // ロック中は矢印を強調して光らせる
        if (markerSr != null)
        {
            Color c = tuning.markerColor;
            markerSr.color = _frozen ? Color.Lerp(c, Color.white, 0.45f) : c;
        }
        if (glowSr != null)
            glowSr.color = GameArt.WithAlpha(tuning.markerColor, _frozen ? 0.85f : 0.45f);
    }

    private void PlaceMarker()
    {
        if (marker == null) return;
        Vector3 p = center.position + GetDirection() * tuning.radius;
        p.z = 0f;
        marker.position = p;
        // 矢印スプライトは回転0で +Y を向くので、-角度 で狙う向きへ回す
        marker.rotation = Quaternion.Euler(0f, 0f, -_angleDeg);
    }

    /// <summary>カーソルが指している方向(正規化、XY平面)。真上基準・+X が正。</summary>
    public Vector3 GetDirection()
    {
        float rad = _angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0f);
    }

    public float GetAngleDeg() => _angleDeg;

#if UNITY_EDITOR
    /// <summary>焼き直しツールから、プレハブのパーツ参照を差し込むために使う。</summary>
    public void BindPartsForBake(Transform arrow, SpriteRenderer arrowSr, SpriteRenderer arrowGlow)
    {
        marker = arrow;
        markerSr = arrowSr;
        glowSr = arrowGlow;
    }
#endif
}
