using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Kmnk.LogStream.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LogJoinLeave : LogWriteBase
    {
        [SerializeField]
        private string _joinLog = "<color='green'>>>JOIN>></color> {{name}}";

        [SerializeField]
        private string _leaveLog = "<color='red'><LEAVE</color> {{name}}";

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!Networking.IsOwner(gameObject)) { return; }
            AddPlayerMessage(LogType.Notification, _joinLog, player);
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (!Networking.IsOwner(gameObject)) { return; }
            AddPlayerMessage(LogType.Notification, _leaveLog, player);
        }
    }
}
