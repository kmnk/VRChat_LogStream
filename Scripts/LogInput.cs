using UnityEngine;

namespace Kmnk.LogStream
{
    public class LogInput : MonoBehaviour
    {
#pragma warning disable CS0414
        [SerializeField]
        [Tooltip("複数の LogStream を設置する際に対象の LogStream の id を指定します")]
        int _id = 0;

        [SerializeField]
        [Tooltip("ボードのピックアップ ON/OFF を切り替えます")]
        bool _pickupable = true;

        [SerializeField]
        [Tooltip("テンプレートメッセージ")]
        string[] _templateMessages = null;

        [SerializeField]
        [Tooltip("テンプレートメッセージのボタンが入るオブジェクト")]
        Transform _templateButtonsTransform;

        [SerializeField]
        [Tooltip("テンプレートメッセージを増やした時に複製するボタンのオブジェクト")]
        GameObject _templateButtonOrigin;
#pragma warning restore CS0414
    }
}