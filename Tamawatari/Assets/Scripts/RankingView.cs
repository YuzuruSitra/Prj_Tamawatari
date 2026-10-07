using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ランキング表示パネル(<c>Assets/Prefabs/UI/RankingPanel.prefab</c>)。
/// タイトルとリザルトの両方から Shift で開く。閉じるときの案内文だけ呼び出し側で差し替える。
///
/// 出すのは端末内の <see cref="ScoreBoard"/> だけ。**通信は行わない**。
///
/// 開いている間は「画像をコピー」(P / パッド △ / カード右上のボタン)でこの一覧を画像にして
/// クリップボードに置ける。撮るのは <see cref="ScoreShot"/>、ここは飾りの出し入れだけ。
/// </summary>
public class RankingView : MonoBehaviour
{
    private const int Rows = ScoreBoard.Capacity;

    /// <summary>コピーの結果を出しておく秒数。</summary>
    private const float NoteLife = 5f;

    [Header("プレハブ上のパーツ")]
    [SerializeField] private Text[] left = new Text[Rows];
    [SerializeField] private Text[] right = new Text[Rows];
    [SerializeField] private Image[] rowBg = new Image[Rows];
    [SerializeField] private Text empty;
    [SerializeField] private InputLabel hintLabel;

    [Header("画像コピー")]
    [SerializeField] private RectTransform copyButton;
    [Tooltip("ボタンの後ろのぼんやりした光")]
    [SerializeField] private Image copyGlow;
    [Tooltip("カプセルの縁取り(箱 + 両端の円の3枚)")]
    [SerializeField] private Image[] copyEdge;
    [Tooltip("カプセルの面(箱 + 両端の円の3枚)")]
    [SerializeField] private Image[] copyFace;
    [SerializeField] private Text copyLabel;
    [SerializeField] private Text copyKey;
    [Tooltip("撮るときだけ出す署名(ゲーム名・日付)")]
    [SerializeField] private Text copyStamp;
    [Tooltip("コピーの結果を数秒だけ出す")]
    [SerializeField] private Text copyNote;

    private static readonly Color Cream = new Color(0.94f, 0.90f, 0.83f);
    private static readonly Color Ember = new Color(1f, 0.68f, 0.28f);

    // 画像コピー
    private System.Func<Rect> _captureArea;
    private bool _capturing;
    private float _press;
    private string _noteText = "";
    private float _noteTime = -99f;
    private bool _noteOk;

    /// <summary>Canvas の下に1枚置く。hint は {C} / {S} を含む案内文。</summary>
    public static RankingView Create(Transform parent, string hint)
    {
        var go = GameAssets.Spawn(GameAssets.I != null ? GameAssets.I.rankingPanel : null, parent);
        if (go == null) return null;

        var v = go.GetComponent<RankingView>();
        if (v != null && v.hintLabel != null) v.hintLabel.Format = hint;
        go.SetActive(false);
        return v;
    }

    /// <summary>
    /// 一覧を読み直して描画する。highlight は強調する順位(0始まり、なければ -1)。
    /// </summary>
    public void Refresh(int highlight = -1)
    {
        var list = ScoreBoard.Load();
        if (empty != null) empty.gameObject.SetActive(list.Count == 0);

        for (int i = 0; i < Rows && i < left.Length; i++)
        {
            bool has = i < list.Count;
            ShowRow(i, has);
            if (!has) continue;

            var e = list[i];
            string name = string.IsNullOrEmpty(e.Name) ? "ななし" : e.Name;
            left[i].text = $"#{i + 1}  {name}   深度 {e.DepthMeters:F1}m   成仏 {e.Kills}   最大 x{e.MaxCombo}" +
                           (e.Cleared ? "   CLEAR" : "") + $"   {e.Date}";
            PaintRow(i, e.Score, i == highlight, e.Cleared);
        }
    }

    private void ShowRow(int i, bool on)
    {
        left[i].gameObject.SetActive(on);
        right[i].gameObject.SetActive(on);
        rowBg[i].gameObject.SetActive(on);
    }

    private void PaintRow(int i, int score, bool hi, bool cleared)
    {
        right[i].text = score.ToString();
        left[i].color = hi ? new Color(1f, 0.88f, 0.5f) : (cleared ? new Color(1f, 0.92f, 0.78f) : Cream);
        right[i].color = hi ? new Color(1f, 0.85f, 0.35f) : Ember;
        rowBg[i].color = hi ? new Color(1f, 0.7f, 0.25f, 0.16f) : new Color(1f, 1f, 1f, 0.035f);
    }

    public bool IsOpen => gameObject.activeSelf;

    public void Toggle(int highlight = -1)
    {
        bool next = !gameObject.activeSelf;
        gameObject.SetActive(next);
        if (next) Refresh(highlight);
    }

    public void Close() => gameObject.SetActive(false);

    // ==================== 画像コピー ====================

    private void Awake()
    {
        // ボタンが出ていない間は空の矩形を返す(タッチが Confirm に抜けるように)
        _captureArea = () => copyButton != null && copyButton.gameObject.activeInHierarchy
            ? GameArt.ScreenRect(copyButton)
            : Rect.zero;
    }

    private void OnEnable()
    {
        InputHub.CaptureTouchArea = _captureArea;

        // 撮影中に閉じられるとコルーチンごと止まるので、開くたびに飾りを出し直す
        _capturing = false;
        if (copyButton != null) copyButton.gameObject.SetActive(true);
        if (hintLabel != null) hintLabel.gameObject.SetActive(true);
        if (copyStamp != null) copyStamp.gameObject.SetActive(false);
        ShowNote(true, "");
    }

    private void OnDisable()
    {
        if (InputHub.CaptureTouchArea == _captureArea) InputHub.CaptureTouchArea = null;
        if (_capturing) ScoreShot.Cancel();
    }

    private void Update()
    {
        UpdateCopyButton();
        UpdateNote();

        // P / △ / 右下のボタンで、この一覧を画像にしてクリップボードへ
        if (InputHub.CapturePressed) BeginCapture();
    }

    /// <summary>
    /// 一覧をそのまま画像にする。操作案内とコピーボタンは写らないように
    /// <see cref="ScoreShot"/> に隠してもらい、代わりに署名を1行出す。
    /// </summary>
    private void BeginCapture()
    {
        if (_capturing || copyButton == null) return;      // 焼き直し前のプレハブには無い
        _capturing = true;
        AudioManager.PlayUi();
        ShowNote(true, "");                                // 前回のお知らせを消しておく

        if (copyStamp != null)
        {
            copyStamp.text = $"たまわたり   ランキング   {System.DateTime.Now:yyyy/MM/dd}";
            copyStamp.gameObject.SetActive(true);
        }

        var hide = new List<GameObject> { copyButton.gameObject };
        if (hintLabel != null) hide.Add(hintLabel.gameObject);

        StartCoroutine(RunCapture(hide.ToArray()));
    }

    private IEnumerator RunCapture(GameObject[] hide)
    {
        yield return ScoreShot.Capture(ScoreShot.MakeFileName("ranking"), ShowNote, hide);
        if (copyStamp != null) copyStamp.gameObject.SetActive(false);
        _capturing = false;
    }

    private void ShowNote(bool ok, string text)
    {
        _noteText = text ?? "";
        _noteOk = ok;
        _noteTime = Time.unscaledTime;
    }

    /// <summary>コピーの結果を <see cref="NoteLife"/> 秒だけ出す。</summary>
    private void UpdateNote()
    {
        if (copyNote == null) return;
        float age = Time.unscaledTime - _noteTime;
        bool show = !string.IsNullOrEmpty(_noteText) && age >= 0f && age < NoteLife;
        if (copyNote.gameObject.activeSelf != show) copyNote.gameObject.SetActive(show);
        if (!show) return;

        copyNote.text = _noteText;
        float fade = Mathf.Clamp01(Mathf.Min(age / 0.12f, (NoteLife - age) / 0.6f));
        copyNote.color = GameArt.WithAlpha(
            _noteOk ? new Color(1f, 0.86f, 0.5f) : new Color(1f, 0.55f, 0.48f), fade);
    }

    /// <summary>
    /// コピーボタン。普段はかすかに息をして、押している間はEmber 色の縁が白く灯り、
    /// 後ろの光が強まってわずかに縮む。
    /// </summary>
    private void UpdateCopyButton()
    {
        if (copyButton == null) return;

        float target = _capturing || InputHub.CaptureHeld ? 1f : 0f;
        _press = Mathf.MoveTowards(_press, target,
                                   Time.unscaledDeltaTime * (target > _press ? 14f : 6f));

        // 押していない間のゆっくりした明滅。目を引きすぎないよう振れ幅は小さく
        float breath = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.9f);
        float lit = Mathf.Max(_press, 0.14f * breath);

        if (copyGlow != null)
            copyGlow.color = GameArt.WithAlpha(Ember, 0.05f + 0.33f * lit);

        Paint(copyEdge, GameArt.WithAlpha(Color.Lerp(Ember, Color.white, 0.6f * _press),
                                          0.55f + 0.45f * lit));
        Paint(copyFace, new Color(0.12f + 0.14f * _press, 0.09f + 0.07f * _press,
                                  0.16f + 0.12f * _press, 0.98f));

        if (copyLabel != null)
            copyLabel.color = GameArt.WithAlpha(Color.Lerp(Cream, Color.white, _press),
                                                _capturing ? 0.45f : 1f);
        if (copyKey != null)
            copyKey.color = new Color(0.78f, 0.74f, 0.72f, _capturing ? 0.3f : 0.8f);

        float sc = 1f - 0.05f * _press;
        copyButton.localScale = new Vector3(sc, sc, 1f);
    }

    /// <summary>カプセルは3枚で1つの形なので、必ずまとめて塗る。</summary>
    private static void Paint(Image[] parts, Color color)
    {
        for (int i = 0; parts != null && i < parts.Length; i++)
            if (parts[i] != null) parts[i].color = color;
    }

#if UNITY_EDITOR
    /// <summary>焼き直しツールから、プレハブのパーツ参照を差し込むために使う。</summary>
    public void BindPartsForBake(Text[] leftTexts, Text[] rightTexts, Image[] rowBackgrounds,
                                 Text emptyText, InputLabel hint)
    {
        left = leftTexts; right = rightTexts; rowBg = rowBackgrounds; empty = emptyText;
        hintLabel = hint;
    }

    public void SetCopyRefs(RectTransform button, Image glow, Image[] edge, Image[] face,
                            Text label, Text key, Text stamp, Text note)
    {
        copyButton = button; copyGlow = glow; copyEdge = edge; copyFace = face;
        copyLabel = label; copyKey = key; copyStamp = stamp; copyNote = note;
    }
#endif
}
