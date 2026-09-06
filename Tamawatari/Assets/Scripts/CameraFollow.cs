using UnityEngine;

/// <summary>
/// 完全2D・上から見た俯瞰の縦スクロール用カメラ(Orthographic)。
/// XY のみ追従、Z 固定・回転なし。着地/被弾/統合でカメラシェイクを行う。
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [SerializeField] private Transform target;
    [SerializeField] private CameraTuning tuning = new CameraTuning();
    [SerializeField] private float fixedZ = -10f;

    public Transform Target { get => target; set => target = value; }
    public CameraTuning Tuning { get => tuning; set => tuning = value; }

    private Camera _cam;
    private float _shakeTime, _shakeDur, _shakeMag;

    private void Awake()
    {
        Instance = this;
        _cam = GetComponent<Camera>();
        _cam.orthographic = true;
        _cam.orthographicSize = tuning.orthographicSize;
        _cam.nearClipPlane = 0.01f;
        _cam.farClipPlane = 100f;
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = tuning.backgroundColor;
        transform.rotation = Quaternion.identity;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void LateUpdate()
    {
        if (_cam != null)
        {
            _cam.orthographicSize = tuning.orthographicSize;
            _cam.backgroundColor = tuning.backgroundColor;
        }
        if (target == null) return;

        Vector3 desired = new Vector3(target.position.x + tuning.offset.x,
                                     target.position.y + tuning.offset.y, fixedZ);
        Vector3 next = Vector3.Lerp(transform.position, desired,
                                   1f - Mathf.Exp(-tuning.followLerp * Time.deltaTime));
        next.z = fixedZ;

        if (_shakeTime > 0f)
        {
            _shakeTime -= Time.deltaTime;
            float amt = _shakeMag * Mathf.Clamp01(_shakeTime / Mathf.Max(0.0001f, _shakeDur));
            Vector2 r = Random.insideUnitCircle * amt;
            next.x += r.x;
            next.y += r.y;
        }

        transform.position = next;
    }

    // ---- Shake API ----
    public void Shake(float duration, float magnitude)
    {
        // 既存のシェイクより強い場合のみ上書き
        if (magnitude * duration <= _shakeMag * _shakeTime) return;
        _shakeDur = Mathf.Max(0.01f, duration);
        _shakeTime = _shakeDur;
        _shakeMag = magnitude;
    }

    public void ShakeLand(float charge01) =>
        Shake(tuning.landShakeDuration, tuning.landShakeMagnitude * Mathf.Lerp(0.4f, 1f, Mathf.Clamp01(charge01)));

    public void ShakeHit() => Shake(tuning.hitShakeDuration, tuning.hitShakeMagnitude);

    public void ShakeMerge() => Shake(tuning.mergeShakeDuration, tuning.mergeShakeMagnitude);
}
