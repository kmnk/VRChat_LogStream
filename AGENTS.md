# VRChat_LogStream

VRChat のワールドに置く、テキストのログを流すギミック集。UdonSharp で書いた prefab の集まりで、unitypackage と zip で配る。リポジトリのルートが Unity 2022.3.22f1 のプロジェクトで、配るのは `Assets/Kmnk/` の下だけである。

詳しい説明は次にある。

- 作りと、新しいギミックを足すときに手を入れる所: [docs/architecture.md](docs/architecture.md)
- 環境の用意、コマンド、テストと振る舞いの対応: [docs/development.md](docs/development.md)
- release の作り方: [docs/release.md](docs/release.md)

## 構成

| パス | 中身 |
| --- | --- |
| `Assets/Kmnk/LogStream/` | LogStream 本体。配る |
| `Assets/Kmnk/Core/` | VRChat_Core のコピー。配る。直接書き換えない（下の「VRChat_Core のコピー」） |
| `Assets/LogStreamDev/` | 開発のためのエディタスクリプト、テスト、テスト用の U# スクリプト。配らない |
| `Assets/SerializedUdonPrograms/`・`Assets/UdonSharp/` | UdonSharp の生成物 |
| `bin/` | 検証・release・Core の同期のコマンド |
| `core-version.txt` | Core のコピーを写した Core の commit |

`Assets/Kmnk/` の下には LogStream と Core のほかに何も置かない。release は `Assets/Kmnk` を丸ごと書き出す。

## 作りの要約

- 設定の層: 親の GameObject の MonoBehaviour（`Scripts/`、名前空間 `Kmnk.LogStream`）が Inspector の値を持つ。CustomEditor（`Scripts/Editor/`、`Kmnk.Core.EditorBase<T>` の派生）が、値を子の UdonSharp のコンポーネントへ写す。ビルドのたびに `LogStreamBuildProcessor` が写し直す。
- 実行の層: `Udon/` の UdonSharpBehaviour（名前空間 `Kmnk.LogStream.Udon`）。`Udon.LogStreamCore` が型・時刻・名前・本文の 4 本の `[UdonSynced]` 配列を持ち、`AddMessage` で先頭に足す。書く側は `LogWriteBase` の派生、表示は `LogStreamViewer`。
- 結び方: 各ギミックは `Id` で同じ `Id` の `LogStreamCore` に結ばれる。

## 守る約束

- `Udon/` の下と `Assets/LogStreamDev/TestUdon/` は UdonSharp のスクリプトで、UdonSharp の制約に従う。`List<T>` などのジェネリックのコレクション、LINQ、ラムダ、`try/catch`、`async` を使わず、配列と `for` で書く。`bin/compile` が通っても制約を守ったことにはならない。使わないフィールドに `List<T>` を書いただけではエラーにならなかった。
- `Scripts/` と `Scripts/Editor/` は通常の C#。
- Inspector の tooltip（`[Tooltip]`）は日本語で書く。
- C# は BOM なしの UTF-8、改行は LF（`.editorconfig`）。UdonSharp の Create > U# Script は BOM つきで書くので、作ったら BOM を外す。`bin/check-encoding` で確かめる。
- asmdef は `Assets/Kmnk/` の下に足さない。足すと `internal` の `Kmnk.Core.EditorBase` に届かなくなる。

## 資産ファイル

`.prefab`・`.unity`・`.asset`・`.meta` は、テキストとして書き換えない。

- 変えるときは Unity の Editor を通す。利用者に Editor で操作してもらうか、`Assets/LogStreamDev/Editor/` のエディタスクリプトを batchmode で動かす。
- 新しい `.cs` の `.meta` は自分で書かない。`bin/compile` を動かすと Unity が作る。
- 新しい U# スクリプトの `UdonSharpProgramAsset`（`.asset`）は `bin/create-program-asset <.cs のパス>` で作る。その後 `bin/check-assets` が出す `Assets/SerializedUdonPrograms/` のファイルを `.gitignore` の例外に足す。
- Claude Code では `.claude/settings.json` が、これらのファイルの書き換えの前に利用者の確認を求める。`Bash` での書き換えは止まらないので、約束と `bin/check-assets` で守る。
- `Assets/SerializedUdonPrograms/` と `Udon/*.asset` に出た差分は、UdonSharp のスクリプトのフィールドを変えたときは `.cs` の変更と同じ commit に入れる。`.cs` を変えていないのに出た差分は戻す。SDK を上げた直後の差分は SDK を上げる commit に入れる。

## コマンド

変更したら、関わるものを動かして結果を報告する。Unity を使うコマンドは sandbox の外で、`&&` でつながずに単独で動かす（`.claude/settings.json` の `sandbox.excludedCommands` は単独で動かしたときだけ効く）。同じプロジェクトを Unity の Editor で開いているときは、Unity を使うコマンドは動かない。

| コマンド | Unity | 何をするか | 成功 | 失敗 |
| --- | --- | --- | --- | --- |
| `bin/setup` | 使う | 新しい clone を検証できる状態にする | 終了コード 0 | 各段の終了コード |
| `bin/compile` | 使う | C# と UdonSharp のコンパイル | 終了コード 0 と `Compile of <n> scripts finished` | 終了コード 1 と `error CS` か `[UdonSharp] <ファイル>(<行>,<列>)` の行 |
| `bin/test` | 使う | 振る舞いのテスト | 終了コード 0 と `Passed:` の行 | 終了コード 1 と `Failed:` の行と文面 |
| `bin/check-assets` | 使わない | prefab・scene・asset の GUID の参照の検査 | 終了コード 0 | 終了コード 1 と `ファイル:行: guid`。`bin/setup` の前は終了コード 2 |
| `bin/check-encoding` | 使わない | C# の BOM と改行の検査 | 終了コード 0 | 終了コード 1 とファイルの一覧 |
| `bin/check-core` | 使わない | Core のコピーと `core-version.txt` の比較 | 終了コード 0 | 終了コード 1 と違うファイル |
| `bin/sync-core [<commit>]` | 使わない | Core のコピーを Core の commit に合わせる | 終了コード 0 | git の終了コード |
| `bin/create-program-asset <.cs>` | 使う | U# スクリプトの `.asset` を作る | 終了コード 0 | 終了コード 1 |
| `bin/release <版>` | 使う | release の unitypackage と zip を作り、直前の release と比べる | 終了コード 0 とファイルのパス | 終了コード 1 と違う項目 |
| `bin/compare-package <新> <旧>` | 使わない | 2 つの unitypackage の項目の比較 | 終了コード 0 | 終了コード 1 と違う項目 |

どのテストがどの振る舞いを確かめるかは [docs/development.md の「テストと振る舞い」](docs/development.md#テストと振る舞い) にある。複数人での同期は ClientSim の 1 人では確かめられないので、変えたときは利用者に VRChat のクライアントでの確認を頼む。

## VRChat_Core のコピー

`Assets/Kmnk/Core/` は [kmnk/VRChat_Core](https://github.com/kmnk/VRChat_Core) のコピーで、どの commit から写したかを `core-version.txt` に持つ。直接書き換えない。Core を変えるときは Core のリポジトリに入れてから `bin/sync-core` で追いつく。追いついた後は `bin/compile`・`bin/test`・`bin/check-assets` を動かし、`Assets/Kmnk/Core/` と `core-version.txt` の変更を 1 つの commit（`Update Core to <短い hash>`）にする。`bin/sync-core` が `注意:` を出したときは、既存の利用者の手元に古いファイルが残るので、利用者に伝える。
