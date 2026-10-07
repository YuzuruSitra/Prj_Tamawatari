// ランキングの画像をクリップボードに置くための WebGL 側の手。
//
// navigator.clipboard.write に PNG の Blob を渡す。これが使えないブラウザ
// (画像のコピーに未対応 / 権限が降りない)ではダウンロードに落とす。
// 書き込みは Promise なので結果はすぐ返せない。TamawatariCopyImage は
// 「受け取れたか」だけを返し、C# 側は TamawatariCopyResult を見張る。
//
//   TamawatariCopyResult : 0 = まだ / 1 = コピーできた / 2 = ダウンロードに落ちた / 3 = 失敗
var TamawatariShot = {
  TamawatariCopyImage: function (dataPtr, length, namePtr) {
    try {
      // UTF8ToString は Unity の WebGL ランタイムが出しているヘルパ
      var name = (typeof UTF8ToString === 'function' ? UTF8ToString(namePtr) : '') || 'tamawatari.png';
      // HEAPU8 の中身は次のフレームで上書きされるので、Blob に渡す前に写しを取る
      var bytes = new Uint8Array(HEAPU8.subarray(dataPtr, dataPtr + length));
      var blob = new Blob([bytes], { type: 'image/png' });

      window.__tamawatariShot = 0;

      // クリップボードに置けないときの逃げ道
      var fallback = function (why) {
        console.warn('[Tamawatari] クリップボードに置けないのでダウンロードします', why);
        try {
          var url = URL.createObjectURL(blob);
          var a = document.createElement('a');
          a.href = url;
          a.download = name;
          a.rel = 'noopener';
          a.style.display = 'none';
          document.body.appendChild(a);
          a.click();
          // 取り消しが早すぎるとダウンロードが始まらない端末があるので少し置く
          setTimeout(function () {
            if (a.parentNode) a.parentNode.removeChild(a);
            URL.revokeObjectURL(url);
          }, 10000);
          window.__tamawatariShot = 2;
        } catch (e) {
          console.error('[Tamawatari] ダウンロードにも失敗しました', e);
          window.__tamawatariShot = 3;
        }
      };

      var canCopy = navigator.clipboard
        && typeof navigator.clipboard.write === 'function'
        && typeof ClipboardItem === 'function';

      if (canCopy) {
        navigator.clipboard.write([new ClipboardItem({ 'image/png': blob })]).then(
          function () { window.__tamawatariShot = 1; },
          function (e) { fallback(e); }
        );
      } else {
        fallback('このブラウザは画像のコピーに未対応');
      }
      return 1;
    } catch (e) {
      console.error('[Tamawatari] 画像を受け取れませんでした', e);
      window.__tamawatariShot = 3;
      return 0;
    }
  },

  TamawatariCopyResult: function () {
    return window.__tamawatariShot | 0;
  },
};

mergeInto(LibraryManager.library, TamawatariShot);
