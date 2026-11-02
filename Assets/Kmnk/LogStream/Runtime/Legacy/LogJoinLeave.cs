using UnityEngine;

namespace Kmnk.LogStream
{
    // 2.x の設定のクラス。値は LegacySettingsMigrator が子の Udon へ移す。新しく値を足さない
    [AddComponentMenu("")]
    public class LogJoinLeave : LegacySettings
    {
#pragma warning disable CS0414
        [SerializeField]
        [Tooltip("複数の LogStream を設置する際に指定します")]
        int _id = 0;

        [SerializeField]
        [Tooltip("入室時のメッセージを設定します")]
        string _joinLog = "<color='green'>>>JOIN>></color> {{name}}";

        [SerializeField]
        [Tooltip("退室時のメッセージを設定します")]
        string _leaveLog = "<color='red'><LEAVE</color> {{name}}";
#pragma warning restore CS0414
    }
}
