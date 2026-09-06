#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""Assets/Resources/Fonts/ShipporiMincho.ttf を作り直す。

WebGL には OS フォントが無いので、描画に使えるのはビルドに埋め込んだこのフォントだけ。
配布元の Shippori Mincho は 8.3MB あり Web には重すぎるため、必要な字だけに絞っている。

収録する字:
  1. ASCII 印字可能文字
  2. Shift-JIS の記号・かな行と第一水準漢字 (日常的な和文はこれでほぼ賄える)
  3. Assets/Scripts/*.cs に出てくる全文字 (現在の UI 文言を確実に含めるため)

第一水準の外の漢字を新しく UI に足したときは、このスクリプトを流し直すこと。
流さないとその字だけ描画されない。

    pip install fonttools
    python Tools/rebuild_font_subset.py [/path/to/ShipporiMincho-Regular.ttf]

引数を省いた場合は Google Fonts から取得する。
"""
import glob
import os
import subprocess
import sys
import urllib.request

ROOT = os.path.dirname(os.path.abspath(os.path.join(__file__, '..')))
SCRIPTS = os.path.join(ROOT, 'Tamawatari', 'Assets', 'Scripts')
OUT = os.path.join(ROOT, 'Tamawatari', 'Assets', 'Resources', 'Fonts', 'ShipporiMincho.ttf')
UPSTREAM = ('https://github.com/google/fonts/raw/main/ofl/shipporimincho/'
            'ShipporiMincho-Regular.ttf')


def charset():
    chars = {chr(c) for c in range(0x20, 0x7F)}
    for lead in range(0x81, 0x99):          # 記号・かな行 + 第一水準漢字
        for trail in range(0x40, 0xFD):
            if trail == 0x7F:
                continue
            try:
                chars.add(bytes([lead, trail]).decode('cp932'))
            except UnicodeDecodeError:
                pass
    for path in glob.glob(os.path.join(SCRIPTS, '*.cs')):
        with open(path, encoding='utf-8') as fp:
            chars |= set(fp.read())
    return {c for c in chars if c.isprintable() or c == ' '}


def main():
    if len(sys.argv) > 1:
        src = sys.argv[1]
    else:
        src = os.path.join(os.path.dirname(OUT), '_upstream.ttf')
        print('downloading', UPSTREAM)
        urllib.request.urlretrieve(UPSTREAM, src)

    txt = os.path.join(os.path.dirname(OUT), '_charset.txt')
    chars = charset()
    with open(txt, 'w', encoding='utf-8') as fp:
        fp.write(''.join(sorted(chars)))
    print('codepoints:', len(chars))

    subprocess.check_call([
        sys.executable, '-m', 'fontTools.subset', src,
        '--text-file=' + txt, '--output-file=' + OUT,
        '--drop-tables+=DSIG', "--name-IDs=*", '--recalc-bounds',
    ])

    from fontTools.ttLib import TTFont
    have = set(TTFont(OUT).getBestCmap())
    used = set()
    for path in glob.glob(os.path.join(SCRIPTS, '*.cs')):
        with open(path, encoding='utf-8') as fp:
            used |= set(fp.read())
    missing = sorted(c for c in used if c.isprintable() and c != ' ' and ord(c) not in have)
    print('%s -> %.2f MB, glyphs %d' % (OUT, os.path.getsize(OUT) / 1048576.0, len(have)))
    if missing:
        print('WARNING: 元フォントに無い字:', ''.join(missing))

    for tmp in (txt, os.path.join(os.path.dirname(OUT), '_upstream.ttf')):
        if os.path.exists(tmp) and len(sys.argv) <= 1 or tmp == txt:
            os.remove(tmp)


if __name__ == '__main__':
    main()
