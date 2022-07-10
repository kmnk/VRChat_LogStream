using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Kmnk.LogStream.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LogTriggerEnterExit : UdonSharpBehaviour
    {
        string _type = "";

        [SerializeField]
        Udon.LogStream _logStream = null;

        [SerializeField]
        string _enterLogFormat = "{0} has entered";

        [SerializeField]
        string _exitLogFormat = "{0} has exited";

        private void Start()
        {
            if (_logStream == null) { return; }
            _logStream.AddEventListener(this);
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!AmIOwner()) { return; }
            if (_logStream == null) { return; }
            _logStream.AddMessage(
                _type,
                string.Format(_enterLogFormat, player.displayName),
                ""
            );
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (!AmIOwner()) { return; }
            if (_logStream == null) { return; }
            _logStream.AddMessage(
                _type,
                string.Format(_exitLogFormat, player.displayName),
                ""
            );
        }

        #region base
        private bool AmIOwner()
        {
            return Networking.IsOwner(gameObject);
        }
        #endregion
    }
}