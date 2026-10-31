using UdonSharp;
using UnityEngine;

namespace Kmnk.LogStream.Udon
{
    public class LogWriteBase : UdonSharpBehaviour
    {
        [SerializeField]
        Udon.LogStreamCore _core = null;

        protected void AddMessage(LogType _type, string message, string playerName)
        {
            _core.AddMessage(_type, message, playerName);
        }
    }
}