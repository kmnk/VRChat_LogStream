# LogStream の作り

LogStream は、ワールドの作者が Inspector で設定する層と、VRChat の中で動く層の 2 層でできている。この文書は、2 層の役割と値の流れ、新しいギミックを足すときに手を入れる所を書く。

## 2 つの層

| 層 | 場所 | 名前空間 | 中身 |
| --- | --- | --- | --- |
| 設定の層 | `Assets/Kmnk/LogStream/Scripts/` | `Kmnk.LogStream` | 親の GameObject に付ける MonoBehaviour 6 本（`LogStreamCore`・`LogStreamViewer`・`LogInput`・`LogJoinLeave`・`LogTriggerEnterExit`・`LogPomodoro`）。値を持つだけで、`#pragma warning disable CS0414` で未使用の警告を止めている |
| 設定の層の Editor | `Assets/Kmnk/LogStream/Scripts/Editor/` | `Kmnk.LogStream` | 上の 6 本の CustomEditor（`Kmnk.Core.EditorBase<T>` の派生）と `LogStreamBuildProcessor` |
| 実行の層 | `Assets/Kmnk/LogStream/Udon/` | `Kmnk.LogStream.Udon` | UdonSharpBehaviour 14 本と、そのうち 10 本の `UdonSharpProgramAsset`（`.asset`） |

`Prefabs/` に `LogStream`・`LogStreamCore`・`LogStreamViewer`・`LogInput`・`LogJoinLeave`・`LogTriggerEnterExit`・`LogPomodoro`・`LogSample` の 8 個があり、`Samples/LogStream.unity` がそれらを置いたサンプルの scene である。

## 値の流れ

1. ワールドの作者は prefab を scene に置き、親の MonoBehaviour の Inspector で値を決める。
2. CustomEditor は、Inspector で値が変わると `ApplyModifiedProperties()` で、子の UdonSharp のコンポーネントの `SerializedObject` に値を写す。音の設定は `AudioSource` と `VRCSpatialAudioSource` にも写す。
3. 各ギミックの `Id` から `LogStreamCoreEditor.GetCore(id)` で scene の中の `LogStreamCore` を探し、その子の `Udon.LogStreamCore` を各 Udon の `_core` に入れる。同じ `Id` の `LogStreamCore` を 2 つ置くと、意図どおりに動かない。
4. `LogStreamBuildProcessor`（`IProcessSceneWithReport`）が、scene のビルドのたびに 6 種の CustomEditor の `ApplyModifiedProperties()` を呼び直す。Inspector を開かずに prefab の値を変えた場合も、ビルドで写る。

## 実行の層

- `Udon.LogStreamCore` は `BehaviourSyncMode.Manual` で、型（`LogType`）・時刻（`DateTime.UtcNow.Ticks`）・名前・本文の 4 本の `[UdonSynced]` 配列を `_logLimit`（既定 100）の長さで持つ。所有者が `Start` で配列を作る。
- `AddMessage(type, message, playerName)` は所有権を取り、4 本の配列を 1 つずつ後ろへずらして先頭に入れ、`RequestSerialization()` する。最も古いものは落ちる。
- 表示する側（`LogStreamViewer` など）は `AddEventListener` で登録し、`OnDisplayAllLogLines` で全行を描き直す。`LogStreamViewer` は `_currentType` で絞り込み、`LogType.None` のときはすべてを描く。
- 書く側は `LogWriteBase` を継承し、`AddMessage` を呼ぶ。最小の例は `Udon/LogSample.cs`。
- `LogType` は `None`・`Message`・`Notification`・`Others` の 4 つ。

## 新しいギミックを足すとき

次の 5 か所に手を入れる。

1. `Udon/` に `LogWriteBase` の派生の U# スクリプトを足し、`bin/create-program-asset` で `.asset` を作る。
2. `Scripts/` に、Inspector の値を持つ MonoBehaviour を足す。tooltip は日本語。
3. `Scripts/Editor/` に、`EditorBase<T>` の派生の CustomEditor を足し、値を子の Udon へ写す `ApplyModifiedProperties()` を書く。
4. prefab を Unity の Editor で作る。親に 2 の MonoBehaviour、子に 1 の UdonBehaviour を付ける。
5. `LogStreamBuildProcessor.OnProcessScene` に `Reapply<新しい MonoBehaviour, 新しい CustomEditor>();` の行を足す。

`Udon/` のスクリプトの制約と、資産ファイルの扱いは [AGENTS.md](../AGENTS.md) にある。

## VRChat_Core への依存

`Assets/Kmnk/Core/` は [kmnk/VRChat_Core](https://github.com/kmnk/VRChat_Core) のコピーで、LogStream は次を使う。

- `Kmnk.Core.Udon.Util`: `AmIOwner`・`TakeOwner`
- `Kmnk.Core.EditorBase<T>`: CustomEditor の基底。アクセス修飾子が無いので `internal` で、LogStream と Core のどちらにも asmdef が無く、両方が `Assembly-CSharp-Editor` に入るので継承できる
