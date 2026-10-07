using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// 見えている画面をそのまま画像にして<b>クリップボードに置く</b>。
/// ランキングの「画像をコピー」(<see cref="RankingView"/>)から呼ぶ。
///
/// ファイルとして書き出すのではなく貼り付けられる状態にするのが狙いなので、
/// 出し先は環境ごとにこうなっている:
///   WebGL      : <c>navigator.clipboard.write</c> に PNG の Blob を渡す
///                (<c>Assets/Plugins/WebGL/TamawatariShot.jslib</c>)。
///                ブラウザが画像のコピーに対応していなければダウンロードに落ちる
///   Windows    : CF_DIB をクリップボードに置く(user32 を直接叩く)
///   それ以外   : クリップボードに手が届かないので <c>persistentDataPath/Screenshots/</c> に書く
///
/// 撮るのは「いま見えている画面」なので、写したくない飾り(操作案内・コピーボタン)は
/// 呼び出し側が <see cref="Capture"/> の hide に並べる。別 Canvas にいるバーチャルパッドは
/// 自分で <see cref="IsCapturing"/> を見て引っ込む。
/// </summary>
public static class ScoreShot
{
    /// <summary>画像のファイル名の頭。</summary>
    public const string Prefix = "tamawatari";

    /// <summary>クリップボードに手が届かない環境で書き出す先(persistentDataPath の下)。</summary>
    public const string FolderName = "Screenshots";

    /// <summary>ブラウザのクリップボード書き込みは非同期なので、決まるまで待つ上限(秒)。</summary>
    private const float ClipboardWait = 4f;

    /// <summary>撮影中。画面に出したくないものを隠す合図になる。</summary>
    public static bool IsCapturing { get; private set; }

    /// <summary>
    /// 撮影中にシーンが変わるなどで途中で終わったときの後始末。印を落としておかないと、
    /// これを見て引っ込むもの(バーチャルパッド)が次のシーンでも隠れたままになる。
    /// </summary>
    public static void Cancel() => IsCapturing = false;

    /// <summary>日時からファイル名を作る。tag は "ranking" など中身の呼び名。</summary>
    public static string MakeFileName(string tag) =>
        $"{Prefix}_{(string.IsNullOrEmpty(tag) ? "shot" : tag)}_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";

    /// <summary>
    /// hide を隠し、1フレーム置いて(別 Canvas のバーチャルパッドが引っ込むのを待って)
    /// 画面を撮り、クリップボードに置く。終わったら隠したものを元に戻し、
    /// done(成功したか, 画面に出す一言) を呼ぶ。
    /// </summary>
    public static IEnumerator Capture(string fileName, System.Action<bool, string> done,
                                      params GameObject[] hide)
    {
        if (IsCapturing)
        {
            done?.Invoke(false, "いま コピーしています");
            yield break;
        }
        IsCapturing = true;

        // もともと消えていたものは触らない(戻すときに出してしまわないため)
        var restore = new List<GameObject>();
        if (hide != null)
        {
            for (int i = 0; i < hide.Length; i++)
            {
                var go = hide[i];
                if (go == null || !go.activeSelf) continue;
                go.SetActive(false);
                restore.Add(go);
            }
        }

        yield return null;                            // 隠れたものが実際に描かれなくなるまで待つ
        yield return new WaitForEndOfFrame();          // 画面が出来上がってから読む

        Texture2D tex = null;
        try
        {
            tex = ScreenCapture.CaptureScreenshotAsTexture();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ScoreShot] 画面を撮れませんでした: {e.Message}");
        }

        for (int i = 0; i < restore.Count; i++)
            if (restore[i] != null) restore[i].SetActive(true);
        IsCapturing = false;

        if (tex == null)
        {
            done?.Invoke(false, "コピーできませんでした");
            yield break;
        }

        yield return Deliver(tex, fileName, done);     // tex はこの中で捨てる
    }

    // ==================== 出し先 ====================

    /// <summary>撮った絵をクリップボード(届かなければファイル)へ渡す。</summary>
    private static IEnumerator Deliver(Texture2D tex, string fileName, System.Action<bool, string> done)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        byte[] png = EncodePng(tex);
        Object.Destroy(tex);
        if (png == null)
        {
            done?.Invoke(false, "コピーできませんでした");
            yield break;
        }

        int accepted = 0;
        try
        {
            accepted = TamawatariCopyImage(png, png.Length, fileName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ScoreShot] ブラウザに画像を渡せませんでした: {e.Message}");
        }
        if (accepted == 0)
        {
            done?.Invoke(false, "コピーできませんでした");
            yield break;
        }

        // クリップボードへの書き込みは Promise なので、結果が入るまで見張る
        int result = 0;
        float until = Time.realtimeSinceStartup + ClipboardWait;
        while (Time.realtimeSinceStartup < until)
        {
            result = TamawatariCopyResult();
            if (result != 0) break;
            yield return null;
        }

        switch (result)
        {
            case 1: done?.Invoke(true, "クリップボードに保持しました"); break;
            case 2: done?.Invoke(true, "コピーできないので ダウンロードしました"); break;
            default: done?.Invoke(false, "コピーできませんでした"); break;
        }
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        byte[] dib = null;
        try
        {
            dib = ToDib(tex);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ScoreShot] 画像を組み立てられませんでした: {e.Message}");
        }
        Object.Destroy(tex);

        bool ok = dib != null && CopyDibToClipboard(dib);
        done?.Invoke(ok, ok ? "クリップボードに保持しました" : "コピーできませんでした");
        yield break;
#else
        byte[] png = EncodePng(tex);
        Object.Destroy(tex);

        string note = "ほぞんできませんでした";
        bool ok = png != null && WriteFile(png, fileName, out note);
        done?.Invoke(ok, note);
        yield break;
#endif
    }

    private static byte[] EncodePng(Texture2D tex)
    {
        try
        {
            return tex != null ? tex.EncodeToPNG() : null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ScoreShot] PNG にできませんでした: {e.Message}");
            return null;
        }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    /// <summary>jslib 側。受け取れたら 1。コピーの成否は <see cref="TamawatariCopyResult"/> で見る。</summary>
    [DllImport("__Internal")]
    private static extern int TamawatariCopyImage(byte[] data, int length, string fileName);

    /// <summary>0 = まだ決まっていない / 1 = コピーできた / 2 = ダウンロードに落ちた / 3 = 失敗。</summary>
    [DllImport("__Internal")]
    private static extern int TamawatariCopyResult();
#endif

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    // ---- Windows のクリップボード ----
    // Unity には画像をクリップボードに置く API が無い(systemCopyBuffer は文字だけ)ので、
    // user32 / kernel32 を直接叩いて CF_DIB を置く。

    private const uint CfDib = 8;
    private const uint GmemMoveable = 0x0002;

    [DllImport("user32.dll")] private static extern bool OpenClipboard(System.IntPtr hWndNewOwner);
    [DllImport("user32.dll")] private static extern bool CloseClipboard();
    [DllImport("user32.dll")] private static extern bool EmptyClipboard();
    [DllImport("user32.dll")] private static extern System.IntPtr SetClipboardData(uint format, System.IntPtr hMem);
    [DllImport("kernel32.dll")] private static extern System.IntPtr GlobalAlloc(uint flags, System.UIntPtr bytes);
    [DllImport("kernel32.dll")] private static extern System.IntPtr GlobalLock(System.IntPtr hMem);
    [DllImport("kernel32.dll")] private static extern bool GlobalUnlock(System.IntPtr hMem);
    [DllImport("kernel32.dll")] private static extern System.IntPtr GlobalFree(System.IntPtr hMem);

    /// <summary>
    /// Texture2D を CF_DIB にする。24bit BI_RGB・行は下から上・1行 4 バイト境界。
    /// 32bit だと貼り先によってアルファの扱いが揺れて真っ黒になることがあるので 24bit で渡す。
    /// </summary>
    private static byte[] ToDib(Texture2D tex)
    {
        int w = tex.width, h = tex.height;
        var px = tex.GetPixels32();           // 0 行目が画面の下端。DIB も下から上なので並びはそのまま
        int stride = (w * 3 + 3) & ~3;
        const int header = 40;                // BITMAPINFOHEADER
        var dib = new byte[header + stride * h];

        void I32(int at, int v)
        {
            dib[at] = (byte)v;
            dib[at + 1] = (byte)(v >> 8);
            dib[at + 2] = (byte)(v >> 16);
            dib[at + 3] = (byte)(v >> 24);
        }
        void I16(int at, int v)
        {
            dib[at] = (byte)v;
            dib[at + 1] = (byte)(v >> 8);
        }

        I32(0, header);                       // biSize
        I32(4, w);                            // biWidth
        I32(8, h);                            // biHeight(正 = 下から上)
        I16(12, 1);                           // biPlanes
        I16(14, 24);                          // biBitCount
        I32(16, 0);                           // biCompression = BI_RGB
        I32(20, stride * h);                  // biSizeImage

        for (int y = 0; y < h; y++)
        {
            int src = y * w;
            int dst = header + y * stride;
            for (int x = 0; x < w; x++)
            {
                var c = px[src + x];
                dib[dst++] = c.b;
                dib[dst++] = c.g;
                dib[dst++] = c.r;
            }
        }
        return dib;
    }

    private static bool CopyDibToClipboard(byte[] dib)
    {
        // 他のアプリが握っていることがあるので少しだけ粘る
        bool opened = false;
        for (int i = 0; i < 10 && !opened; i++)
        {
            opened = OpenClipboard(System.IntPtr.Zero);
            if (!opened) System.Threading.Thread.Sleep(10);
        }
        if (!opened)
        {
            Debug.LogError("[ScoreShot] クリップボードを開けませんでした(他のアプリが使用中)。");
            return false;
        }

        var hMem = System.IntPtr.Zero;
        try
        {
            if (!EmptyClipboard()) return false;

            hMem = GlobalAlloc(GmemMoveable, (System.UIntPtr)dib.Length);
            if (hMem == System.IntPtr.Zero) return false;

            var p = GlobalLock(hMem);
            if (p == System.IntPtr.Zero) return false;
            try
            {
                Marshal.Copy(dib, 0, p, dib.Length);
            }
            finally
            {
                GlobalUnlock(hMem);
            }

            if (SetClipboardData(CfDib, hMem) == System.IntPtr.Zero) return false;
            hMem = System.IntPtr.Zero;        // 置けたら所有権はクリップボードへ移る
            Debug.Log("[ScoreShot] クリップボードに保持しました。");
            return true;
        }
        finally
        {
            if (hMem != System.IntPtr.Zero) GlobalFree(hMem);
            CloseClipboard();
        }
    }
#endif

    /// <summary>クリップボードに手が届かない環境の受け皿。</summary>
    private static bool WriteFile(byte[] png, string fileName, out string note)
    {
        try
        {
            string dir = Path.Combine(Application.persistentDataPath, FolderName);
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, fileName);
            File.WriteAllBytes(path, png);
            Debug.Log($"[ScoreShot] ほぞんしました: {path}");
            note = $"ほぞんしました  {fileName}";
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ScoreShot] 書き出せませんでした: {e.Message}");
            note = "ほぞんできませんでした";
            return false;
        }
    }
}
