# release の作り方

3.0.0 から、release は VPM のパッケージ `dev.kmnk.log-stream` として GitHub Actions の workflow で作る。workflow はまだ作っている途中で、できるまでは release を出さない。

## 配るもの

- VPM の zip（`Packages/dev.kmnk.log-stream/` の中身。一番上に `package.json`）。listing（kmnk/vpm-repos）が指し、VCC・ALCOM が入れる
- unitypackage と、それを `README.md`・`README_ja.md`・`LICENSE.md` と固めた zip。ファイルで入れる人と Booth のため。入れる前に `Assets/Kmnk/LogStream` を消してもらう

## 直前の release と比べる

`bin/compare-package <新> <旧>` は、2 つの unitypackage の項目を比べる。直前の release にある項目の GUID が無い（`MISSING`）か、同じ `pathname` で GUID が違う（`GUID`）と終了コード 1 になる。消した項目は既存の利用者の手元に古いファイルとして残り、GUID の違う項目は同じパスでぶつかるからである。同じ GUID で `pathname` が違う項目は `MOVED` と出すだけで止めない。3.0.0 では置き場所が `Packages/dev.kmnk.log-stream/` に変わるので、v2.2.3 の項目はすべて `MOVED` になる。

## 配るものを確かめる

配るものは `bin/check-install` で、使い捨てのプロジェクトに利用者と同じ入り方で入れて確かめる（[development.md の「配るパッケージを入れて確かめる」](development.md#配るパッケージを入れて確かめる)）。
