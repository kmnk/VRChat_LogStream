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
        string _joinLog = "<color='green'>>>JOIN>></color> {{name}}";

        [SerializeField]
        [Tooltip("退室時のメッセージを設定します")]
        string _leaveLog = "<color='red'><LEAVE</color> {{name}}";
#pragma warning restore CS0414
    }
}