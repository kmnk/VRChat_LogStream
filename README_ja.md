# MessageBoard
VRChat のワールドに設置する、任意のログテキストを流す簡単な仕組みです。

## Required
- [UdonSharp](vrchat-community/UdonSharp)
- [kmnk/VRChat_Core](https://github.com/kmnk/VRChat_Core)

## 使い方
1. UdonSharp を Import
2. LogStream の unitypackage を Import
3. Kmnk/LogStream/Prefabs 下の LogStream Prefab をシーンに配置
4. 用途に応じて後述の Prefab を追加で配置

### LogInput
テンプレートの文言や任意のテキストログを入力する機能です。
設置したい場所に prefab を配置してください。

### LogJoinLeave
ワールドへの Join/Leave をログへ流す機能です。
ワールドのどこかへ配置すれば機能します。

### LogStreamViewer
LogStream に流れるログと同じものを流す機能を持った Prefab です。
LogStream は Id 毎に1つしか設置できないですが Viewer はいくつでも設置できるので、複数箇所でログを表示したい場所がある場合に設置してください。

### LogTriggerEnter
任意のエリア内にプレーヤーが入ったことを通知する機能です。
Enter Log Format, Leave Log Format のメッセージが流れます。 `{0}` にプレーヤーの名前が入ります。
プレハブの中にある `Udon` オブジェクトについている Box Collider が検知するエリアになっているので、大きさを調整して使ってください。

### LogSample
好きなログを流すための最低限の機能を実装したサンプルの Prefab です。
デフォルトでは Use した時にログが流れる実装が `Kmnk/LogStream/Udon/LogSample.cs` に実装されています。
簡単なコメントを書いているので、何か自分でテキストを流す際の参考にしてください。


## その他
- Unity 2019.4.31f1, VRCSDK3 WORLD 2022.06.03.00.03 Public, UdonSharp v0.20.3 で動作を確認しています

## License
MIT License
Copyright (c) 2022 KMNK

## 更新履歴
- 2022/MM/DD v1.0.0 公開

## クレジット
- [ICOOON MONO](https://icooon-mono.com/)