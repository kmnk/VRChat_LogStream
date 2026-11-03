"""Udon で動く C# から、UdonSharp が受け付けない構文を文字列で探す。見つかれば ファイル:行: 理由 を出し、終了コード 1。

bin/compile は、使われない field に書いた List<T> などを止めないことがあるので、この検査で補う。
文字列の照合なので、コメントと文字列の中身は外してから探す。見ない構文（ラムダ、local function、タプル）は
docs/development.md の「UdonSharp の制約の検査」にある。

対象は Packages/dev.kmnk.log-stream と Assets/LogStreamDev/TestUdon の .cs のうち、Editor/ の下に無いもの。
"""
import os
import re
import sys

TARGETS = [os.path.join('Packages', 'dev.kmnk.log-stream'), os.path.join('Assets', 'LogStreamDev', 'TestUdon')]

RULES = [
    (re.compile(r'\b(List|Dictionary|HashSet|Queue|Stack)\s*<'), 'generic のコレクション'),
    (re.compile(r'^\s*using\s+System\.Linq\b'), 'LINQ'),
    (re.compile(r'\b(try|catch|finally|throw)\b'), 'try/catch/throw'),
    (re.compile(r'\b(async|await|yield)\b'), 'async/await/yield'),
    (re.compile(r'\?\.'), '?.'),
    (re.compile(r'\binterface\s+\w'), 'interface'),
    (re.compile(r'\bis\s+(not\s+)?[A-Za-z_]'), 'is'),
    (re.compile(r'\bas\s+[A-Z]'), 'as'),
    (re.compile(r'\bswitch\s*\{'), 'switch 式'),
    (re.compile(r'\bnew\s*\(\s*\)'), '型を省いた new()'),
    (re.compile(r'\b(goto|lock|checked)\b'), 'goto/lock/checked'),
    (re.compile(r'^\s*((public|private|protected|internal)\s+)*static\s+(readonly\s+)?[\w.\[\]<>,]+\s+\w+\s*(=|;)'), 'static field'),
    (re.compile(r'\[\s*,'), '多次元配列'),
]

TYPE_DECL = re.compile(r'\b(class|struct|enum)\s+\w')


def strip(line, state):
    """コメントと文字列の中身を外す。state['block'] は /* */ の中にいるか"""
    out = []
    i = 0
    while i < len(line):
        if state['block']:
            end = line.find('*/', i)
            if end < 0:
                return ''.join(out)
            state['block'] = False
            i = end + 2
            continue
        c = line[i]
        if line.startswith('//', i):
            break
        if line.startswith('/*', i):
            state['block'] = True
            i += 2
            continue
        if c == '"' or c == "'":
            q = c
            i += 1
            while i < len(line) and line[i] != q:
                i += 2 if line[i] == '\\' else 1
            out.append(q + q)
            i += 1
            continue
        out.append(c)
        i += 1
    return ''.join(out)


def check(path):
    found = []
    state = {'block': False}
    depth = 0
    lines = open(path, encoding='utf-8').read().split('\n')
    # namespace があれば型の中は深さ 2 以上、無ければ 1 以上
    nested = 2 if any(re.match(r'\s*namespace\b', l) for l in lines) else 1
    for n, raw in enumerate(lines, 1):
        line = strip(raw, state)
        for pattern, reason in RULES:
            if pattern.search(line):
                found.append((n, reason))
        if depth >= nested and TYPE_DECL.search(line):
            found.append((n, '入れ子の型'))
        depth += line.count('{') - line.count('}')
    return found


def main(root):
    bad = 0
    count = 0
    for t in TARGETS:
        for d, dirs, files in os.walk(os.path.join(root, t)):
            dirs[:] = [x for x in dirs if x != 'Editor']
            for f in sorted(files):
                if not f.endswith('.cs'):
                    continue
                count += 1
                p = os.path.join(d, f)
                for n, reason in check(p):
                    bad += 1
                    print(f'{os.path.relpath(p, root)}:{n}: {reason}')
    print(f'.cs {count} ファイル、見つかったもの {bad} 件', file=sys.stderr)
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1]))
