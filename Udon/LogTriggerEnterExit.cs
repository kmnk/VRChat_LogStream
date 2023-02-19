using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

using Kmnk.Core.Udon;

namespace Kmnk.LogStream.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LogTriggerEnterExit : LogWriteBase
    {
        LogType _type = LogType.Notification;

        [SerializeField]
        string _enterLogFormat = "{0} has entered";

        [SerializeField]
        string _exitLogFormat = "{0} has exited";

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!Util.AmIOwner(gameObject)) { return; }
            AddMessage(
                _type,
                string.Format(_enterLogFormat, player.displayName),
                ""
            );
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (!Util.AmIOwner(gameObject)) { return; }
            AddMessage(
                _type,
                string.Format(_exitLogFormat, player.displayName),
                ""
            );
        }
    }
}