using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Kmnk.LogStream.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LogTriggerEnterExit : LogWriteBase
    {
        [SerializeField]
        private string _enterLog = "{{name}} has entered";

        [SerializeField]
        private string _exitLog = "{{name}} has exited";

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!Networking.IsOwner(gameObject)) { return; }
            AddPlayerMessage(LogType.Notification, _enterLog, player);
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (!Networking.IsOwner(gameObject)) { return; }
            AddPlayerMessage(LogType.Notification, _exitLog, player);
        }
    }
}
