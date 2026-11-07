# LogStream の作り

LogStream は、VRChat の中で動く UdonSharp のスクリプトと、ワールドの作者が Inspector で設定するための Editor のコードでできている。この文書は、両者の役割と値の流れ、2.x から移すための仕組み、新しいギミックを足すときと既存のギミックを変えるときに手を入れる所を書く。

## 置き場所

| 場所 | 名前空間 | 中身 |
| --- | --- | --- |
| `Packages/dev.kmnk.log-stream/Runtime/` | `Kmnk.LogStream.Udon` | UdonSharpBehaviour 16 本（うち abstract 4 本）と、そのうち 12 本の `UdonSharpProgramAsset`（`.asset`）、enum 2 本（`LogType`・`PomodoroStatus`）、static クラス `ListenerArray` |
| `Packages/dev.kmnk.log-stream/Runtime/Root/` | `Kmnk.LogStream` | prefab の親に置く外側のコンポーネントの基底 `GimmickRoot` と、3.0.0 の後に足したギミックの外側（`PomodoroVoiceZone`）。値は持たない |
| `Packages/dev.kmnk.log-stream/Runtime/Legacy/` | `Kmnk.LogStream` | 2.x で設定を持っていた `MonoBehaviour` 6 本と、その基底 `LegacySettings`（`GimmickRoot` の派生）。値は持たず、3.0.0 からある prefab の外側と、2.x の scene から値を移すための読み出し元を兼ねる |
| `Packages/dev.kmnk.log-stream/Editor/` | `Kmnk.LogStream` | Udon の CustomEditor 7 本と基底 `UdonSettingsEditor`、外側の Inspector の `GimmickRootEditor` とその派生の `LegacySettingsEditor`・`PomodoroVoiceZoneRootEditor`、`LegacySettingsMigrator`、`LogStreamBuildProcessor`、`LogStreamEditorUtility` |

2.x から上書きで import した利用者の手元では、2.x からあるファイルは 2.x の場所（`Udon/`・`Scripts/`・`Scripts/Editor/`）のまま更新され、3.0.0 で足したファイルは、Editor のものは `Scripts/Editor/` に、`Runtime/` のものは新しい `Runtime/`・`Runtime/Legacy/` に置かれる（2026-11-02 に 2.2.3 の上に 3.0.0 を batchmode で import して確かめた）。unitypackage の import が GUID で同じアセットかを決めるためで、ファイルは `.meta` ごと動かして GUID を保っている（[release.md](release.md)）。

`Prefabs/` に `LogStream`・`LogStreamCore`・`LogStreamViewer`・`LogInput`・`LogJoinLeave`・`LogTriggerEnterExit`・`LogPomodoro`・`LogSample`・`PomodoroVoiceZone` の 9 個があり、`Samples/LogStream.unity` がそれらを置いたサンプルの scene である。各ギミックの prefab は、親の GameObject に外側のコンポーネント（`GimmickRoot` の派生）、子の `Udon` に同じ名前の UdonSharpBehaviour を持つ。3.0.0 からある 6 個の外側は 2.x の設定のクラス（`LegacySettings` の派生）で、後から足した `PomodoroVoiceZone` の外側は `GimmickRoot` を直に継ぐ。`LogStream` は他の prefab を束ねたもの、`LogSample` は親に Udon を直に置いた書き方の例で、どちらも外側を持たない。`LogPomodoro` は子の `View` に表示の `LogPomodoroView` も持つ。

LogStream のコードは `Assets/Kmnk/Core/`（VRChat_Core のコピー）を参照しない。Core のコピーはパッケージの外にあり、3.0.0 から配らない。

`Packages/dev.kmnk.log-stream/` は VPM のパッケージで、`package.json`、`Runtime/` の asmdef `Kmnk.LogStream.Runtime` と U# Assembly Definition `Kmnk.LogStream.Runtime.asset`、`Editor/` の asmdef `Kmnk.LogStream.Editor` を持つ。VCC・ALCOM で入れた利用者の手元では、2.x までの `Assets/Kmnk/LogStream/` を `package.json` の `legacyFolders` が消し、同じ GUID のファイルが `Packages/dev.kmnk.log-stream/` に置かれるので、scene の参照はパッケージの側へつながる（`bin/check-install` の `over-v2` が確かめる）。

## 設定と Inspector

- 設定の値は、子の `Udon` の UdonSharpBehaviour の `[SerializeField]` の field が持つ。音のファイル・音量・届く範囲は子の `AudioSource` と `VRCSpatialAudioSource`、持ち運びの可否は `VRCPickup` が直に持つ。
- Udon の CustomEditor（`UdonSettingsEditor` の派生）は、設定を見出しの枠ごとに描き、別のコンポーネントの欄も同じ Inspector に描く。複数を選んだ編集では、全ての選択に書く。設定が変わると `_id` から同じ scene の `LogStreamCore` を探して `_core` に結ぶ。`LogInput` はテンプレートのボタンを作り直す。
- UdonSharp は、U# の型に付いた CustomEditor を自分の `UdonSharpBehaviourOverrideEditor` に差し替え、その中から呼ぶ。Editor のコードから CustomEditor を作るときは、`Editor.CreateEditor(targets, <CustomEditor の型>)` と型を明示する。型を渡さないと UdonSharp の既定の Inspector が作られる。
- 外側のコンポーネント（`GimmickRoot`）の Inspector（`GimmickRootEditor`）は、自分の値を出さず、tips を描いた後に、子の同じ名前の Udon の CustomEditor をそのまま描く。作者は 2.x と同じく親を選んで設定できる。tips は型ごとの派生の editor が `DrawTips` を override して書く（`PomodoroVoiceZoneRootEditor` は範囲の決め方と、`Pomodoro` が空のときの警告）。2.x の設定のクラスの Inspector（`LegacySettingsEditor`）は、開いたときに 2.x の値を移す処理だけを足す。
- Unity の Editor の上での小さな処理も外側に置く。`PomodoroVoiceZone` の外側は、`OnDrawGizmos` で子の `BoxCollider` の範囲を、選んでいなくても線で描く。外側は VRChat の whitelist に無い `MonoBehaviour` で、ワールドの中では動かない前提で書く。
- `LogStreamBuildProcessor`（`IProcessSceneWithReport`）は、scene のビルドのたびに、その scene の中だけで、2.x の設定を移し、`_core` を結び、テンプレートのボタンを作り直す。

## 2.x の設定を移す

`LegacySettingsMigrator` は、2.x の設定のクラスの値を、同じ名前の Udon の field（`SerializedObject.CopyFromSerializedProperty`）と、音・pickup のコンポーネントへ写し、`LegacySettings._migrated` を true にする。1 度移した後に Udon で変えた値は戻さない。

走るのは、scene を開いた時、Editor の起動とスクリプトの再読み込みの後、ビルドの時、親の Inspector を開いた時である。配る prefab では、2.x の設定のクラスの値を Udon の値と同じにして `_migrated` を false のままにしてあるので、新しく置いたインスタンスでも 1 度走るが、同じ値を写すだけになる。prefab の値は `Assets/LogStreamDev/Editor/V3Assets.cs` で揃えた。

2.x の設定のクラスを prefab から外さないのは、外すと利用者の scene のインスタンスが持つ値の上書きを移す前に読めなくなるからである。2.x の scene が残らないと言える次の major の版で外すのは、値を移す処理（`LegacySettingsMigrator` と 2.x の field）だけで、外側のコンポーネントは残す。

## 実行の層

- `LogStreamCore` は `BehaviourSyncMode.Manual` で、型（`LogType`）・時刻・名前・本文の 4 本の `[UdonSynced]` 配列を `_logLimit`（既定 100、1〜200）の長さで持つ。owner が `Start` で配列を作る。
- `AddMessage(type, message, playerName)` は、本文を 256 文字、名前を 64 文字で切る。owner なら配列を 1 つずつ後ろへずらして先頭に入れ、`RequestSerialization()` する。owner でなければ `[NetworkCallable]` の `_AddMessageAsOwner` を owner に送る。所有権は動かさないので、同時に足されても owner が順に足す。時刻は `Networking.GetNetworkDateTime()` で決める。
- 表示する側は `LogStreamEventListener`（abstract）を継承して `OnDisplayAllLogLines` を override し、`AddEventListener` で登録する。`LogStreamViewer` は `_currentType` で絞り込み、`LogType.None` のときはすべてを描く。絞り込みの変化は `LogStreamViewerEventListener` を継承した `LogStreamToggleTypeButton` に `OnChangeType` で伝える。受け手が基底を継承して override するのは、UdonSharp が別の型の引数つきのメソッドを呼ぶとき、基底のメソッドの名前で変数と event の名前を決めるからである。
- 書く側は `LogWriteBase`（abstract）を継承し、`AddMessage` か `AddPlayerMessage`（`{{name}}` をプレイヤーの名前にする）を呼ぶ。最小の例は `Runtime/LogSample.cs`。
- `LogPomodoro` は状態・回数・終わりの時刻・一時停止の残り・Master Only・Auto Continue を同期し、状態が移るたびにログを足す。1 秒ごとの処理は `SendCustomEventDelayedSeconds` で自分を呼び直す `_Tick` で行う。表示は `LogPomodoroView.Show`・`ShowTimer` に値を渡して描かせる。Master Only の間は、インスタンスのマスターだけが操作でき、`OnOwnershipRequest` でマスター以外への所有権の移動を拒む。
- `LogPomodoro` の状態を受け取る側は `LogPomodoroEventListener`（abstract）を継承して `OnPomodoroStatusChanged` を override し、`LogPomodoro.AddEventListener` で登録する。状態が変わるたびに（owner は操作と時間切れで、他の人は `OnDeserialization` で）呼ばれ、受け手は `GetStatus`・`IsTimerRunning`（`InPomodoro` の間だけ true）で読む。
- `PomodoroVoiceZone` は `BehaviourSyncMode.None` で同期しない。`IsTimerRunning` の間だけ 1 秒ごとの `_Tick` を動かし、ローカルの player か相手の足元が `_zones` の collider の中（`Collider.ClosestPoint` が同じ点を返す）なら相手の声を変え（`SetVoiceDistanceNear`・`SetVoiceDistanceFar`・`SetVoiceVolumetricRadius` を 0、または `SetVoiceGain`）、初めて変える前の値を `playerId` ごとの `DataDictionary` に取っておいて戻す。声の値は各端末の中だけのもので、各端末が自分に聞こえる声を変える。ログは書かない。
- UI から呼ぶメソッドは名前を `_` で始め、network から呼べないようにしている。`Runtime/` に毎フレーム動く `Update` は置かない。
- `LogType` は `None`・`Message`・`Notification`・`Others` の 4 つ。

## 新しいギミックを足すとき

次の 5 か所に手を入れる（3 は外側のコンポーネントと prefab）。

1. `Runtime/` に `LogWriteBase` の派生の U# スクリプトを足し（ログを書かないギミックは別の基底でよい。`PomodoroVoiceZone` は `LogPomodoroEventListener` の派生）、`bin/create-program-asset` で `.asset` を作る。設定は `[SerializeField]` の field に、日本語の `[Tooltip]` を付けて持つ。UI から呼ぶメソッドの名前は `_` で始める。
2. `Editor/` に `UdonSettingsEditor` の派生の CustomEditor を足し、`DrawSettings` で設定を描く。`_id` と `_core` を持たないギミックは、`ApplyRelated` を何もしない形で override する。
3. `Runtime/Root/` に、1 と同じ名前の `GimmickRoot` の派生（名前空間 `Kmnk.LogStream`）を足す。tips が要るときは `Editor/` に `GimmickRootEditor` の派生を足して `DrawTips` を書く。prefab を Unity の Editor で作る。親の GameObject にこの外側のコンポーネントを付け、その下に `Udon` という子を置いて 1 の UdonBehaviour を付ける。新しいギミックには 2.x の設定のクラス（`LegacySettings`）は要らない。
4. `docs/development.md` のテストの表に行を足し、振る舞いのテストを足す。
5. 利用者向けの `Packages/dev.kmnk.log-stream/README_ja.md` と `README.md` の「使い方」（Usage）の表に、プレハブの行を足す。設定の説明は `[Tooltip]` に書き、README には Tooltip に無く設置で迷う事柄（子の `Udon` の collider で場所を決める、など）だけを書く。

## 既存のギミックに設定や機能を足すとき

例えば `LogPomodoro` に設定を 1 つ足すときは、次の 6 か所に手を入れる。

1. `Runtime/LogPomodoro.cs` に `[SerializeField]` の field を、日本語の `[Tooltip]` を付けて足す。field を変えると `Runtime/LogPomodoro.asset` と `Assets/SerializedUdonPrograms/` に差分が出るので、同じ commit に入れる。
2. `Editor/LogPomodoroEditor.cs` の `DrawSettings` に、その field を描く行を足す。
3. `Runtime/Legacy/LogPomodoro.cs`（2.x の設定のクラス）には足さない。2.x の scene に無い値なので、移す処理（`LegacySettingsMigrator`）も変えない。
4. `Prefabs/LogPomodoro.prefab` の既定の値や UI を変えるときは、Unity の Editor を通す（[AGENTS.md の「資産ファイル」](../AGENTS.md#資産ファイル)）。
5. `docs/development.md` のテストの表に行を足し、振る舞いのテストを足す。
6. 設定の説明は 1 の `[Tooltip]` に書き、README には書かない。設置の手順や Tooltip に無い注意が変わるときだけ、利用者向けの `Packages/dev.kmnk.log-stream/README_ja.md` と `README.md` の「使い方」（Usage）を直す。英語と日本語の両方を直す。

設定を足さずに振る舞いだけを変えるときは、1・5・6 のうち当たるものを行う。同期する値（`[UdonSynced]`）や所有権に触れたときは、ClientSim の 1 人では確かめられないので、利用者に VRChat のクライアントでの確認を頼む。

`Runtime/` のスクリプトの制約と、資産ファイルの扱いは [AGENTS.md](../AGENTS.md) にある。
