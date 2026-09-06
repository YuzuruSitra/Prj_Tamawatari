using UnityEngine;

/// <summary>
/// UI をゆらゆら揺らす簡易アニメーション。ゲームオーバーの煽り演出などに使う。
/// </summary>
public class UIWiggle : MonoBehaviour
{
    [SerializeField] private Vector2 moveAmplitude = new Vector2(6f, 10f);
    [SerializeField] private float rotateAmplitude = 6f;
    [SerializeField] private float scaleAmplitude = 0.05f;
    [SerializeField] private float speed = 2.4f;

    private RectTransform _rt;
    private Vector2 _basePos;
    private Vector3 _baseScale;
    private float _seed;
    private bool _captured;

    public static UIWiggle Attach(RectTransform rt, Vector2 move, float rotate, float scale, float speed)
    {
        var w = rt.gameObject.AddComponent<UIWiggle>();
        w.moveAmplitude = move;
        w.rotateAmplitude = rotate;
        w.scaleAmplitude = scale;
        w.speed = speed;
        return w;
    }

    private void OnEnable()
    {
        _rt = (RectTransform)transform;
        if (_captured) return;                 // 再表示のたびに基準がずれないよう1度だけ記録
        _basePos = _rt.anchoredPosition;
        _baseScale = _rt.localScale;
        _seed = Random.Range(0f, 10f);
        _captured = true;
    }

    private void Update()
    {
        if (_rt == null) return;
        float t = Time.unscaledTime * speed + _seed;

        _rt.anchoredPosition = _basePos + new Vector2(Mathf.Sin(t * 1.3f) * moveAmplitude.x,
                                                     Mathf.Sin(t) * moveAmplitude.y);
        _rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.8f) * rotateAmplitude);
        float s = 1f + Mathf.Sin(t * 1.7f) * scaleAmplitude;
        _rt.localScale = new Vector3(_baseScale.x * s, _baseScale.y * s, _baseScale.z);
    }
}
