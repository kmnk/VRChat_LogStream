# release の作り方

release の unitypackage と zip を作り、GitHub の Release と Booth に上げる手順。作るのは `bin/release` で、上げるのは手で行う。

## 前提

- mac で、[development.md](development.md) の手順で `bin/setup` を済ませている。
- `gh` で GitHub にログインしている（直前の release を取るため）。
- 同じプロジェクトを Unity の Editor で開いていない。

## 手順

1. `Assets/Kmnk/LogStream/README.md` と `README_ja.md` の更新履歴（Updates・更新履歴）に版と変更を足し、動作確認（Operation check・動作確認）の版を `bin/compile` と `bin/test` で確かめた Unity と SDK の版にする。commit する。
2. `bin/release <版>`（例 `bin/release 2.2.4`）を動かす。次を行い、終了コード 0 なら `Build/release/` に 2 つのファイルができる。
   - `bin/check-core` で、Core のコピーが `core-version.txt` の commit と同じかを確かめる
   - `Assets/Kmnk` を丸ごと `LogStream_v<版の . を _ にしたもの>.unitypackage` に書き出す
   - その unitypackage と `README.md`・`README_ja.md`・`LICENSE.md` を `VRChat_LogStream_v<版の . を _ にしたもの>.zip` に固める
   - GitHub の直前の release の unitypackage を取り、`bin/compare-package` で比べる。直前の release にある項目が同じ `pathname` と GUID で無ければ、終了コード 1 で止まる。既存の利用者が上書きで import できなくなるからである
3. 終了コード 1 で `GUID` や `MISSING` が出たときは、上げずに原因を調べる。Core の同期で GUID が変わったときは、Core の側で `.meta` を戻すかを利用者が決める。
4. `git tag v<版>` を付けて push し、GitHub の Release を作って 2 つのファイルを上げる。
5. Booth に zip を上げる。

直前の release を手元のファイルで比べるときは、`PREVIOUS_PACKAGE=<unitypackage のパス> bin/release <版>` とする。

## Windows の Editor から書き出すとき

`bin/` は mac でだけ確かめている。Windows では、Unity の Editor のメニュー Tools > LogStreamDev > Export Release Package で unitypackage を書き出せる。この場合は zip を手で作り、mac か Git Bash で `bin/compare-package <新> <直前の release の unitypackage>` を動かして比べる。
