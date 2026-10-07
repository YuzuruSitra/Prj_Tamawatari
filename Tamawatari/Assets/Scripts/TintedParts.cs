using UnityEngine;

/// <summary>
/// プレハブの中で「1色から色を作っている」パーツをまとめて塗り替えるための部品。
/// 人魂・お化けの親玉・ふわっと消えるお化けのように、生成時に基準色が決まるものに付ける。
///
/// 焼き直しツールが、焼いたときの色を基準色で割った比 (<see cref="Part.alphaScale"/> /
/// <see cref="Part.tone"/>) を記録しておくので、実行時は基準色を渡すだけで見た目が揃う。
/// </summary>
public class TintedParts : MonoBehaviour
{
    [System.Serializable]
    public struct Part
    {
        public SpriteRenderer renderer;
        [Tooltip("基準色の不透明度に掛ける比")]
        public float alphaScale;
        [Tooltip("基準色を白へ寄せる量。0 = 基準色そのまま")]
        public float tone;
    }

    [SerializeField] private Part[] parts;

    public Part[] Parts { get => parts; set => parts = value; }

    /// <summary>基準色を流し込む。</summary>
    public void SetTint(Color tint)
    {
        if (parts == null) return;
        for (int i = 0; i < parts.Length; i++)
        {
            var p = parts[i];
            if (p.renderer == null) continue;
            Color c = p.tone > 0f ? Color.Lerp(tint, Color.white, p.tone) : tint;
            p.renderer.color = new Color(c.r, c.g, c.b, tint.a * p.alphaScale);
        }
    }
}
