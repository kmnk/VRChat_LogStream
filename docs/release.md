# release の作り方

3.0.0 から、release は VPM のパッケージ `dev.kmnk.log-stream` として GitHub Actions の workflow（`.github/workflows/release.yml`、Build Release）で作る。形は VRChat の template-package の `release.yml` と同じで、Unity は使わない。

## release を出す

1. 版を上げる commit を作る。`Packages/dev.kmnk.log-stream/package.json` の `version` を上げ、`CHANGELOG.md` と `CHANGELOG_ja.md` の `## [Unreleased]` を `## [<版>] - <日付>` にする。直前の tag からの commit と見比べ、利用者に見える変更の書き漏らしが無いかを見る。`main` に入れる。
2. dry run で通す: `gh workflow run release.yml -R kmnk/VRChat_LogStream -f dry_run=true`。`gh run watch` で終わりを待ち、`gh run download` で artifact を取る。dry run は tag・Release・listing を作らない。
3. artifact の zip と unitypackage を `bin/check-install` で確かめる（下の「配るものを確かめる」）。
4. 本番: `gh workflow run release.yml -R kmnk/VRChat_LogStream -f dry_run=false`。tag（`v` を付けない版、例 `3.0.0`）と Release を作り、`VPM_REPOS_TOKEN` が secrets にあれば kmnk/vpm-repos の listing を作り直す。無ければ `gh workflow run build-listing.yml -R kmnk/vpm-repos` を動かす。
5. Release の `VRChat_LogStream_v<版の . を _ にしたもの>.zip` を Booth の「最新バージョン」に手で上げる。説明文の更新履歴は更新しない。

workflow は次を順に行い、どこかで止まれば後は行わない。

- `package.json` から版を読む。本番では、同じ tag が既にあれば止まる
- `CHANGELOG.md` の版の節を Release の本文にする（`bin/lib/changelog_section.py`）。dry run で版の節がまだ無ければ `[Unreleased]` を使う
- パッケージのフォルダを VPM の zip にし、`.meta` の一覧から `pCYSl5EDgo/create-unitypackage` で unitypackage を作る
- unitypackage と `README.md`・`README_ja.md`・`LICENSE.md` を `VRChat_LogStream_v<版>.zip` に固める。`package.json` を入れないので listing には載らない
- 直前の release（pre-release を除く）の unitypackage と `bin/compare-package` で比べる

## 配るもの

- VPM の zip（`Packages/dev.kmnk.log-stream/` の中身。一番上に `package.json`）。listing（kmnk/vpm-repos）が指し、VCC・ALCOM が入れる
- unitypackage と、それを `README.md`・`README_ja.md`・`LICENSE.md` と固めた zip。ファイルで入れる人と Booth のため。入れる前に `Assets/Kmnk/LogStream` を消してもらう

## 直前の release と比べる

`bin/compare-package <新> <旧>` は、2 つの unitypackage の項目を比べる。直前の release にある項目の GUID が無い（`MISSING`）か、同じ `pathname` で GUID が違う（`GUID`）と終了コード 1 になる。消した項目は既存の利用者の手元に古いファイルとして残り、GUID の違う項目は同じパスでぶつかるからである。同じ GUID で `pathname` が違う項目は `MOVED` と出すだけで止めない。3.0.0 では置き場所が `Packages/dev.kmnk.log-stream/` に変わるので、v2.2.3 の LogStream の項目はすべて `MOVED` になる。

3.0.0 から配らない Core と、VPM のパッケージのルートが持たない 2.x のルートのフォルダは、比べる対象から除く。

```
bin/compare-package --ignore Assets/Kmnk/Core --ignore-item Assets/Kmnk/LogStream <新> <旧>
```

`--ignore <パス>` はそのパスとその下と、それを中に持つ親のフォルダ（`Assets/Kmnk`）を除く。Core は利用者の手元に残るので、消えても困らない。`--ignore-item <pathname>` はその 1 項目だけを除き、`Assets/Kmnk/LogStream` は `legacyFolders`（unitypackage では利用者）が消す。その下の項目は比べ続けるので、LogStream のファイルが消えると `MISSING` で止まる。

## 配るものを確かめる

配るものは `bin/check-install` で、使い捨てのプロジェクトに利用者と同じ入り方で入れて確かめる（[development.md の「配るパッケージを入れて確かめる」](development.md#配るパッケージを入れて確かめる)）。
