using UdonSharp;
using UnityEngine;

namespace Kmnk.LogStream.Udon
{
    public class LogWriteBase : UdonSharpBehaviour
    {
        [SerializeField]
        Udon.LogStream _logStream = null;

        protected void AddMessage(LogType _type, string message, string playerName)
        {
            _logStream.AddMessage(_type, message, playerName);
        }
    }
}