"""Assets/ の下の .prefab・.unity・.asset の guid の参照が、既知の GUID に解決するかを確かめる。

既知の GUID は、Assets/・Packages/・Library/PackageCache/ の .meta の guid と、Unity の組み込みの資産の GUID。
解決しない参照を「ファイル:行: guid」で出し、1 件でもあれば終了コード 1。
Assets/Kmnk と Assets/LogStreamDev の資産が指す Assets/SerializedUdonPrograms の生成物が .gitignore で除かれていれば、
それも出して終了コード 1（clone のたびに GUID が変わり、資産の参照が書き換わるため）。
SDK のパッケージが入っていない（bin/setup の前）ときは終了コード 2。
"""
import os
import re
import subprocess
import sys

BUILTIN = {'0000000000000000e000000000000000', '0000000000000000f000000000000000'}
GUID_LINE = re.compile(r'^guid: ([0-9a-f]{32})\s*$')
REF = re.compile(r'guid: ([0-9a-f]{32})')
PROGRAM_REF = re.compile(r'serialized(?:Udon)?ProgramAsset: \{fileID: \d+, guid: ([0-9a-f]{32})')
OWN = (os.path.join('Assets', 'Kmnk'), os.path.join('Assets', 'LogStreamDev'))
ASSET_EXT = ('.prefab', '.unity', '.asset')


def known_guids(root):
    """GUID から .meta のパス（組み込みは None）への対応を返す"""
    known = dict.fromkeys(BUILTIN)
    for base in ('Assets', 'Packages', os.path.join('Library', 'PackageCache')):
        for d, _, files in os.walk(os.path.join(root, base)):
            for f in files:
                if not f.endswith('.meta'):
                    continue
                with open(os.path.join(d, f), encoding='utf-8', errors='replace') as fh:
                    for line in fh:
                        m = GUID_LINE.match(line)
                        if m:
                            known[m.group(1)] = os.path.relpath(os.path.join(d, f), root)
                            break
    return known


def main(root, targets):
    if not os.path.isdir(os.path.join(root, 'Packages', 'com.vrchat.worlds')) \
            or not os.path.isdir(os.path.join(root, 'Library', 'PackageCache')):
        print('SDK のパッケージが入っていないので、SDK の参照を解決できない。先に bin/setup を動かす', file=sys.stderr)
        return 2
    known = known_guids(root)
    bad = 0
    for t in targets:
        for d, _, files in os.walk(os.path.join(root, t)):
            for f in sorted(files):
                if not f.endswith(ASSET_EXT):
                    continue
                p = os.path.join(d, f)
                with open(p, encoding='utf-8', errors='replace') as fh:
                    for n, line in enumerate(fh, 1):
                        for g in REF.findall(line):
                            if g not in known:
                                bad += 1
                                print(f'{os.path.relpath(p, root)}:{n}: {g}')
    bad += check_programs_tracked(root, known)
    print(f'既知の GUID {len(known)} 個、見つかった問題 {bad} 件', file=sys.stderr)
    return 1 if bad else 0


def check_programs_tracked(root, known):
    paths = []
    for t in OWN:
        for d, _, files in os.walk(os.path.join(root, t)):
            for f in sorted(files):
                if f.endswith(ASSET_EXT):
                    with open(os.path.join(d, f), encoding='utf-8', errors='replace') as fh:
                        for g in PROGRAM_REF.findall(fh.read()):
                            meta = known.get(g)
                            if meta:
                                paths += [meta[:-len('.meta')], meta]
    if not paths:
        return 0
    ignored = subprocess.run(['git', '-C', root, 'check-ignore', '--no-index', *paths],
                             capture_output=True, text=True).stdout.split()
    for p in ignored:
        print(f'{p}: 資産が指す Udon の生成物が .gitignore で除かれている。.gitignore の例外に足す')
    return len(ignored)


if __name__ == '__main__':
    sys.exit(main(sys.argv[1], sys.argv[2:] or ['Assets']))
