"""CHANGELOG.md から 1 つの版の節（見出しの次の行から次の ## の前まで）を出す。release の workflow が Release の本文に使う。

使い方: changelog_section.py <CHANGELOG.md> <版 か Unreleased>
節が無いか中身が空なら、終了コード 1。
"""
import re
import sys


def section(text, version):
    head = re.compile(r'^## \[' + re.escape(version) + r'\](?: - \d{4}-\d{2}-\d{2})?\s*$')
    lines = text.split('\n')
    for i, line in enumerate(lines):
        if head.match(line):
            body = []
            for line2 in lines[i + 1:]:
                if line2.startswith('## '):
                    break
                body.append(line2)
            return '\n'.join(body).strip()
    return None


def main(path, version):
    body = section(open(path, encoding='utf-8').read(), version)
    if not body:
        print(f'{path} に [{version}] の節が無いか、中身が空', file=sys.stderr)
        return 1
    print(body)
    return 0


if __name__ == '__main__':
    if len(sys.argv) != 3:
        print(__doc__, file=sys.stderr)
        sys.exit(2)
    sys.exit(main(sys.argv[1], sys.argv[2]))
