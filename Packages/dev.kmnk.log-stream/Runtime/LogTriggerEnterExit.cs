using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Kmnk.LogStream.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LogTriggerEnterExit : LogWriteBase
    {
        [SerializeField]
        [Tooltip("Trigger Enter 時のメッセージを設定します。 {{name}} には入った人の名前が入ります")]
        private string _enterLog = "{{name}} has entered";

        [SerializeField]
        [Tooltip("Trigger Exit 時のメッセージを設定します。 {{name}} には出た人の名前が入ります")]
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
