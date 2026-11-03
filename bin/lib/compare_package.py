"""2 つの unitypackage の項目を比べる。

旧にある項目の GUID が、新にもあるかを見る。unitypackage の import は GUID で同じアセットかを決め、
既にあるアセットはそのパスのまま中身を入れ替えるので、.meta ごと動かした（GUID を保った）項目は、
既存の利用者の上書きの import を妨げない。

- MISSING: 旧の GUID が新に無い。既存の利用者の手元に古いファイルが残るので、失敗にする
- GUID: 同じ pathname で GUID が違う。既存の利用者の手元で同じパスに 2 つのアセットがぶつかるので、失敗にする
- MOVED: 同じ GUID で pathname が違う。失敗にしない
- NEW: 旧に無い GUID。失敗にしない

1 件でも失敗があれば終了コード 1。
--ignore <パス> を付けると、旧の項目のうち、そのパスとその下と、それを中に持つ親のフォルダを比べない。
3.0.0 で配らなくなった Assets/Kmnk/Core は利用者の手元に残るので、それと親の Assets/Kmnk を比べないのに使う。
--ignore-item <pathname> は、その 1 項目だけを比べない。VPM のパッケージのルートは .meta を持たないので、
2.x のルートのフォルダ Assets/Kmnk/LogStream（legacyFolders が消す）を、その下の項目は比べたまま除くのに使う。
"""
import sys
import tarfile


def items(path):
    """GUID から pathname への対応"""
    out = {}
    with tarfile.open(path, 'r:gz') as t:
        for m in t.getmembers():
            if m.name.endswith('/pathname'):
                guid = m.name.split('/')[-2]
                out[guid] = t.extractfile(m).read().decode('utf-8').splitlines()[0]
    return out


def ignored(path, prefix):
    """prefix そのものかその下、または prefix を中に持つ親のフォルダ（Assets/Kmnk/Core に対する Assets/Kmnk）なら真"""
    prefix = prefix.rstrip('/')
    return path == prefix or path.startswith(prefix + '/') or prefix.startswith(path + '/')


def main(new_path, old_path, ignore=(), ignore_items=()):
    new, old = items(new_path), items(old_path)
    skipped = {g for g, p in old.items() if any(ignored(p, i) for i in ignore) or p in ignore_items}
    old = {g: p for g, p in old.items() if g not in skipped}
    new_by_path = {p: g for g, p in new.items()}
    bad = 0
    for g, p in sorted(old.items(), key=lambda x: x[1]):
        if g in new:
            if new[g] != p:
                print(f'MOVED {p} -> {new[g]}')
        elif p in new_by_path:
            bad += 1
            print(f'GUID {p}: {g} -> {new_by_path[p]}')
        else:
            bad += 1
            print(f'MISSING {p}')
    for g, p in sorted(new.items(), key=lambda x: x[1]):
        if g not in old:
            print(f'NEW {p}')
    print(f'旧 {len(old)} 項目（比べないもの {len(skipped)}）、新 {len(new)} 項目、失敗 {bad} 件', file=sys.stderr)
    return 1 if bad else 0


if __name__ == '__main__':
    args, ignore, ignore_items = [], [], []
    it = iter(sys.argv[1:])
    for a in it:
        if a == '--ignore':
            ignore.append(next(it, ''))
        elif a == '--ignore-item':
            ignore_items.append(next(it, ''))
        else:
            args.append(a)
    if len(args) != 2 or '' in ignore or '' in ignore_items:
        print('使い方: compare_package.py [--ignore <パス>]... [--ignore-item <pathname>]... <新> <旧>', file=sys.stderr)
        sys.exit(2)
    sys.exit(main(args[0], args[1], ignore, ignore_items))
