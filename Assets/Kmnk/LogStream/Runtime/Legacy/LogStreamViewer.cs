using UnityEngine;

namespace Kmnk.LogStream
{
    // 2.x の設定のクラス。値は LegacySettingsMigrator が子の Udon へ移す。新しく値を足さない
    [AddComponentMenu("")]
    public class LogStreamViewer : LegacySettings
    {
#pragma warning disable CS0414
        [SerializeField]
        [Tooltip("複数の LogStream を設置する際に指定します")]
        int _id = 0;

        [SerializeField]
        [Tooltip("ボードのピックアップ ON/OFF を切り替えます")]
        bool _pickupable = true;
#pragma warning restore CS0414
    }
}
