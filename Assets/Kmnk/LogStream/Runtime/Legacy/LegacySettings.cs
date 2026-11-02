using UnityEngine;

namespace Kmnk.LogStream
{
    // 2.x で設定の値を持っていたクラスの基底。3.0.0 から設定は子の Udon が持ち、
    // このクラスは 2.x の scene の値を子の Udon へ移すための読み出し元と、Inspector の入口として残す。
    // 移したかどうかを _migrated に持つ。移す処理は Editor/LegacySettingsMigrator.cs
    public abstract class LegacySettings : MonoBehaviour
    {
#pragma warning disable CS0414
        [SerializeField]
        [HideInInspector]
        private bool _migrated = false;
#pragma warning restore CS0414
    }
}
