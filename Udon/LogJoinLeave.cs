using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

using Kmnk.Core.Udon;

namespace Kmnk.LogStream.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LogJoinLeave : LogWriteBase
    {
        LogType _type = LogType.Notification;

        [SerializeField]
        string _joinLog = "<color='green'>>>JOIN>></color> {{name}}";

        [SerializeField]
        string _leaveLog = "<color='red'><LEAVE<</color> {{name}}";

        public override void OnPlayerJoined(VRCPlayerApi player)  
        {
            if (!Util.AmIOwner(gameObject)) { return; }
            AddMessage(
                _type,
                _joinLog.Replace("{{name}}", player.displayName),
                ""
            );
        }

        public override void OnPlayerLeft(VRCPlayerApi player)  
        {
            if (!Util.AmIOwner(gameObject)) { return; }
            AddMessage(
                _type,
                _leaveLog.Replace("{{name}}", player.displayName),
                ""
            );
        }
    }
}