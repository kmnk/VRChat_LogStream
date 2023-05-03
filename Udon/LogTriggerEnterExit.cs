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
        string _enterLog = "{{name}} has entered";

        [SerializeField]
        string _exitLog = "{{name}} has exited";

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!Util.AmIOwner(gameObject)) { return; }
            AddMessage(
                _type,
                _enterLog.Replace("{{name}}", player.displayName),
                ""
            );
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (!Util.AmIOwner(gameObject)) { return; }
            AddMessage(
                _type,
                _exitLog.Replace("{{name}}", player.displayName),
                ""
            );
        }
    }
}