using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace Kmnk.LogStream.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LogLine : UdonSharpBehaviour
    {
        [SerializeField]
        private Text _messageText = null;

        [SerializeField]
        private Text _nameText = null;

        [SerializeField]
        private Text _timeText = null;

        public void Initialize()
        {
            Display(string.Empty, string.Empty, string.Empty);
        }

        public bool HasTextReferences()
        {
            return _messageText != null && _nameText != null && _timeText != null;
        }

        public void Display(string message, string playerName, string time)
        {
            _messageText.text = message;
            _nameText.text = playerName;
            _timeText.text = time;
        }
    }
}
