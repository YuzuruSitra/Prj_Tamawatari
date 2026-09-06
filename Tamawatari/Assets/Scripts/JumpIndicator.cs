using UnityEngine;

/// <summary>
/// 方向カーソル(矢印)。完全2D / XY平面。
/// 真上(+Y)を中心に、左右 swingHalfRangeDeg ずつ(合計180度)を振り子のように往復する。
/// 端に到達したら反転。速度は IndicatorTuning.swingSpeed(deg/sec)。
///
/// スペース押下で SetFrozen(true)(方向ロック)、着地で SetFrozen(false)(再開)。
/// 実際の飛行方向は PlayerController が GetDirection() を読んでロックする。
/// </summary>
public class JumpIndicator : MonoBehaviour
{
    [SerializeField] private Transform center;                    // 通常はプレイヤー
    [SerializeField] private IndicatorTuning tuning = new IndicatorTuning();

    private Transform _marker;
    private SpriteRenderer _markerSr, _glowSr;
    private bool _frozen;
    private float _angleDeg;     // 真上(+Y)を0、+X方向(右)を正
    private float _dir = 1f;

    public Transform Center { get => center; set => center = value; }
    public IndicatorTuning Tuning { get => tuning; set => tuning = value; }
    public bool IsFrozen => _frozen;

    public void SetFrozen(bool frozen) => _frozen = frozen;

    private void Awake()
    {
        var go = MockUtil.MakeArrow("IndicatorArrow", tuning.markerColor, tuning.markerDiameter, sortingOrder: 20);
        _markerSr = go.GetComponent<SpriteRenderer>();

        var glow = MockUtil.MakeGlow("arrowGlow", MockUtil.WithAlpha(tuning.markerColor, 0.5f), 1.6f, 18);
        glow.transform.SetParent(go.transform, false);
        _glowSr = glow.GetComponent<SpriteRenderer>();

        _marker = go.transform;
        _marker.SetParent(transform, false);
    }

    private void Start() => PlaceMarker();

    private void Update()
    {
        if (center == null || _marker == null) return;

        if (!_frozen)
        {
            float half = Mathf.Max(1f, tuning.swingHalfRangeDeg);
            _angleDeg += _dir * tuning.swingSpeed * Time.deltaTime;
            if (_angleDeg >= half) { _angleDeg = half; _dir = -1f; }
            else if (_angleDeg <= -half) { _angleDeg = -half; _dir = 1f; }
        }

        PlaceMarker();

        // ロック中は矢印を強調して光らせる
        if (_markerSr != null)
        {
            Color c = tuning.markerColor;
            _markerSr.color = _frozen ? Color.Lerp(c, Color.white, 0.45f) : c;
        }
        if (_glowSr != null)
            _glowSr.color = MockUtil.WithAlpha(tuning.markerColor, _frozen ? 0.85f : 0.45f);
    }

    private void PlaceMarker()
    {
        Vector3 p = center.position + GetDirection() * tuning.radius;
        p.z = 0f;
        _marker.position = p;
        // 矢印スプライトは回転0で +Y を向くので、-角度 で狙う向きへ回す
        _marker.rotation = Quaternion.Euler(0f, 0f, -_angleDeg);
    }

    /// <summary>カーソルが指している方向(正規化、XY平面)。真上基準・+X が正。</summary>
    public Vector3 GetDirection()
    {
        float rad = _angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0f);
    }

    public float GetAngleDeg() => _angleDeg;
}
