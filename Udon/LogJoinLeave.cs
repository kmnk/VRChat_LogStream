using System;
using System.Globalization;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace Kmnk.LogStream.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LogJoinLeave : UdonSharpBehaviour
    {
        string _type = "JOIN/LEAVE";

        [SerializeField]
        Udon.LogStream _logStream = null;

        [SerializeField]
        string _joinLogFormat = "<color='green'>>>JOIN>></color> {0}";

        [SerializeField]
        string _leaveLogFormat = "<color='red'><LEAVE<</color> {0}";

        public override void OnPlayerJoined(VRCPlayerApi player)  
        {
            if (!AmIOwner()) { return; }
            _logStream.AddMessage(
                _type,
                string.Format(_joinLogFormat, player.displayName),
                ""
            );
        }

        public override void OnPlayerLeft(VRCPlayerApi player)  
        {
            if (!AmIOwner()) { return; }
            _logStream.AddMessage(
                _type,
                string.Format(_leaveLogFormat, player.displayName),
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