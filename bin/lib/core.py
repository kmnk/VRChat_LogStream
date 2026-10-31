#!/usr/bin/env python3
"""VRChat_Core のコピー（Assets/Kmnk/Core）を Core のリポジトリの commit に合わせる・比べる。bin/check-core と bin/sync-core が呼ぶ。

使い方:
  core.py check  <リポジトリのルート> <Core の git>            固定した commit とコピーを比べ、origin/main が先に進んでいれば知らせる
  core.py sync   <リポジトリのルート> <Core の git> [<commit>]  コピーを <commit>（既定は origin/main）に合わせ、固定を書き換える

固定した commit はプロジェクトのルートの core-version.txt に 1 行で持つ。
Core が追跡するファイル（.gitignore を除く）だけを写し、Core が追跡しないコピーだけのファイル
（LICENSE.md、ルートのフォルダや README・LICENSE の .meta）には触れない。
"""
import os
import subprocess
import sys

CORE_DIR = os.path.join('Assets', 'Kmnk', 'Core')
PIN_FILE = 'core-version.txt'
SKIP = {'.gitignore'}


def git(repo, *args, binary=False):
    out = subprocess.run(['git', '-C', repo, *args], check=True, capture_output=True).stdout
    return out if binary else out.decode('utf-8')


def tracked(repo, commit):
    names = git(repo, 'ls-tree', '-r', '--name-only', commit).splitlines()
    return {n for n in names if n not in SKIP}


def guid_of(text):
    for line in text.splitlines():
        if line.startswith('guid:'):
            return line.split()[1]
    return None


def read_pin(project):
    with open(os.path.join(project, PIN_FILE)) as f:
        return f.read().strip()


def compare(project, repo, commit):
    """コピーと commit の違いを (path, 理由) の一覧で返す"""
    diffs = []
    for path in sorted(tracked(repo, commit)):
        local = os.path.join(project, CORE_DIR, path)
        if not os.path.exists(local):
            diffs.append((path, 'コピーに無い'))
        elif open(local, 'rb').read() != git(repo, 'show', f'{commit}:{path}', binary=True):
            diffs.append((path, '中身が違う'))
    return diffs


def check(project, repo):
    pin = read_pin(project)
    diffs = compare(project, repo, pin)
    for path, why in diffs:
        print(f'{CORE_DIR}/{path}: {why}（固定 {pin[:7]} と）')
    if diffs:
        print('コピーを直接書き換えず、変更は Core のリポジトリに入れてから bin/sync-core で追いつく', file=sys.stderr)
    git(repo, 'fetch', '--quiet', 'origin')
    ahead = git(repo, 'rev-list', '--oneline', f'{pin}..origin/main').splitlines()
    if ahead:
        print(f'Core の origin/main は固定 {pin[:7]} より {len(ahead)} commit 先にある。bin/sync-core で追いつける:')
        for line in ahead:
            print(f'  {line}')
    else:
        print(f'Core の固定 {pin[:7]} は origin/main と同じ')
    return 1 if diffs else 0


def sync(project, repo, target):
    pin = read_pin(project)
    git(repo, 'fetch', '--quiet', 'origin')
    target = git(repo, 'rev-parse', target).strip()
    before, after = tracked(repo, pin), tracked(repo, target)
    warnings = []

    for path in sorted(after):
        data = git(repo, 'show', f'{target}:{path}', binary=True)
        local = os.path.join(project, CORE_DIR, path)
        if path.endswith('.meta') and os.path.exists(local):
            old, new = guid_of(open(local, encoding='utf-8').read()), guid_of(data.decode('utf-8'))
            if old != new:
                warnings.append(f'GUID が変わった: {path} {old} -> {new}。既存の利用者が import すると別の資産として入り、古い方が残る')
        if not path.endswith('.meta') and f'{path}.meta' not in after and not os.path.exists(local + '.meta'):
            warnings.append(f'.meta を Core が追跡していない新しいファイル: {path}。Unity が作る GUID は他のリポジトリのコピーと違う')
        os.makedirs(os.path.dirname(local), exist_ok=True)
        with open(local, 'wb') as f:
            f.write(data)
        print(f'写した: {CORE_DIR}/{path}')

    for path in sorted(before - after):
        local = os.path.join(project, CORE_DIR, path)
        if os.path.exists(local):
            os.remove(local)
            print(f'消した: {CORE_DIR}/{path}')
        if not path.endswith('.meta'):
            warnings.append(f'Core から消えた・動いた: {path}。unitypackage の import は消えたファイルを消さないので、既存の利用者の手元に残る')

    # 空になったフォルダと、その .meta（Core が追跡していないもの）を消す
    for d, dirs, files in os.walk(os.path.join(project, CORE_DIR), topdown=False):
        if d != os.path.join(project, CORE_DIR) and not os.listdir(d):
            os.rmdir(d)
            if os.path.exists(d + '.meta'):
                os.remove(d + '.meta')
            print(f'消した: {os.path.relpath(d, project)}/')

    with open(os.path.join(project, PIN_FILE), 'w') as f:
        f.write(target + '\n')
    print(f'固定を {pin[:7]} から {target[:7]} にした')
    for w in warnings:
        print(f'注意: {w}')
    return 0


if __name__ == '__main__':
    cmd, project, repo = sys.argv[1], sys.argv[2], sys.argv[3]
    if cmd == 'check':
        sys.exit(check(project, repo))
    sys.exit(sync(project, repo, sys.argv[4] if len(sys.argv) > 4 else 'origin/main'))
