using UnityEngine;

namespace Kmnk.LogStream
{
    public class LogJoinLeave : MonoBehaviour
    {
#pragma warning disable CS0414
        [SerializeField]
        [Tooltip("複数の LogStream を設置する際に指定します")]
        int _id = 0;

        [SerializeField]
        [Tooltip("入室時のメッセージを設定します")]
        string _joinLogFormat = "<color='green'>>>JOIN>></color> {0}";

        [SerializeField]
        [Tooltip("退室時のメッセージを設定します")]
        string _leaveLogFormat = "<color='red'><LEAVE</color> {0}";
#pragma warning restore CS0414
    }
}