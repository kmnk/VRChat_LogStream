# LogStream の作り

LogStream は、VRChat の中で動く UdonSharp のスクリプトと、ワールドの作者が Inspector で設定するための Editor のコードでできている。この文書は、両者の役割と値の流れ、2.x から移すための仕組み、新しいギミックを足すときに手を入れる所を書く。

## 置き場所

| 場所 | 名前空間 | 中身 |
| --- | --- | --- |
| `Assets/Kmnk/LogStream/Runtime/` | `Kmnk.LogStream.Udon` | UdonSharpBehaviour 14 本（うち abstract 3 本）と、そのうち 11 本の `UdonSharpProgramAsset`（`.asset`）、enum 2 本（`LogType`・`PomodoroStatus`）、static クラス `ListenerArray` |
| `Assets/Kmnk/LogStream/Runtime/Legacy/` | `Kmnk.LogStream` | 2.x で設定を持っていた `MonoBehaviour` 6 本と、その基底 `LegacySettings`。値は持たず、2.x の scene から値を移すための読み出し元と、Inspector の入口として残している |
| `Assets/Kmnk/LogStream/Editor/` | `Kmnk.LogStream` | Udon の CustomEditor 6 本と基底 `UdonSettingsEditor`、`LegacySettingsEditor`、`LegacySettingsMigrator`、`LogStreamBuildProcessor`、`LogStreamEditorUtility` |

2.x から上書きで import した利用者の手元では、2.x からあるファイルは 2.x の場所（`Udon/`・`Scripts/`・`Scripts/Editor/`）のまま更新され、3.0.0 で足したファイルは、Editor のものは `Scripts/Editor/` に、`Runtime/` のものは新しい `Runtime/`・`Runtime/Legacy/` に置かれる（2026-11-02 に 2.2.3 の上に 3.0.0 を batchmode で import して確かめた）。unitypackage の import が GUID で同じアセットかを決めるためで、ファイルは `.meta` ごと動かして GUID を保っている（[release.md](release.md)）。

`Prefabs/` に `LogStream`・`LogStreamCore`・`LogStreamViewer`・`LogInput`・`LogJoinLeave`・`LogTriggerEnterExit`・`LogPomodoro`・`LogSample` の 8 個があり、`Samples/LogStream.unity` がそれらを置いたサンプルの scene である。各 prefab は、親の GameObject に 2.x の設定のクラス、子の `Udon` に UdonSharpBehaviour を持つ。`LogPomodoro` は子の `View` に表示の `LogPomodoroView` も持つ。

LogStream のコードは `Assets/Kmnk/Core/`（VRChat_Core のコピー）を参照しない。Core のコピーは、2.x の release と同じ GUID の項目を保つためと、同じ Core を使う他のギミックのために配っている。

## 設定と Inspector

- 設定の値は、子の `Udon` の UdonSharpBehaviour の `[SerializeField]` の field が持つ。音のファイル・音量・届く範囲は子の `AudioSource` と `VRCSpatialAudioSource`、持ち運びの可否は `VRCPickup` が直に持つ。
- Udon の CustomEditor（`UdonSettingsEditor` の派生）は、設定を見出しの枠ごとに描き、別のコンポーネントの欄も同じ Inspector に描く。複数を選んだ編集では、全ての選択に書く。設定が変わると `_id` から同じ scene の `LogStreamCore` を探して `_core` に結ぶ。`LogInput` はテンプレートのボタンを作り直す。
- UdonSharp は、U# の型に付いた CustomEditor を自分の `UdonSharpBehaviourOverrideEditor` に差し替え、その中から呼ぶ。Editor のコードから CustomEditor を作るときは、`Editor.CreateEditor(targets, <CustomEditor の型>)` と型を明示する。型を渡さないと UdonSharp の既定の Inspector が作られる。
- 親の 2.x の設定のクラスの Inspector（`LegacySettingsEditor`）は、自分の値を出さず、子の Udon の CustomEditor をそのまま描く。作者は 2.x と同じく親を選んで設定できる。
- `LogStreamBuildProcessor`（`IProcessSceneWithReport`）は、scene のビルドのたびに、その scene の中だけで、2.x の設定を移し、`_core` を結び、テンプレートのボタンを作り直す。

## 2.x の設定を移す

`LegacySettingsMigrator` は、2.x の設定のクラスの値を、同じ名前の Udon の field（`SerializedObject.CopyFromSerializedProperty`）と、音・pickup のコンポーネントへ写し、`LegacySettings._migrated` を true にする。1 度移した後に Udon で変えた値は戻さない。

走るのは、scene を開いた時、Editor の起動とスクリプトの再読み込みの後、ビルドの時、親の Inspector を開いた時である。配る prefab では、2.x の設定のクラスの値を Udon の値と同じにして `_migrated` を false のままにしてあるので、新しく置いたインスタンスでも 1 度走るが、同じ値を写すだけになる。prefab の値は `Assets/LogStreamDev/Editor/V3Assets.cs` で揃えた。

2.x の設定のクラスを prefab から外さないのは、外すと利用者の scene のインスタンスが持つ値の上書きを移す前に読めなくなるからである。外すのは、2.x の scene が残らないと言える次の major の版にする。

## 実行の層

- `LogStreamCore` は `BehaviourSyncMode.Manual` で、型（`LogType`）・時刻・名前・本文の 4 本の `[UdonSynced]` 配列を `_logLimit`（既定 100、1〜200）の長さで持つ。owner が `Start` で配列を作る。
- `AddMessage(type, message, playerName)` は、本文を 256 文字、名前を 64 文字で切る。owner なら配列を 1 つずつ後ろへずらして先頭に入れ、`RequestSerialization()` する。owner でなければ `[NetworkCallable]` の `_AddMessageAsOwner` を owner に送る。所有権は動かさないので、同時に足されても owner が順に足す。時刻は `Networking.GetNetworkDateTime()` で決める。
- 表示する側は `LogStreamEventListener`（abstract）を継承して `OnDisplayAllLogLines` を override し、`AddEventListener` で登録する。`LogStreamViewer` は `_currentType` で絞り込み、`LogType.None` のときはすべてを描く。絞り込みの変化は `LogStreamViewerEventListener` を継承した `LogStreamToggleTypeButton` に `OnChangeType` で伝える。受け手が基底を継承して override するのは、UdonSharp が別の型の引数つきのメソッドを呼ぶとき、基底のメソッドの名前で変数と event の名前を決めるからである。
- 書く側は `LogWriteBase`（abstract）を継承し、`AddMessage` か `AddPlayerMessage`（`{{name}}` をプレイヤーの名前にする）を呼ぶ。最小の例は `Runtime/LogSample.cs`。
- `LogPomodoro` は状態・回数・終わりの時刻・一時停止の残り・Master Only・Auto Continue を同期し、状態が移るたびにログを足す。1 秒ごとの処理は `SendCustomEventDelayedSeconds` で自分を呼び直す `_Tick` で行う。表示は `LogPomodoroView.Show`・`ShowTimer` に値を渡して描かせる。Master Only の間は、インスタンスのマスターだけが操作でき、`OnOwnershipRequest` でマスター以外への所有権の移動を拒む。
- UI から呼ぶメソッドは名前を `_` で始め、network から呼べないようにしている。`Runtime/` に毎フレーム動く `Update` は置かない。
- `LogType` は `None`・`Message`・`Notification`・`Others` の 4 つ。

## 新しいギミックを足すとき

次の 4 か所に手を入れる。

1. `Runtime/` に `LogWriteBase` の派生の U# スクリプトを足し、`bin/create-program-asset` で `.asset` を作る。設定は `[SerializeField]` の field に、日本語の `[Tooltip]` を付けて持つ。UI から呼ぶメソッドの名前は `_` で始める。
2. `Editor/` に `UdonSettingsEditor` の派生の CustomEditor を足し、`DrawSettings` で設定を描く。
3. prefab を Unity の Editor で作る。親の GameObject の下に `Udon` という子を置き、1 の UdonBehaviour を付ける。新しいギミックには 2.x の設定のクラスは要らない。
4. `docs/development.md` のテストの表に行を足し、振る舞いのテストを足す。

`Runtime/` のスクリプトの制約と、資産ファイルの扱いは [AGENTS.md](../AGENTS.md) にある。
