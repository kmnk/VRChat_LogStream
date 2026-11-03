# LogStream
VRChat のワールドに設置する、任意のテキストログを流す簡単な仕組みです。

## 必要なもの
- VRChat SDK Worlds（UdonSharp を含む）
- v3.0.0 から [kmnk/VRChat_Core](https://github.com/kmnk/VRChat_Core) は使っていません。同梱の `Kmnk/Core` は、同じ Core を使う他のギミックのために残しています

## 使い方
ログを表示するボードをワールドプロジェクトに置くまでの手順です。

0. LogStream の unitypackage を Booth や github のリポジトリの zip ファイル等から用意する
1. VRChat SDK Worlds を入れた Unity プロジェクトを用意する（UdonSharp は VRChat SDK Worlds 3.4.0 以降に含まれています）
2. 同 Unity プロジェクトに LogStream の unitypackage を Import する
3. Project タブの `Kmnk/LogStream/Prefabs` 下の `LogStream` プレハブをシーンにドラッグ＆ドロップして、好きな場所に配置する

## その他の機能の準備と使い方
- 基本的に `Kmnk/LogStream/Prefabs` 下の任意のプレハブをシーンにドラッグ＆ドロップするだけで使うことができます
- 細かい調整欄の説明は、 `Inspector` タブの各値のラベル部分にカーソルを合わせると Tips がポップアップします

### ワールドインスタンスへの Join/Leave をログへ流す
#### 準備
1. `LogJoinLeave` プレハブをシーンにドラッグ＆ドロップする（場所はどこでも大丈夫です）
2. メッセージを変更したい場合には、 `LogJoinLeave` オブジェクトを選択して、 `Join Log` と `Leave Log` を変更する
    - `{{name}}` に Join/Leave した人の名前が入ります

#### 使い方
- 設置すると自動的に動作します

### テンプレートのメッセージや入力したメッセージをログへ流す
#### 準備
1. `LogInput` プレハブをシーンにドラッグ＆ドロップして、好きな場所に配置する
2. テンプレートのメッセージを変更・追加したい場合には、 `LogInput` オブジェクトを選択して、 `Template Text` にセットしたテキストファイルを編集して、 `Use Template Text` にチェックを入れてください
3. 持ち運びを出来るようにする場合は、 `Pickupable` にチェックを入れてください

#### 使い方
- テキストの書いてあるボタンを押すと入力欄に文字が追加され、 `SEND` ボタンを押すと送信できます。 `x` ボタンを押すとキャンセルできます
- `Click To Input` を押して、直接テキストを入力して `SEND` ボタンを押すと、任意のメッセージを送信できます

### 色々な場所に同じログを表示する板を設置する
#### 準備
1. `LogStreamViewer` プレハブをシーンにドラッグ＆ドロップして、好きな場所に配置する
2. 持ち運びを出来るようにする場合は、 `Pickupable` にチェックを入れてください

#### 使い方
- 設置すると自動的に動作します

### 指定の場所への人の出入りをログに流す
#### 準備
1. `LogTriggerEnterExit` プレハブをシーンにドラッグ＆ドロップして、好きな場所に配置する
2. 設置した `LogTriggerEnterExit` オブジェクトの中にある `Udon` オブジェクトを選択する
3. `Inspector` タブに表示される、 `Box Collider` コンポーネントの `Center` や `Size` を調整して、出入りをログに流したい場所を設定する
4. メッセージを変更したい場合には、 `LogTriggerEnterExit` オブジェクトを選択して、 `Enter Log` と `Exit Log` を変更する
    - `{{name}}` に Join/Leave した人の名前が入ります

#### 使い方
- 3 で指定した場所に人が出入りすると自動的にログが流れます

### ポモドーロタイマーを使う
#### 準備
1. `LogPomodoro` プレハブをシーンにドラッグ＆ドロップして、好きな場所に配置する
2. 設置した `LogPomodoro` オブジェクトを選択して、ポモドーロのタイマー設定（ `Pomodoro` の欄）を好みの値に調整する
    - `Only Master` を ON にすると、インスタンスのマスターだけが操作できます。ゲーム内では鍵のボタンで切り替えられます
3. メッセージを変更したい場合には、 `LogPomodoro` オブジェクトを選択して、 `Option` 欄の値を調整する
4. タイマーが終わった時のサウンドを変更したい場合には、 `LogPomodoro` オブジェクトを選択して、 `Sound Effect` 欄の値を調整する

#### 使い方
- 真ん中のボタンを押すと、タイマーのスタート/ポーズが切り替わります
- 左のボタンを押すとタイマーがリセットされ、最初からになります
- 右のボタンを押すと今のタイマーがスキップされて次の状態に移ります

### 1 ワールドに複数の種類のログを表示するボードを設置する
1. `LogStream` プレハブを複数シーンにドラッグ＆ドロップして、好きな場所に配置する
2. `LogStream` オブジェクトの中にある `LogStreamCore` を選択して、分けたいログ毎に `Core` 欄の `Id` の数字を変更する
    - 他のプレハブにも同じ `Id` 設定欄があるので、グループごとに同じ `Id` を設定してください
    - `LogStreamCore` は同じ `Id` で複数設置することはできません。意図した動作をしなくなるので、注意してください

### その他
- 好きなログを流すための最低限の機能を実装したサンプルを `LogSample` プレハブと `Kmnk/LogStream/Runtime/LogSample.cs` のスクリプトに実装しています（v2 から上書きで Import した場合は `Kmnk/LogStream/Udon/LogSample.cs` にあります）
- 各プレハブの設定は、子の `Udon` オブジェクトが持っています。親のオブジェクトを選択すると、子の `Udon` オブジェクトの設定が表示されます

## 動作確認
- Unity 2022.3.22f1, VRChat SDK Base 3.10.6, VRChat SDK Worlds 3.10.6 で動作を確認しています

## License
MIT License
Copyright (c) 2024 KMNK

## 更新履歴
[CHANGELOG_ja.md](CHANGELOG_ja.md) にあります。

## クレジット
- [ICOOON MONO](https://icooon-mono.com/)
- [効果音ラボ](https://soundeffect-lab.info/)
