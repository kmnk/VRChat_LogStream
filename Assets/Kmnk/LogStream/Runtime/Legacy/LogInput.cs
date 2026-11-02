using UnityEngine;

namespace Kmnk.LogStream
{
    // 2.x の設定のクラス。値は LegacySettingsMigrator が子の Udon へ移す。新しく値を足さない
    [AddComponentMenu("")]
    public class LogInput : LegacySettings
    {
#pragma warning disable CS0414
        [SerializeField]
        [Tooltip("複数の LogStream を設置する際に対象の LogStream の id を指定します")]
        int _id = 0;

        [SerializeField]
        [Tooltip("ボードのピックアップ ON/OFF を切り替えます")]
        bool _pickupable = true;

        [SerializeField]
        [Tooltip("テンプレートメッセージ指定（旧方式）")]
        string[] _templateMessages = null;

        [SerializeField]
        [Tooltip("テキストファイルからテンプレートメッセージを読み込みます")]
        bool _useTemplateText = false;

        [SerializeField]
        [Tooltip("テンプレートメッセージを改行区切りで指定したテキストファイル")]
        TextAsset _templateText = null;

        [SerializeField]
        [Tooltip("テンプレートメッセージのボタンが入るオブジェクト")]
        Transform _templateButtonsTransform;

        [SerializeField]
        [Tooltip("テンプレートメッセージを増やした時に複製するボタンのオブジェクト")]
        GameObject _templateButtonOrigin;
#pragma warning restore CS0414
    }
}
