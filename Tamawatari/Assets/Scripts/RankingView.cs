using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ランキング表示パネル。タイトルとリザルトの両方から Shift で開く。
/// </summary>
public class RankingView : MonoBehaviour
{
    private const int Rows = ScoreBoard.Capacity;

    private Text[] _left = new Text[Rows];
    private Text[] _right = new Text[Rows];
    private Image[] _rowBg = new Image[Rows];
    private Text _empty;

    private static readonly Color Cream = new Color(0.94f, 0.90f, 0.83f);
    private static readonly Color Ember = new Color(1f, 0.68f, 0.28f);
    private static readonly Color Dim = new Color(0.62f, 0.58f, 0.58f);

    public static RankingView Create(Transform parent, string hint)
    {
        var root = MockUtil.CreateRect(parent, "RankingPanel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        MockUtil.CreateImage(root, new Color(0.02f, 0.015f, 0.04f, 0.92f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "Dim");

        var card = MockUtil.CreateImage(root, new Color(0.10f, 0.08f, 0.14f, 0.985f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-420, -300), new Vector2(420, 300), "Card");
        var c = card.transform;

        MockUtil.CreateBox(c, MockUtil.WithAlpha(Ember, 0.9f), new Vector2(0, 290), new Vector2(840, 5), "top");
        MockUtil.CreateBox(c, MockUtil.WithAlpha(Ember, 0.35f), new Vector2(0, -290), new Vector2(840, 3), "bottom");

        MockUtil.CreateText(c, "R A N K I N G", 46, TextAnchor.UpperCenter,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -86), new Vector2(0, -22), Ember, display: true);
        MockUtil.CreateBox(c, MockUtil.WithAlpha(Ember, 0.4f), new Vector2(0, 198), new Vector2(280, 2), "rule");

        var v = root.gameObject.AddComponent<RankingView>();

        for (int i = 0; i < Rows; i++)
        {
            float y = 152f - i * 52f;
            v._rowBg[i] = MockUtil.CreateBox(c, new Color(1f, 1f, 1f, 0.035f), new Vector2(0, y), new Vector2(760, 44), $"row{i}");
            v._left[i] = MockUtil.CreateText(c, "", 22, TextAnchor.MiddleLeft,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-364, y - 22), new Vector2(220, y + 22), Cream);
            v._right[i] = MockUtil.CreateText(c, "", 28, TextAnchor.MiddleRight,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(220, y - 22), new Vector2(364, y + 22), Ember);
        }

        v._empty = MockUtil.CreateText(c, "まだ記録がありません", 24, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300, -20), new Vector2(300, 30), Dim);

        InputLabel.Bind(MockUtil.CreateText(c, "", 21, TextAnchor.LowerCenter,
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 22), new Vector2(0, 58),
            new Color(0.74f, 0.7f, 0.68f)), hint);

        root.gameObject.SetActive(false);
        return v;
    }

    /// <summary>一覧を読み直して描画する。highlight は強調する順位(0始まり、なければ -1)。</summary>
    public void Refresh(int highlight = -1)
    {
        var list = ScoreBoard.Load();
        _empty.gameObject.SetActive(list.Count == 0);

        for (int i = 0; i < Rows; i++)
        {
            bool has = i < list.Count;
            _left[i].gameObject.SetActive(has);
            _right[i].gameObject.SetActive(has);
            _rowBg[i].gameObject.SetActive(has);
            if (!has) continue;

            var e = list[i];
            bool hi = i == highlight;
            _left[i].text = $"#{i + 1}   深度 {e.DepthMeters:F1}m   成仏 {e.Kills}   最大 x{e.MaxCombo}" +
                            (e.Cleared ? "   CLEAR" : "") + $"   {e.Date}";
            _right[i].text = e.Score.ToString();
            _left[i].color = hi ? new Color(1f, 0.88f, 0.5f) : (e.Cleared ? new Color(1f, 0.92f, 0.78f) : Cream);
            _right[i].color = hi ? new Color(1f, 0.85f, 0.35f) : Ember;
            _rowBg[i].color = hi ? new Color(1f, 0.7f, 0.25f, 0.16f) : new Color(1f, 1f, 1f, 0.035f);
        }
    }

    public bool IsOpen => gameObject.activeSelf;

    public void Toggle(int highlight = -1)
    {
        bool next = !gameObject.activeSelf;
        gameObject.SetActive(next);
        if (next) Refresh(highlight);
    }

    public void Close() => gameObject.SetActive(false);
}
