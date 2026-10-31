using UdonSharp;
using UnityEngine;

// テストがログを足すための U# スクリプト。LogStreamCore.AddMessage は引数を取り、Udon の外から引数つきで呼ぶには
// UdonSharp の内部の変数名に依るので、値を変数で受け取って引数なしの Write で呼ぶ
public class LogStreamTestWriter : UdonSharpBehaviour
{
    public Kmnk.LogStream.Udon.LogStreamCore core;
    public int type;
    public string message;
    public string playerName;

    public void Write()
    {
        core.AddMessage((Kmnk.LogStream.Udon.LogType)type, message, playerName);
    }
}
