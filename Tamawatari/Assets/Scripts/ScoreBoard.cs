using System.Collections.Generic;
using UnityEngine;

/// <summary>これまでの成績1件。</summary>
public struct ScoreEntry
{
    public string Name;
    public int Score;
    public int DepthX10;      // 深度(m) × 10 で保持
    public int Kills;
    public int MaxCombo;
    public bool Cleared;
    public string Date;

    public float DepthMeters => DepthX10 / 10f;
}

/// <summary>
/// リザルトのランキング。PlayerPrefs に上位 Capacity 件を保存する。
/// </summary>
public static class ScoreBoard
{
    public const int Capacity = 8;
    private const string Key = "tamawatari_ranking_v1";

    public static List<ScoreEntry> Load()
    {
        var list = new List<ScoreEntry>();
        string raw = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(raw)) return list;

        foreach (var line in raw.Split(';'))
        {
            if (string.IsNullOrEmpty(line)) continue;
            var f = line.Split('|');
            if (f.Length < 6) continue;
            int.TryParse(f[0], out int sc);
            int.TryParse(f[1], out int dp);
            int.TryParse(f[2], out int kl);
            int.TryParse(f[3], out int cb);
            list.Add(new ScoreEntry
            {
                Name = f.Length >= 7 ? f[6] : "",
                Score = sc,
                DepthX10 = dp,
                Kills = kl,
                MaxCombo = cb,
                Cleared = f[4] == "1",
                Date = f[5],
            });
        }
        list.Sort((a, b) => b.Score.CompareTo(a.Score));
        return list;
    }

    /// <summary>成績を登録し、入った順位(0始まり)を返す。圏外なら -1。</summary>
    public static int Submit(ScoreEntry e)
    {
        if (string.IsNullOrEmpty(e.Date)) e.Date = System.DateTime.Now.ToString("MM/dd HH:mm");

        var list = Load();
        list.Add(e);
        list.Sort((a, b) => b.Score.CompareTo(a.Score));

        int rank = -1;
        for (int i = 0; i < list.Count; i++)
        {
            // 同点は後から入れた方が下。Date まで一致する自分自身を探す
            if (list[i].Score == e.Score && list[i].Date == e.Date && list[i].Kills == e.Kills
                && list[i].DepthX10 == e.DepthX10)
            {
                rank = i;
                break;
            }
        }
        if (list.Count > Capacity) list.RemoveRange(Capacity, list.Count - Capacity);
        if (rank >= Capacity) rank = -1;

        Save(list);
        return rank;
    }

    // ==================== プレイヤー名 ====================
    public const int NameMaxLength = 10;
    private const string NameKey = "tamawatari_player_name";

    /// <summary>タイトルで入れた名前。PlayerPrefs に残るので次回も引き継がれる。</summary>
    public static string PlayerName
    {
        get => Sanitize(PlayerPrefs.GetString(NameKey, ""));
        set
        {
            PlayerPrefs.SetString(NameKey, Sanitize(value));
            PlayerPrefs.Save();
        }
    }

    /// <summary>保存形式の区切り文字を落として長さを詰める。</summary>
    public static string Sanitize(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        var sb = new System.Text.StringBuilder(raw.Length);
        foreach (char c in raw)
        {
            if (c == '|' || c == ';' || c < ' ') continue;
            sb.Append(c);
            if (sb.Length >= NameMaxLength) break;
        }
        return sb.ToString().Trim();
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }

    private static void Save(List<ScoreEntry> list)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (i > 0) sb.Append(';');
            sb.Append(e.Score).Append('|').Append(e.DepthX10).Append('|').Append(e.Kills).Append('|')
              .Append(e.MaxCombo).Append('|').Append(e.Cleared ? '1' : '0').Append('|').Append(e.Date)
              .Append('|').Append(Sanitize(e.Name));
        }
        PlayerPrefs.SetString(Key, sb.ToString());
        PlayerPrefs.Save();
    }
}
