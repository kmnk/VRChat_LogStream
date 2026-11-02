# SerializedUdonPrograms

UdonSharp と Udon がコンパイルした結果が入る。手で書き換えない。

- LogStream の `Runtime/*.asset` と `Samples/LogStream.unity` が GUID で指すものだけを、`.gitignore` の例外にして追跡する。
- 中身は gzip で圧縮した 16 進の文字列で、gzip のヘッダーの OS の byte が mac では `13`、Windows では `0a` になる。中身が同じでも、別の OS でコンパイルし直すと差分になる。
- そのため `.gitattributes` で、ここの `.asset` に git の clean filter `udon-gzip` を付けている。commit と `git status` のときにだけ OS の byte を `13` にそろえて比べる。ファイルそのものは書き換えない。
- filter は clone ごとに `bin/setup-git` で設定する。設定していないと `bin/check-assets` が警告を出す。

詳しくは `docs/development.md` の「UdonSharp の生成物」にある。
