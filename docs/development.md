# 開発の手順

LogStream を手元で開発し、変更をコマンドで確かめるための手順。リポジトリのルートが Unity プロジェクトで、`bin/` のコマンドが Unity を batchmode で動かす。

## 版

| もの | 版 | 決めている所 |
| --- | --- | --- |
| Unity | 2022.3.22f1 | `ProjectSettings/ProjectVersion.txt` |
| VRChat Worlds SDK | 3.10.6 | `Packages/vpm-manifest.json` |
| Unity Test Framework | 1.1.33 | `Packages/manifest.json` |

UdonSharp は Worlds SDK に入っているので、別に入れない。

## 用意

mac での手順。Windows で `bin/` を動かすことは確かめていない。

1. Unity Hub で Unity 2022.3.22f1 を入れる。別の場所に入れたときは、環境変数 `UNITY_EDITOR` に Unity の実行ファイルのパスを入れる。
2. `vrc-get` を入れる（`brew install vrc-get`）。Python 3 と `git` と `gh` も使う。
3. clone したリポジトリのルートで `bin/setup` を動かす。次を行う。
   - `bin/check-core` で Core のコピーを確かめる
   - `vrc-get resolve` で SDK のパッケージを `Packages/` に入れる
   - Unity を開き、VRChat SDK がエディタの更新の中で行う設定の書き換え（define など）が落ち着くまで待って保存する。保存しないと、テストの途中で再コンパイルが起きてテストが切れる
   - `bin/compile` を 1 度動かす
4. `bin/setup` の後に `bin/check-assets` と `bin/test` が終了コード 0 になれば、用意ができている。初回は Unity の import で 1 分ほどかかる。

Claude Code では、Unity を使うコマンドは sandbox の中では Unity のライセンスのクライアントが動かないので、`.claude/settings.json` の `sandbox.excludedCommands` で sandbox の外で動かす。単独で動かしたときだけ効くので、`&&` でつながない。

## コマンド

コマンドの一覧と成功・失敗の見分け方は [AGENTS.md の「コマンド」](../AGENTS.md#コマンド) にある。ログは `Logs/bin-<コマンド>.log`、テストの結果は `Logs/test-results.xml`、release は `Build/release/` に書く。どれも git で追跡しない。

## テストと振る舞い

テストは `Assets/LogStreamDev/Tests/LogStreamTests.cs` にあり、`bin/test` が Unity Test Framework の EditMode で動かす。LogStream に asmdef が無く、テストの asmdef から LogStream の型を参照できないので、型は名前で探し、Udon の値は `UdonBehaviour` の変数として読む。実行の層のテストは、テストの中で Play Mode に入り、ClientSim の 1 人のプレイヤーで動かす。ログはテスト用の U# スクリプト `Assets/LogStreamDev/TestUdon/LogStreamTestWriter.cs` で足す。

| テスト | 確かめる振る舞い | 落ちることを確かめた変更 |
| --- | --- | --- |
| `BuildProcessor_CopiesInspectorValuesToUdon` | ビルドのときに、`LogStreamCore` の音の ON/OFF と音量、`LogJoinLeave` のメッセージが子の Udon と `VRCSpatialAudioSource` に写り、`Id` で `_core` が結ばれる | `LogStreamCoreEditor` の音の ON/OFF を写す行、音量を写す行を外す |
| `AddMessage_ShiftsAndDropsOldest` | ログを足すと配列の先頭に入り、古いものは後ろへずれ、`_logLimit` を超えると最も古いものが落ちる | `Udon/LogStreamCore.cs` の `AddMessage` のずらすループの本文の行を外す |
| `Viewer_FiltersByLogType` | `LogStreamViewer` は選んだ `LogType` のログだけを描き、`None` ではすべてを描く | `Udon/LogStreamViewer.cs` の `LogType` の比較を外す |

確かめていないこと:

- 複数人での同期（`[UdonSynced]`・所有権）。ClientSim の 1 人では確かめられないので、VRChat のクライアントで確かめる。
- `LogInput`・`LogTriggerEnterExit`・`LogPomodoro` の振る舞い。

テストを足すときは、この表に行を足し、変更を外すとテストが落ちることを確かめる。

## UdonSharp の生成物

`Assets/SerializedUdonPrograms/` には UdonSharp と Udon がコンパイルした結果が入る。LogStream の `Udon/*.asset` と `Samples/LogStream.unity` はここのファイルを GUID で指すので、指されているものだけを `.gitignore` の例外にして追跡する。SDK の sample の分は clone のたびに乱数の GUID で作られるので追跡しない。U# スクリプトを足したときに、生成物が `.gitignore` で除かれたままだと、`bin/check-assets` がそのファイルを出して終了コード 1 になる。

## SDK を上げるとき

1. `vrc-get install com.vrchat.worlds <版>` で `Packages/vpm-manifest.json` を書き換える。
2. `bin/setup` を動かす。SDK の設定の書き換えで `ProjectSettings/` に差分が出る。
3. `bin/compile` と `bin/test` を動かす。`Assets/SerializedUdonPrograms/` の生成物に差分が出る。
4. 2 と 3 の差分を 1 つの commit にする。もう 1 度 `bin/compile` を動かして差分が出ないことを確かめる。
5. `Assets/Kmnk/LogStream/` の README の動作確認の版を書き換える。

テストと batchmode の実行は、SDK の内部に依る所が 2 つある。SDK を上げてテストが動かなくなったら、ここを見る。

- `Assets/LogStreamDev/Editor/BatchmodeHarmonyGuard.cs`: Apple silicon の Mac の batchmode では、UdonSharp の Inspector のタイトルバーの Harmony の当て込みが失敗し続けてテストが進まない。UdonSharp の内部の `_inspectorTitlePatched` を reflection で書き換えて止めている。名前が見つからなければ `[LogStreamDev] _inspectorTitlePatched not found` の警告を出す。
- テストの Play Mode: SDK 3.10.6 には ClientSim のテストの基底クラスが無いので、`ClientSimRuntimeLoader.BeginUnityTesting` で起こし、Domain Reload を切って入る。

## 文字コード

C# は BOM なしの UTF-8 と LF。UdonSharp の Create > U# Script は BOM つきで書くので、作ったら `bin/check-encoding` で見つけ、先頭の 3 byte（`EF BB BF`）を外す。`Assets/Kmnk/Core/` は Core のリポジトリが決めるので検査しない。
