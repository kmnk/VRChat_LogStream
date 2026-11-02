using UnityEngine;

namespace Kmnk.LogStream
{
    // 2.x の設定のクラス。値は LegacySettingsMigrator が子の Udon へ移す。新しく値を足さない
    [AddComponentMenu("")]
    public class LogTriggerEnterExit : LegacySettings
    {
#pragma warning disable CS0414
        [SerializeField]
        [Tooltip("複数の LogStream を設置する際に指定します")]
        int _id = 0;

        [SerializeField]
        [Tooltip("Trigger Enter 時のメッセージを設定します")]
        string _enterLog = "{{name}} has entered";

        [SerializeField]
        [Tooltip("Trigger Exit 時のメッセージを設定します")]
        string _exitLog = "{{name}} has exited";
#pragma warning restore CS0414
    }
}
