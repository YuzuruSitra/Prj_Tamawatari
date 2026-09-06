using UnityEngine;

/// <summary>
/// プレイヤーの簡易アニメーション(手続き)。
/// PlayerController の状態を読み、見た目ルート(Visual)の位置・スケール・回転だけを動かす。
///  待機: ふわふわ上下 + 呼吸  /  溜め: しゃがみ + つぶれ + 震え
///  跳躍: 跳ね上がり + 進行方向への伸びと傾き  /  着地: つぶれてから戻る  /  失敗: 縮みながら回転
/// </summary>
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private PlayerAnimTuning tuning = new PlayerAnimTuning();

    private PlayerController _pc;
    private Transform _visual;
    private Vector3 _baseScale;
    private float _landTimer;
    private bool _wasJumping;
    private float _t;

    public PlayerAnimTuning Tuning { get => tuning; set => tuning = value; }

    public void Init(PlayerController pc, Transform visual, PlayerAnimTuning t = null)
    {
        _pc = pc;
        _visual = visual;
        if (t != null) tuning = t;
        _baseScale = visual != null ? visual.localScale : Vector3.one;
        _t = Random.Range(0f, 10f);
    }

    private void LateUpdate()
    {
        if (_pc == null || _visual == null) return;
        _t += Time.deltaTime;

        // 着地したら squash を仕込む
        bool jumping = _pc.IsJumping;
        if (_wasJumping && !jumping && !_pc.IsMissFalling) _landTimer = tuning.landSquashTime;
        _wasJumping = jumping;
        if (_landTimer > 0f) _landTimer -= Time.deltaTime;

        Vector3 pos = Vector3.zero;
        Vector3 scl = Vector3.one;
        float rot = 0f;

        if (_pc.IsMissFalling)
        {
            float k = _pc.MissProgress01;
            scl = Vector3.one * Mathf.Max(0.01f, 1f - k);
            rot = -tuning.missSpinDeg * k;
            pos.y = -0.5f * k;
        }
        else if (jumping)
        {
            float t = _pc.JumpProgress01;
            float s = Mathf.Sin(Mathf.PI * t);
            pos.y = tuning.jumpHop * s;
            scl = new Vector3(1f - tuning.jumpStretch * 0.5f * s, 1f + tuning.jumpStretch * s, 1f);

            Vector3 d = _pc.LockedDir;
            float aimDeg = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;      // 真上=0
            rot = -aimDeg * (tuning.jumpTiltDeg / 90f) * s;
        }
        else
        {
            float bob = Mathf.Sin(_t * tuning.bobSpeed);
            pos.y = tuning.bobAmount * bob;
            float breathe = tuning.breathAmount * bob;
            scl = new Vector3(1f - breathe, 1f + breathe, 1f);

            if (_pc.IsCharging)
            {
                float c = _pc.Charge01;
                pos.y -= tuning.crouchAmount * c;
                pos.x += Mathf.Sin(_t * 46f) * tuning.chargeShake * c;
                scl.x += tuning.chargeSquash * c;
                scl.y -= tuning.chargeSquash * 0.8f * c;
            }

            if (_landTimer > 0f)
            {
                float k = Mathf.Clamp01(_landTimer / Mathf.Max(0.01f, tuning.landSquashTime)); // 1 -> 0
                float sq = tuning.landSquash * k;
                scl.x += sq;
                scl.y -= sq * 0.9f;
                pos.y -= sq * 0.25f;
            }
        }

        _visual.localPosition = pos;
        _visual.localScale = new Vector3(_baseScale.x * scl.x, _baseScale.y * scl.y, _baseScale.z);
        _visual.localRotation = Quaternion.Euler(0f, 0f, rot);
    }
}
