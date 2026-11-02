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

テストは `Assets/LogStreamDev/Tests/LogStreamTests.cs` と `LogStreamTests.Gimmicks.cs` にあり、`bin/test` が Unity Test Framework の EditMode で動かす。LogStream に asmdef が無く、テストの asmdef から LogStream の型を参照できないので、型は名前で探し、Udon の値は `UdonBehaviour` の変数として読む。実行の層のテストは、テストの中で Play Mode に入り、ClientSim の 1 人のプレイヤーで動かす。ログはテスト用の U# スクリプト `Assets/LogStreamDev/TestUdon/LogStreamTestWriter.cs` で足す。

| テスト | 確かめる振る舞い | 落ちることを確かめた変更 |
| --- | --- | --- |
| `BuildProcessor_CopiesInspectorValuesToUdon` | ビルドのときに、`LogStreamCore` の音の ON/OFF と音量、`LogJoinLeave` のメッセージが子の Udon と `VRCSpatialAudioSource` に写り、`Id` で `_core` が結ばれる | `LogStreamCoreEditor` の音の ON/OFF を写す行、音量を写す行を外す |
| `AddMessage_ShiftsAndDropsOldest` | ログを足すと配列の先頭に入り、古いものは後ろへずれ、`_logLimit` を超えると最も古いものが落ちる | `Runtime/LogStreamCore.cs` の `AddMessage` のずらすループの本文の行を外す |
| `Viewer_FiltersByLogType` | `LogStreamViewer` は選んだ `LogType` のログだけを描き、`None` ではすべてを描く | `Runtime/LogStreamViewer.cs` の `LogType` の比較を外す |
| `ToggleTypeButton_FiltersAndNotifiesButtons` | 絞り込みのボタンを押すと Viewer がその `LogType` に絞り込み、別のボタンを押すと先のボタンが Viewer からの `OnChangeType` で外れる | `Runtime/LogStreamViewer.cs` の `ChangeType` の `InvokeOnChangeType()` を外す |
| `AddMessage_OwnerReceivesNetworkEvent` | owner でない人が送る `_AddMessageAsOwner` の network event を、owner が受けてログを足す | `Runtime/LogStreamCore.cs` の `_AddMessageAsOwner` ですぐ戻る |
| `AddMessage_NonOwnerDoesNotWriteLocally` | ClientSim のリモートのプレイヤーを owner にすると、自分の `AddMessage` は配列を書かず、所有権も取らない | `AddMessage` で所有権を取って自分で書く（2.x の形） |
| `AddMessage_TruncatesLongMessage` | 本文は 256 文字で切られる | `LogStreamCore.Truncate` で切らない |
| `Runtime_HasNoUpdate` | `Runtime/` に `Update`・`LateUpdate`・`FixedUpdate` が無い | `Runtime/LogLine.cs` に `Update` を足す |
| `ProjectSettings_HasVRChatLayers` | `ProjectSettings/` の Layer と衝突の行列が、VRChat SDK がビルドの前に確かめる配置（SDK の `UpdateLayers.AreLayersSetup`・`IsCollisionLayerMatrixSetup`）になっている | `ProjectSettings/TagManager.asset` と `DynamicsManager.asset` を、Layer 3 が `reserved3` だった前の版に戻す |
| `BuildProcessor_AppliesAllSettings` | 2.x の利用者の scene（`Assets/LogStreamDev/Fixtures/V2Overrides.unity`）を開いてビルドの処理に通すと、2.x の設定のクラスの全ての値が、同じ名前の Udon の field と `AudioSource`・`VRCSpatialAudioSource`・`VRCPickup`・テンプレートのボタンに移る（`_timeFormat` を含む） | `Editor/LegacySettingsMigrator.cs` で `_timeFormat` を写さない |
| `Migration_RunsOnceAndKeepsUdonEdits` | 2.x の設定を移すのは 1 度だけで、移した後に Udon で変えた値はもう 1 度移す処理を通しても戻らない | `LegacySettingsMigrator.Migrate` の `_migrated` を見る行を外す |
| `Editor_AppliesToAllSelectedTargets` | 複数を選んだ Inspector で `_id` を変えると、選んだ全ての `_core` が結ばれる | `UdonSettingsEditor.ApplyRelated` で最初の 1 つだけを扱う |
| `BuildProcessor_LinksWithinProcessedScene` | 2 つの scene を開いた状態で、どちらをビルドの処理に通しても、その scene の `LogStreamCore` に結ぶ | `LogStreamEditorUtility.GetCore` で読み込まれた全ての object から探す |
| `Pomodoro_TransitionsAndMessages` | 開始・一時停止・再開・時間切れ・スキップ・リセット・自動で続けることで状態と回数が移り、決まった文面のログが足される。4 回目の後の休憩が長い休憩になる | `Runtime/LogPomodoro.cs` の休憩の終わりのメッセージの `- 1` を外す |
| `Pomodoro_ClampsMinutesAndInterval` | 分数と長い休憩までの回数に 0 を入れても 1 に丸められ、0 で割らない | `Runtime/LogPomodoro.cs` の `Start` の分数を丸める行を外す |
| `Buttons_CallRenamedEvents` | Pomodoro と入力の prefab のボタンを押すと、`_` で始まる名前の event が呼ばれて動く | `Runtime/LogInput.cs` の `_Submit` の名前を変える |
| `JoinLeave_AddsPlayerName` | 入室・退室の event で、プレイヤーの名前を `{{name}}` に入れたログが足される | `Runtime/LogJoinLeave.cs` の退室で入室の文を使う |
| `TriggerEnterExit_AddsPlayerName` | Trigger の enter・exit の event で、同上 | `Runtime/LogTriggerEnterExit.cs` の exit で enter の文を使う |
| `Input_AddsTypedMessage` | 入力欄の文を送ると、送った人の名前でログが足される | `Runtime/LogInput.cs` で入力欄の文の代わりに固定の文を送る |
| `SampleScene_HasNoBlueprintId` | git で追跡する `Samples/LogStream.unity` の `PipelineManager` に、ワールドの ID が入っていない | `Samples/LogStream.unity` を ID の入った前の版に戻す |
| `SampleBlueprintId_KeepsIdOutOfSavedScene` | サンプルの scene を開くと `UserSettings/` に置いた ID が入り、保存したファイルからは外れ、開いている scene には残る。scene の ID が変わっていれば、保存のときに置き場へ書く | `Assets/LogStreamDev/Editor/SampleBlueprintId.cs` の、保存の前に ID を外す行・保存の後に戻す行・置き場へ書く行・開いたときに入れる行を、1 つずつ外す |

確かめていないこと:

- 複数人での同期（`[UdonSynced]`・所有権）。ClientSim では、owner でない人の event が別の端末の owner に届いて同期されることまでは確かめられないので、VRChat のクライアントで確かめる。
- 「マスターのみ」の判定。ClientSim のローカルのプレイヤーは常にマスターなので、マスターでない人の操作が効かないことは VRChat のクライアントで確かめる。
- ネットワークの時刻（`Networking.GetNetworkDateTime()`）で、端末の時計のずれが打ち消されること。

`Assets/LogStreamDev/Fixtures/V2Overrides.unity` は、2.x の prefab を置き、設定のクラスの全ての値に既定と違う上書きを入れた scene で、`Assets/LogStreamDev/Editor/V2Fixture.cs` で作った。2.x の prefab から作ったものなので、作り直さない。

`Assets/LogStreamDev/Editor/V3Assets.cs` は、3.0.0 で配る prefab と `Samples/LogStream.unity` を Unity の Editor の API で書き換えたスクリプトで、2.x の設定の値を子の Udon に揃え（`_migrated` は false のまま）、Pomodoro の表示を `View` の子の `LogPomodoroView` に分け、UI の event の名前を `_` で始まる名前にし、入力欄の文字数の上限を 256 にした。何度動かしても同じ結果になる。動かすときは `bash -c 'source bin/lib/unity.sh; run_unity "$root/Logs/v3assets.log" -quit -executeMethod Kmnk.LogStreamDev.V3Assets.Apply'` とする。

テストを足すときは、この表に行を足し、変更を外すとテストが落ちることを確かめる。

## サンプルのワールドをアップロードする

`Samples/LogStream.unity` のワールドの ID は、git で追跡しない `UserSettings/LogStreamDev/SampleBlueprintId.txt` に置く。ID はアップロードした人のアカウントのもので、配る scene に入れると、利用者がサンプルをアップロードしようとしたときに他人の ID になるため。

`Assets/LogStreamDev/Editor/SampleBlueprintId.cs` が、サンプルの scene を開くとファイルの ID を `PipelineManager` に入れ、scene を保存するときはファイルに書く分からだけ ID を外す。保存が終わると開いている scene に戻すので、VRChat SDK の Build & Upload は ID の入った `PipelineManager` を使う。ビルドしたものに ID が入るかは確かめていない。入らなくても、SDK が新しいワールドに ID を振るのはビルドの後なので、新しいワールドの最初のアップロードと同じ形になる（SDK 3.10.6 の `Packages/com.vrchat.worlds/Editor/VRCSDK/SDK3/VRCSdkControlPanelWorldBuilder.cs` で、`pM.AssignId` がビルドの後のアップロードの処理にあることを読んだ）。

- 別のマシンで初めてアップロードするときは、Unity のメニューの `Tools > LogStreamDev > Sample Blueprint ID` で ID を入れて保存する。今の ID は、VRChat のサイトのワールドのページの URL の `wrld_` で始まる部分か、ID を外す前の commit の scene（`git show 7a0d475:Assets/Kmnk/LogStream/Samples/LogStream.unity | grep blueprintId`）で分かる。
- ファイルが無いままアップロードすると、SDK が新しいワールドの ID を振り、次に scene を保存したときにファイルへ書く。
- ID を忘れさせるときは、ファイルを消して Unity を開き直す。

3.0.0 より前の commit と、2.2.3 までの release の `Samples/LogStream.unity` には ID が入っている。秘密ではないので、履歴は書き換えていない。

## UdonSharp の制約の検査

`bin/check-udonsharp`（`bin/lib/check_udonsharp.py`）は、`Assets/Kmnk/LogStream/` と `Assets/LogStreamDev/TestUdon/` の `.cs` のうち `Editor/` の下に無いものから、generic のコレクション、LINQ、`try`/`catch`/`throw`、`async`/`await`/`yield`、`?.`、`interface`、`is`・`as`、`switch` 式、`new()`、`goto`/`lock`/`checked`、static field、多次元配列、入れ子の型を探す。コメントと文字列の中身は外してから探す。ラムダ・local function・タプルは、property の式の形（`get => _x`）や通常の括弧と文字列で見分けられないので見ない。2.x の設定のクラス（通常の `MonoBehaviour`）も対象に入るが、これらの構文を使っていないので外していない。

`.editorconfig` は、波括弧・可視性・`this.`・型のキーワード・定数と private field の名前の規則を持つ。Rider・Visual Studio が読み、警告として出す。`Assets/Kmnk/Core/` は Core のリポジトリが書き方を決めるので、可視性と field の名前の規則を切っている。

## 文書の検査

`bin/check-docs`（`bin/lib/check_docs.py`）は、`AGENTS.md`・`README.md`・`docs/*.md` のバッククォートの中と Markdown のリンクから、次が今のツリーにあるかを確かめる。

- パス: リポジトリのルート、文書のディレクトリ、`Assets/Kmnk/LogStream/` からの相対か、追跡しているファイルのパスの末尾に一致するもの。`.gitignore` に当たるパス（`Logs/`・`Build/`・`UserSettings/`・SDK のパッケージ）は手元の状態で変わるので見ない
- `bin/` のコマンド
- 識別子: `_` で始まるか途中に大文字を持つ語（`LegacySettingsMigrator`・`_AddMessageAsOwner` など）が、追跡している `.cs`・`.py`・`.sh`・`.json` と `bin/` のどれかに語として出るか
- リンク: 文書からの相対のファイル。`http` と `#` だけのリンクは見ない

2.x の置き場所（`Udon/`・`Scripts/`・`Scripts/Editor/`）や SDK の型のように、意図して今のツリーに無いものを書くときは、`bin/lib/check_docs.py` の `IGNORE` に理由と一緒に足す。説明の中身が古いこと（数や振る舞いの説明）は見つけない。識別子の名前を全ての `.cs` で変える、`bin/check-core` の名前を変える、文書のリンク先を無い名前にする、のどれでも終了コード 1 になることを確かめた。

## UdonSharp の生成物

`Assets/SerializedUdonPrograms/` には UdonSharp と Udon がコンパイルした結果が入る。LogStream の `Runtime/*.asset` と `Samples/LogStream.unity` はここのファイルを GUID で指すので、指されているものだけを `.gitignore` の例外にして追跡する。SDK の sample の分は clone のたびに乱数の GUID で作られるので追跡しない。U# スクリプトを足したときに、生成物が `.gitignore` で除かれたままだと、`bin/check-assets` がそのファイルを出して終了コード 1 になる。

## SDK を上げるとき

1. `vrc-get install com.vrchat.worlds <版>` で `Packages/vpm-manifest.json` を書き換える。
2. `bin/setup` を動かす。SDK の設定の書き換えで `ProjectSettings/` に差分が出る。
3. `bin/compile` と `bin/test` を動かす。`Assets/SerializedUdonPrograms/` の生成物に差分が出る。
4. `bin/test` の `ProjectSettings_HasVRChatLayers` が落ちたときは、SDK が求める Layer の配置が変わっている。Unity の Editor で VRChat SDK の画面の Builder を開き、Setup Layers for VRChat と Set Collision Matrix を押して、`ProjectSettings/TagManager.asset` と `DynamicsManager.asset` の差分を同じ commit に入れる。
5. 2〜4 の差分を 1 つの commit にする。もう 1 度 `bin/compile` を動かして差分が出ないことを確かめる。
6. `Assets/Kmnk/LogStream/` の README の動作確認の版を書き換える。

テストと batchmode の実行は、SDK の内部に依る所が 2 つある。SDK を上げてテストが動かなくなったら、ここを見る。

- `Assets/LogStreamDev/Editor/BatchmodeHarmonyGuard.cs`: Apple silicon の Mac の batchmode では、UdonSharp の Inspector のタイトルバーの Harmony の当て込みが失敗し続けてテストが進まない。UdonSharp の内部の `_inspectorTitlePatched` を reflection で書き換えて止めている。名前が見つからなければ `[LogStreamDev] _inspectorTitlePatched not found` の警告を出す。
- テストの Play Mode: SDK 3.10.6 には ClientSim のテストの基底クラスが無いので、`ClientSimRuntimeLoader.BeginUnityTesting` で起こし、Domain Reload を切って入る。

## 文字コード

C# は BOM なしの UTF-8 と LF。UdonSharp の Create > U# Script は BOM つきで書くので、作ったら `bin/check-encoding` で見つけ、先頭の 3 byte（`EF BB BF`）を外す。`Assets/Kmnk/Core/` は Core のリポジトリが決めるので検査しない。
