using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Kmnk.LogStream.Udon
{
    // ログを書くギミックの基底。_core は Editor が Id から結ぶ
    public abstract class LogWriteBase : UdonSharpBehaviour
    {
        [SerializeField]
        private LogStreamCore _core = null;

        protected void AddMessage(LogType type, string message, string playerName)
        {
            _core.AddMessage(type, message, playerName);
        }

        // template の {{name}} を player の名前にしたログを、名前の欄を空にして足す
        protected void AddPlayerMessage(LogType type, string template, VRCPlayerApi player)
        {
            AddMessage(type, template.Replace("{{name}}", player.displayName), string.Empty);
        }
    }
}
