# LogStream
VRChat のワールドに設置する、任意のテキストログを流す簡単な仕組みです。

## 必要なもの
- VRChat SDK Worlds 3.10.6 以降（UdonSharp を含む）
- v3.0.0 から [kmnk/VRChat_Core](https://github.com/kmnk/VRChat_Core) は使っていません。v2 までに入れた `Assets/Kmnk/Core` は、同じ Core を使う他のギミックのためにそのまま残ります

## 入れ方
### VCC・ALCOM で入れる（おすすめ）
1. [VCC に追加](vcc://vpm/addRepo?url=https://kmnk.github.io/vpm-repos/index.json) を開くか、VCC・ALCOM の設定の Packages で `https://kmnk.github.io/vpm-repos/index.json` を追加する
2. ワールドのプロジェクトの Manage Project で `LogStream` を追加する

v2 までを unitypackage で入れていたプロジェクトでも、そのまま追加できます。古い `Assets/Kmnk/LogStream` は中身ごと自動で消え、シーンに置いたプレハブはそのまま新しい版を使います。`Assets/Kmnk/LogStream` の中に自分のシーンやファイルを置いている場合は、追加する前に `Assets` の別の場所へ移してください。

### unitypackage で入れる
1. [GitHub の Releases](https://github.com/kmnk/VRChat_LogStream/releases) か Booth から、LogStream の unitypackage を用意する
2. v2 までを入れたプロジェクトでは、先に Project タブで `Assets/Kmnk/LogStream` を消す。消さずに Import すると、スクリプトがコンパイルできなくなります（`Assets/Kmnk/Core` は消さなくて大丈夫です）
3. unitypackage を Import する

## 使い方
`Packages/LogStream/Prefabs` のプレハブをシーンにドラッグ＆ドロップすると動きます。ログを表示するには `LogStream` が 1 つ要ります。設定の項目は、Inspector のラベルにカーソルを合わせると説明が出ます。

| プレハブ | すること | 置いた後にすること |
| --- | --- | --- |
| `LogStream` | ログを表示するボード | 無し |
| `LogJoinLeave` | インスタンスへの Join/Leave をログに流す | 無し。場所はどこでもよい |
| `LogInput` | 定型文のボタンか入力した文をログに流す | 定型文を変えるときは、`Template Text` のテキストファイルを編集して `Use Template Text` に印を付ける |
| `LogTriggerEnterExit` | 指定の場所への出入りをログに流す | 子の `Udon` の `Box Collider` の `Center` と `Size` で場所を決める |
| `LogPomodoro` | ポモドーロタイマー | 無し |
| `LogStreamViewer` | 同じログを別の場所にも表示する | 無し |

- 各プレハブの設定は、子の `Udon` オブジェクトが持っています。親のオブジェクトを選択しても、子の設定が表示されます
- ポモドーロタイマーは、真ん中のボタンでスタートとポーズ、左のボタンでリセット、右のボタンで次に進みます。鍵のボタンで、インスタンスのマスターだけが操作できるかを切り替えます
- `LogInput`・`LogStreamViewer` を持ち運べるようにするときは、`Pickupable` に印を付けます

### 1 つのワールドに複数のボードを置く
1. `LogStream` を必要な数だけ置く
2. それぞれの `LogStream` の中の `LogStreamCore` で、`Core` 欄の `Id` を別の数にする
3. 他のプレハブの `Id` を、ログを流したいボードの `Id` に合わせる

同じ `Id` の `LogStreamCore` を 2 つ置くと、正しく動きません。

### 自分でログを流す
`LogSample` プレハブと `Packages/LogStream/Runtime/LogSample.cs` が、好きなログを流す最小の例です。パッケージの中のファイルは更新で置き換わるので、書き換えるときは `Assets` の下に写してから使ってください。

## 動作確認
- Unity 2022.3.22f1, VRChat SDK Base 3.10.6, VRChat SDK Worlds 3.10.6 で動作を確認しています

## License
MIT License
Copyright (c) 2026 KMNK

## 更新履歴
[CHANGELOG_ja.md](CHANGELOG_ja.md) にあります。

## クレジット
- [ICOOON MONO](https://icooon-mono.com/)
- [効果音ラボ](https://soundeffect-lab.info/)
