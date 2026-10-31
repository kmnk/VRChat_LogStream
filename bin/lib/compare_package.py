"""2 つの unitypackage の項目を比べる。

旧にある項目が、新にも同じ pathname と GUID であるかを見る。無い・GUID が違う項目を出し、1 件でもあれば終了コード 1。
旧に無い項目（新しいファイル）は NEW として出すだけで、失敗にしない。
"""
import sys
import tarfile


def items(path):
    out = {}
    with tarfile.open(path, 'r:gz') as t:
        for m in t.getmembers():
            if m.name.endswith('/pathname'):
                guid = m.name.split('/')[-2]
                out[t.extractfile(m).read().decode('utf-8').splitlines()[0]] = guid
    return out


def main(new_path, old_path):
    new, old = items(new_path), items(old_path)
    bad = 0
    for p, g in sorted(old.items()):
        if p not in new:
            bad += 1
            print(f'MISSING {p}')
        elif new[p] != g:
            bad += 1
            print(f'GUID {p}: {g} -> {new[p]}')
    for p in sorted(set(new) - set(old)):
        print(f'NEW {p}')
    print(f'旧 {len(old)} 項目、新 {len(new)} 項目、違い {bad} 件', file=sys.stderr)
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1], sys.argv[2]))
