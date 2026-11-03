"""bin/check-install が使う。VPM の zip から、手元の http.server で出すテスト用の listing を作る。

使い方: check_install_listing.py <VPM の zip> <出す URL の頭（例 http://127.0.0.1:8123）> <出すディレクトリ>
出すディレクトリに package.zip と index.json を置き、パッケージ名と版を 1 行ずつ出す。
"""
import hashlib
import json
import shutil
import sys
import zipfile


def main(zip_path, base_url, out_dir):
    with zipfile.ZipFile(zip_path) as z:
        if 'package.json' not in z.namelist():
            print(f'zip の一番上に package.json が無い: {zip_path}', file=sys.stderr)
            return 2
        manifest = json.loads(z.read('package.json').decode('utf-8-sig'))
    shutil.copyfile(zip_path, f'{out_dir}/package.zip')
    with open(zip_path, 'rb') as f:
        sha = hashlib.sha256(f.read()).hexdigest()
    manifest['url'] = f'{base_url}/package.zip'
    manifest['zipSHA256'] = sha
    listing = {
        'name': 'check-install',
        'id': 'local.check-install',
        'url': f'{base_url}/index.json',
        'author': 'check-install',
        'packages': {manifest['name']: {'versions': {manifest['version']: manifest}}},
    }
    with open(f'{out_dir}/index.json', 'w') as f:
        json.dump(listing, f, ensure_ascii=False, indent=2)
    print(manifest['name'])
    print(manifest['version'])
    return 0


if __name__ == '__main__':
    if len(sys.argv) != 4:
        print(__doc__, file=sys.stderr)
        sys.exit(2)
    sys.exit(main(*sys.argv[1:]))
