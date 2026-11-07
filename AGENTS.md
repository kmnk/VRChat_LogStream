# VRChat_LogStream

VRChat のワールドに置く、テキストのログを流すギミック集。UdonSharp で書いた prefab の集まりで、VPM のパッケージ `dev.kmnk.log-stream`（listing は kmnk/vpm-repos）と unitypackage で配る。リポジトリのルートが Unity 2022.3.22f1 のプロジェクトで、配るのは `Packages/dev.kmnk.log-stream/` の下だけである。

詳しい説明は次にある。

- 作りと、ギミックを足す・変えるときに手を入れる所: [docs/architecture.md](docs/architecture.md)
- 環境の用意、コマンド、テストと振る舞いの対応: [docs/development.md](docs/development.md)
- release の作り方: [docs/release.md](docs/release.md)

## 構成

| パス | 中身 |
| --- | --- |
| `Packages/dev.kmnk.log-stream/` | LogStream 本体。VPM のパッケージ（`package.json`）として配る |
| `Assets/Kmnk/Core/` | VRChat_Core のコピー。3.0.0 から配らない（LogStream は使わない）。直接書き換えない（下の「VRChat_Core のコピー」） |
| `Assets/LogStreamDev/` | 開発のためのエディタスクリプト、テスト、テスト用の U# スクリプト。配らない |
| `Assets/SerializedUdonPrograms/`・`Assets/UdonSharp/` | UdonSharp の生成物 |
| `bin/` | 検証・release・Core の同期のコマンド |
| `core-version.txt` | Core のコピーを写した Core の commit |

配るのは `Packages/dev.kmnk.log-stream/` の中身だけで、release はこのフォルダを VPM の zip と unitypackage にする。2.x までの置き場所 `Assets/Kmnk/LogStream/` は、`package.json` の `legacyFolders` で VCC・ALCOM が利用者の手元から消す。

## 作りの要約

- 設定: 各 prefab の子の `Udon` の UdonSharpBehaviour（`Runtime/`、名前空間 `Kmnk.LogStream.Udon`）が持つ。CustomEditor（`Editor/`、`UdonSettingsEditor` の派生）が設定を描き、複数の選択に書く。
- 外側のコンポーネント: 各 prefab の親の GameObject は、`GimmickRoot`（`Runtime/Root/`、名前空間 `Kmnk.LogStream`）の派生の `MonoBehaviour` を持つ。値は持たず、子の Udon の設定を描く入口・tips・Unity の Editor の上での小さな処理を受け持つ（`Editor/GimmickRootEditor.cs`）。
- 2.x の設定: 3.0.0 からある prefab の外側（`Runtime/Legacy/`）は `GimmickRoot` の派生の `LegacySettings` の派生で、2.x の scene の値を子の Udon へ 1 度だけ移す読み出し元（`LegacySettingsMigrator`）も兼ねる。新しい値を足さない。
- 実行: `Udon.LogStreamCore` が型・時刻・名前・本文の 4 本の `[UdonSynced]` 配列を持つ。owner だけが書き、owner でない人のログは `[NetworkCallable]` の event で owner に送る。書く側は `LogWriteBase` の派生、表示は `LogStreamEventListener` の派生の `LogStreamViewer`。
- 結び方: 各ギミックは `_id` で同じ scene の同じ `_id` の `LogStreamCore` に結ばれる。ビルドのたびに `LogStreamBuildProcessor` が結び直す。
- 詳しくは [docs/architecture.md](docs/architecture.md)。

## 守る約束

- `Runtime/` の下と `Assets/LogStreamDev/TestUdon/` は UdonSharp のスクリプトで、UdonSharp の制約に従う。`List<T>` などのジェネリックのコレクション、LINQ、ラムダ、`try/catch`、`async` を使わず、配列と `for` で書く。`bin/compile` が通っても制約を守ったことにはならない。使わないフィールドに `List<T>` を書いただけではエラーにならなかった。`bin/check-udonsharp` が文字列の照合で補う。
- `Runtime/Legacy/` と `Editor/` は通常の C#。
- Inspector の tooltip（`[Tooltip]`）は日本語で書く。
- C# は BOM なしの UTF-8、改行は LF（`.editorconfig`）。UdonSharp の Create > U# Script は BOM つきで書くので、作ったら BOM を外す。`bin/check-encoding` で確かめる。
- asmdef は `Runtime/` の `Kmnk.LogStream.Runtime` と `Editor/` の `Kmnk.LogStream.Editor` の 2 つ。`Runtime/` の U# スクリプトは、`Runtime/Kmnk.LogStream.Runtime.asset`（U# Assembly Definition）で UdonSharp に結ぶ。無いと UdonSharp が `does not belong to a U# assembly` で止まる。asmdef を足すときは、隣に U# Assembly Definition を `Kmnk.LogStreamDev.Verify.CreateUdonSharpAssemblyDefinition` で作る。
- 2.x を unitypackage で入れた利用者が unitypackage で更新するときは、先に `Assets/Kmnk/LogStream` を消してもらう。消さずに入れると、2.x と同じ GUID のファイルが元の場所に残って asmdef に入らず、コンパイルが壊れる（`bin/check-install` の `over-v2-unitypackage` が確かめる形）。利用者向けの README にこの手順を書く。
- UI から呼ぶメソッドの名前は `_` で始める（network から呼べないようにするため）。`Runtime/` に `Update` を置かず、1 秒ごとの処理は `SendCustomEventDelayedSeconds` で行う。

## 資産ファイル

`.prefab`・`.unity`・`.asset`・`.meta` は、テキストとして書き換えない。

- 変えるときは Unity の Editor を通す。利用者に Editor で操作してもらうか、`Assets/LogStreamDev/Editor/` のエディタスクリプトを batchmode で動かす。
- 新しい `.cs` の `.meta` は自分で書かない。`bin/compile` を動かすと Unity が作る。
- 新しい U# スクリプトの `UdonSharpProgramAsset`（`.asset`）は `bin/create-program-asset <.cs のパス>` で作る。その後 `bin/check-assets` が出す `Assets/SerializedUdonPrograms/` のファイルを `.gitignore` の例外に足す。
- Claude Code では `.claude/settings.json` が、これらのファイルの書き換えの前に利用者の確認を求める。`Bash` での書き換えは止まらないので、約束と `bin/check-assets` で守る。
- `Assets/SerializedUdonPrograms/` と `Runtime/*.asset` に出た差分は、UdonSharp のスクリプトのフィールドを変えたときは `.cs` の変更と同じ commit に入れる。`.cs` を変えていないのに出た差分は戻す。SDK を上げた直後の差分は SDK を上げる commit に入れる。

## コマンド

変更したら、関わるものを動かして結果を報告する。Unity を使うコマンドは sandbox の外で、`&&` でつながずに単独で動かす（`.claude/settings.json` の `sandbox.excludedCommands` は単独で動かしたときだけ効く）。同じプロジェクトを Unity の Editor で開いているときは、Unity を使うコマンドは動かない。

| コマンド | Unity | 何をするか | 成功 | 失敗 |
| --- | --- | --- | --- | --- |
| `bin/setup` | 使う | 新しい clone を検証できる状態にする | 終了コード 0 | 各段の終了コード |
| `bin/setup-git` | 使わない | clone ごとの git の設定（UdonSharp の生成物の filter）を入れる | 終了コード 0 | 0 以外の終了コード |
| `bin/compile` | 使う | C# と UdonSharp のコンパイル | 終了コード 0 と `Compile of <n> scripts finished` | 終了コード 1 と `error CS` か `[UdonSharp] <ファイル>(<行>,<列>)` の行 |
| `bin/test` | 使う | 振る舞いのテスト | 終了コード 0 と `Passed:` の行 | 終了コード 1 と `Failed:` の行と文面 |
| `bin/check-assets` | 使わない | prefab・scene・asset の GUID の参照の検査 | 終了コード 0 | 終了コード 1 と `ファイル:行: guid`。`bin/setup` の前は終了コード 2 |
| `bin/check-encoding` | 使わない | C# の BOM と改行の検査 | 終了コード 0 | 終了コード 1 とファイルの一覧 |
| `bin/check-udonsharp` | 使わない | Udon で動く C# から UdonSharp が受け付けない構文を文字列で探す | 終了コード 0 | 終了コード 1 と `ファイル:行: 理由` |
| `bin/check-docs` | 使わない | `AGENTS.md`・`README.md`・`docs/` が指すパス・識別子・コマンド・リンクが今のツリーにあるかの検査 | 終了コード 0 | 終了コード 1 と `ファイル:行: 名前（理由）` |
| `bin/check-core` | 使わない | Core のコピーと `core-version.txt` の比較 | 終了コード 0 | 終了コード 1 と違うファイル |
| `bin/sync-core [<commit>]` | 使わない | Core のコピーを Core の commit に合わせる | 終了コード 0 | git の終了コード |
| `bin/create-program-asset <.cs>` | 使う | U# スクリプトの `.asset` を作る | 終了コード 0 | 終了コード 1 |
| `bin/compare-package [--ignore <パス>] [--ignore-item <pathname>] <新> <旧>` | 使わない | 2 つの unitypackage の項目の比較（[docs/release.md](docs/release.md)） | 終了コード 0 | 終了コード 1 と違う項目 |
| `bin/check-install <zip か unitypackage> <場合>` | 使う | 配るパッケージを使い捨てのプロジェクトに利用者と同じ入り方で入れ、コンパイル・scene の参照・2.x の設定の移し替えを確かめる | 終了コード 0 と `成功` | 終了コード 1 と `NG` の行 |

どのテストがどの振る舞いを確かめるかは [docs/development.md の「テストと振る舞い」](docs/development.md#テストと振る舞い) にある。配るパッケージの形・`package.json`・asmdef・置き場所を変えたときは、`bin/test` の後に `bin/check-install` で確かめる（[docs/development.md の「配るパッケージを入れて確かめる」](docs/development.md#配るパッケージを入れて確かめる)）。複数人での同期は ClientSim の 1 人では確かめられないので、変えたときは利用者に VRChat のクライアントでの確認を頼む。

## 文書を直す

振る舞い・設定・ファイルの置き場所・コマンドを変えたら、同じ commit の中で、関わる `AGENTS.md`・`docs/`・利用者向けの README（`Packages/dev.kmnk.log-stream/README_ja.md` と `README.md`）を直す。どこを直すかは [docs/architecture.md](docs/architecture.md) の「新しいギミックを足すとき」「既存のギミックに設定や機能を足すとき」にある。利用者に見える変更（振る舞い・設定・prefab・イベントの名前・入れ方）を入れる commit では、`Packages/dev.kmnk.log-stream/CHANGELOG.md`（英語）と `CHANGELOG_ja.md`（日本語）の `## [Unreleased]` に、利用者向けの 1 行を同じ commit で足す（[Keep a Changelog](https://keepachangelog.com/en/1.0.0/) の形）。release の前に版を上げる commit で、`[Unreleased]` を `## [<版>] - <日付>` にする。直したら `bin/check-docs` を動かす。`bin/check-docs` は名前が消えたことしか見つけないので、説明の中身は自分で読み直す。文書とコードが違うのに気づいたら、コードを正として文書を直すか、利用者に伝える。

## VRChat_Core のコピー

`Assets/Kmnk/Core/` は [kmnk/VRChat_Core](https://github.com/kmnk/VRChat_Core) のコピーで、どの commit から写したかを `core-version.txt` に持つ。直接書き換えない。Core を変えるときは Core のリポジトリに入れてから `bin/sync-core` で追いつく。追いついた後は `bin/compile`・`bin/test`・`bin/check-assets` を動かし、`Assets/Kmnk/Core/` と `core-version.txt` の変更を 1 つの commit（`Update Core to <短い hash>`）にする。`bin/sync-core` が `注意:` を出したときは、既存の利用者の手元に古いファイルが残るので、利用者に伝える。
