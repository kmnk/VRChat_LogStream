"""C# のファイルが BOM なしの UTF-8 と LF かを確かめる。外れたファイルを出し、1 件でもあれば終了コード 1。

Assets/Kmnk/Core/ は VRChat_Core のコピーで、中身は Core のリポジトリが決めるので見ない。
"""
import os
import sys

TARGETS = [os.path.join('Assets', 'Kmnk', 'LogStream'), os.path.join('Assets', 'LogStreamDev')]


def main(root):
    bad = 0
    count = 0
    for t in TARGETS:
        for d, _, files in os.walk(os.path.join(root, t)):
            for f in sorted(files):
                if not f.endswith('.cs'):
                    continue
                count += 1
                p = os.path.join(d, f)
                data = open(p, 'rb').read()
                reasons = []
                if data.startswith(b'\xef\xbb\xbf'):
                    reasons.append('BOM')
                if b'\r' in data:
                    reasons.append('CR')
                try:
                    data.decode('utf-8')
                except UnicodeDecodeError:
                    reasons.append('UTF-8 でない')
                if reasons:
                    bad += 1
                    print(f'{os.path.relpath(p, root)}: {"・".join(reasons)}')
    print(f'.cs {count} ファイル、外れたもの {bad}', file=sys.stderr)
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1]))
