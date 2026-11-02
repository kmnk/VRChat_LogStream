"""開発者向けの文書が指すパス・識別子・リンクが、今のツリーにあるかを確かめる。無いものを
ファイル:行: 名前（理由） で出し、1 件でもあれば終了コード 1。

対象は AGENTS.md・README.md・docs/*.md。バッククォートの中と Markdown のリンクを見る。

- パス（/ か拡張子で終わるもの、または最初の部分がルートにある / を含むもの）: リポジトリのルート、文書のディレクトリ、Assets/Kmnk/LogStream/
  からの相対のどれかにあるか、追跡しているファイルのパスの末尾に一致すればよい。.gitignore に当たる
  パス（Logs/・Build/・UserSettings/・SDK のパッケージ）は、手元の状態で変わるので見ない
- bin/ のコマンド: 最初の語が bin/ の下にあればよい
- 識別子（_ で始まるか、途中に大文字を持つ語。. でつないだものは各部分）: 追跡している .cs・.py・
  .sh・bin/ のどれかに語として出ればよい
- リンク: # より前のファイルが、文書のディレクトリからの相対であればよい。http は見ない

説明の中身が古いことは見つけられない。意図して今のツリーに無いものを書くときは IGNORE に足す。
"""
import os
import re
import subprocess
import sys

# 意図して今のツリーに無いもの。2.x のときの置き場所で、2.x から上書きした利用者の手元に残る場所として書いている
IGNORE = {
    'Udon/': '2.x の置き場所',
    'Scripts/': '2.x の置き場所',
    'Scripts/Editor/': '2.x の置き場所',
    'UdonSharpBehaviourOverrideEditor': 'VRChat SDK（UdonSharp）の型。SDK は追跡しない',
    'pM.AssignId': 'VRChat SDK のコードの式',
}

EXTENSIONS = ('.cs', '.md', '.py', '.sh', '.prefab', '.unity', '.asset', '.asmdef', '.txt', '.json',
              '.meta', '.unitypackage', '.zip', '.xml', '.log', '.png')
CODE_SPAN = re.compile(r'`([^`]+)`')
LINK = re.compile(r'\]\(([^)\s]+)\)')
IDENT = re.compile(r'^[A-Za-z_][A-Za-z0-9_]*$')


def tracked(root):
    out = subprocess.run(['git', '-C', root, 'ls-files'], capture_output=True, text=True, check=True).stdout
    return out.splitlines()


def doc_files(root):
    docs = [p for p in ('AGENTS.md', 'README.md') if os.path.exists(os.path.join(root, p))]
    d = os.path.join(root, 'docs')
    if os.path.isdir(d):
        docs += [os.path.join('docs', f) for f in sorted(os.listdir(d)) if f.endswith('.md')]
    return docs


def is_identifier(word):
    if not IDENT.match(word):
        return False
    return word.startswith('_') or re.search(r'[a-z0-9][A-Z]', word) is not None


def ignored(root, path):
    return subprocess.run(['git', '-C', root, 'check-ignore', '-q', path]).returncode == 0


def path_exists(span, doc_dir, root, files, dirs):
    p = span.rstrip('/')
    if ignored(root, span):
        return True
    for base in ('', doc_dir, os.path.join('Assets', 'Kmnk', 'LogStream')):
        if os.path.exists(os.path.join(root, base, p)):
            return True
    if span.endswith('/'):
        return any(d == p or d.endswith('/' + p) for d in dirs)
    return any(f == p or f.endswith('/' + p) for f in files)


def check_span(span, doc_dir, root, files, dirs, words):
    """問題があれば理由を返す。見ないもの・問題の無いものは None"""
    span = span.strip()
    if not span or span in IGNORE:
        return None
    if span.startswith('bin/'):
        cmd = span.split()[0]
        return None if os.path.exists(os.path.join(root, cmd)) else 'コマンドが無い'
    if ' ' in span or any(c in span for c in '<>*{}$|=(),;') or span.startswith(('-', '.', '@', '~', '#')):
        return None
    if span.endswith(('/',) + EXTENSIONS) or '/' in span and os.path.exists(os.path.join(root, span.split('/')[0])):
        return None if path_exists(span, doc_dir, root, files, dirs) else 'パスが無い'
    parts = span.split('.')
    if all(is_identifier(x) or IDENT.match(x) for x in parts) and any(is_identifier(x) for x in parts):
        missing = [x for x in parts if is_identifier(x) and x not in words]
        return f'識別子が無い: {", ".join(missing)}' if missing else None
    return None


def main(root):
    files = tracked(root)
    dirs = {os.path.dirname(f) for f in files}
    dirs |= {d.rsplit('/', i)[0] for d in list(dirs) for i in range(1, d.count('/') + 1)}
    words = set()
    for f in files:
        if f.endswith(('.cs', '.py', '.sh', '.json')) or f.startswith('bin/'):
            try:
                words |= set(re.findall(r'[A-Za-z_][A-Za-z0-9_]*', open(os.path.join(root, f), encoding='utf-8').read()))
            except (UnicodeDecodeError, IsADirectoryError, FileNotFoundError):
                pass
    bad = 0
    checked = 0
    for doc in doc_files(root):
        doc_dir = os.path.dirname(doc)
        fenced = False
        for n, line in enumerate(open(os.path.join(root, doc), encoding='utf-8'), 1):
            if line.lstrip().startswith('```'):
                fenced = not fenced
                continue
            if fenced:
                continue
            for span in CODE_SPAN.findall(line):
                checked += 1
                reason = check_span(span, doc_dir, root, files, dirs, words)
                if reason:
                    bad += 1
                    print(f'{doc}:{n}: {span}（{reason}）')
            for target in LINK.findall(CODE_SPAN.sub('', line)):
                if target.startswith(('http://', 'https://', '#', 'mailto:')):
                    continue
                checked += 1
                path = target.split('#')[0]
                if not os.path.exists(os.path.normpath(os.path.join(root, doc_dir, path))):
                    bad += 1
                    print(f'{doc}:{n}: {target}（リンク先が無い）')
    print(f'文書 {len(doc_files(root))} ファイル、参照 {checked}、無いもの {bad}', file=sys.stderr)
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1]))
