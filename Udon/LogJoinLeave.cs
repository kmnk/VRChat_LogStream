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
        string _joinLogFormat = "<color='green'>>>JOIN>></color> {0}";

        [SerializeField]
        string _leaveLogFormat = "<color='red'><LEAVE<</color> {0}";

        public override void OnPlayerJoined(VRCPlayerApi player)  
        {
            if (!Util.AmIOwner(gameObject)) { return; }
            AddMessage(
                _type,
                string.Format(_joinLogFormat, player.displayName),
                ""
            );
        }

        public override void OnPlayerLeft(VRCPlayerApi player)  
        {
            if (!Util.AmIOwner(gameObject)) { return; }
            AddMessage(
                _type,
                string.Format(_leaveLogFormat, player.displayName),
                ""
            );
        }
    }
}