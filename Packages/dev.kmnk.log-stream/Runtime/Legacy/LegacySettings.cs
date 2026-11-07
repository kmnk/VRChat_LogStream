using UnityEngine;

namespace Kmnk.LogStream
{
    // 2.x で設定の値を持っていたクラスの基底。3.0.0 から設定は子の Udon が持ち、
    // このクラスは 2.x の scene の値を子の Udon へ移すための読み出し元として残す。Inspector の入口などの外側の役目は GimmickRoot が持つ。
    // 移したかどうかを _migrated に持つ。移す処理は Editor/LegacySettingsMigrator.cs
    public abstract class LegacySettings : GimmickRoot
    {
#pragma warning disable CS0414
        [SerializeField]
        [HideInInspector]
        private bool _migrated = false;
#pragma warning restore CS0414
    }
}
